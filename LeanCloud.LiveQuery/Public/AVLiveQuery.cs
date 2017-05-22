using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal;
using LeanCloud.Realtime;
using LeanCloud.Core.Internal;
using LeanCloud.Realtime.Internal;
using System.Linq;
using System.Linq.Expressions;

namespace LeanCloud.LiveQuery
{
    public class AVLiveQuery<T> where T : AVObject
    {
        public static bool Opend { get; internal set; }
        internal static readonly object mutex = new object();
        public static string GetCurrentSessionToken(string sessionToken = "")
        {
            if (sessionToken == "")
            {
                if (AVUser.CurrentUser != null)
                {
                    sessionToken = AVUser.CurrentUser.SessionToken;
                }
            }
            return sessionToken;
        }

        public static Task<AVLiveQuery<T>> CreateAsync<T>(AVQuery<T> query, string subscriptionId = "", string sessionToken = "", CancellationToken cancellationToken = default(CancellationToken)) where T : AVObject
        {
            var queryMap = new Dictionary<string, object>()
            {
                { "where",query.Condition},
                { "className",query.GetClassName<T>()}
            };

            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "query",queryMap },
                { "sessionToken", sessionToken },
                { "id",subscriptionId.Length >0 ?subscriptionId:null }
            };

            var command = new AVCommand("LiveQuery/subscribe",
                                        "POST",
                                        sessionToken: sessionToken,
                                        data: strs);

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t =>
            {
                subscriptionId = t.Result.Item2["id"] as string;
                var queryId = t.Result.Item2["query_id"] as string;
                var rtn = new AVLiveQuery<T>(subscriptionId)
                {
                    Query = query,
                    QueryId = queryId
                };
                return rtn;
            });
        }
        public string Id { get; set; }

        public string QueryId { get; set; }

        public AVLiveQuery(string id)
        {
            Id = id;
        }

        public AVLiveQuery(AVQuery<T> query)
        {
            this.Query = query;
        }

        public AVQuery<T> Query { get; set; }

        public event EventHandler<AVLiveQueryEventArgs<T>> OnLiveQueryReceived;

        /// <summary>
        /// 推送抵达时触发事件通知
        /// </summary>
        /// <param name="scope">产生这条推送的原因。
        /// <remarks>
        /// create:符合查询条件的对象创建;
        /// update:符合查询条件的对象属性修改。
        /// enter:对象修改事件，从不符合查询条件变成符合。
        /// leave:对象修改时间，从符合查询条件变成不符合。
        /// delete:对象删除
        /// login:只对 _User 对象有效，表示用户登录。
        /// </remarks>
        /// </param>
        /// <param name="onRecevived"></param>
        public void On(string scope, Action<T> onRecevived)
        {
            this.OnLiveQueryReceived += (sender, e) =>
            {
                if (e.Scope == scope)
                {
                    onRecevived.Invoke(e.Payload);
                }
            };
        }

        public Task SubscribeAsync(string sessionToken = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.Query == null) throw new Exception("Query can not be null when subcribe.");

            GetCurrentSessionToken(sessionToken);
            // get installation id as the LiveQuery subscription id.
            return AVPlugins.Instance.InstallationIdController.GetAsync().OnSuccess(insT =>
             {
                 var subscriptionId = insT.Result.ToString();
                 // create LiveQuery subscription with an id.
                 return CreateAsync(this.Query, subscriptionId, sessionToken, cancellationToken);
             }).Unwrap().OnSuccess(subcriptionT =>
             {
                 lock (mutex)
                 {
                     this.Id = subcriptionT.Result.Id;
                     this.QueryId = subcriptionT.Result.QueryId;
                     if (AVRealtime.Instance.State == AVRealtime.Status.Online)
                     {
                         return Task.FromResult(0);
                     }
                     if (AVRealtime.Instance == null) throw new NullReferenceException("before subscribe live query, plaese call new AVRealtime(config) to initalize Realtime module.");
                     // open the websocket with LeanCloud Realtime cloud service.
                     return AVRealtime.Instance.OpenAsync();
                 }

             }).Unwrap().OnSuccess(openT =>
             {
                 lock (mutex)
                 {
                     if (Opend) return Task.FromResult(new Tuple<int, IDictionary<string, object>>(0, null));
                     AVRealtime.Instance.ToggleNotification(true);
                     var liveQueryLogInCmd = new AVIMCommand().Command("login")
                              .Argument("installationId", this.Id)
                              .Argument("service", 1);
                     // open the session for LiveQuery.
                     return AVRealtime.AVIMCommandRunner.RunCommandAsync(liveQueryLogInCmd);
                 }
             }).Unwrap().OnSuccess(runT =>
             {
                 lock (mutex)
                 {
                     if (runT.Result.Item1 > 0)
                     {
                         AVRealtime.PrintLog("error on login to LiveQuery");
                     }
                     Opend = true;
                     // set the event hanler when received LiveQuery data pushed.
                     ToggleNotice(true);
                 }
             });
        }
        private bool registed;
        public bool NoticeSwitch { get; private set; }
        private void ToggleNotice(bool toggle)
        {
            this.NoticeSwitch = toggle;
            if (!this.NoticeSwitch) return;
            if (registed) return;

            AVRealtime.Instance.NoticeReceived += (sender, e) =>
            {
                if (e.CommandName == "data")
                {
                    if (NoticeSwitch)
                    {
                        var ids = AVDecoder.Instance.DecodeList<string>(e.RawData["ids"]);
                        var msg = e.RawData["msg"] as IEnumerable<object>;
                        if (msg != null)
                        {
                            var receivedPayloads = from item in msg
                                                   select item as Dictionary<string, object>;
                            if (receivedPayloads != null)
                            {
                                var matchedPayloads = receivedPayloads.Where(item =>
                                {
                                    if (!item.ContainsKey("query_id")) return false;
                                    var query_id = item["query_id"].ToString();
                                    return query_id == this.QueryId;
                                });
                                foreach (var payload in matchedPayloads)
                                {
                                    var scope = payload["op"].ToString();
                                    var payloadMap = payload["object"] as Dictionary<string, object>;
                                    // emit it to livequery instance.
                                    this.Emit(scope, payloadMap);
                                }
                            }
                        }
                    }
                }
            };

            registed = true;
        }

        /// <summary>
        /// 增加对该 LiveQuery 对象 7 天的延长订阅有效期
        /// </summary>
        /// <param name="sessionToken"></param>
        /// <returns></returns>
        public Task<bool> RenewAsync(string sessionToken = "")
        {
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "id", Id },
                { "query_id", QueryId },
            };

            GetCurrentSessionToken(sessionToken);

            var command = new AVCommand("LiveQuery/subscribe/ping",
                                      "POST",
                                      sessionToken: sessionToken,
                                      data: strs);

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t =>
            {
                return t.Result.Item1 != System.Net.HttpStatusCode.NotFound;
            });
        }

        /// <summary>
        /// 取消对当前 LiveQuery 对象的订阅
        /// </summary>
        /// <param name="sessionToken"></param>
        /// <returns></returns>
        public Task<bool> Unsubscribe(string sessionToken = "")
        {
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "id", Id },
                { "query_id", QueryId },
            };
            GetCurrentSessionToken(sessionToken);
            var command = new AVCommand("LiveQuery/unsubscribe/",
                          "POST",
                          sessionToken: sessionToken,
                          data: strs);

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t =>
            {
                return t.Result.Item1 != System.Net.HttpStatusCode.NotFound;
            });
        }

        /// <summary>
        /// 本地推送到指定的 LiveQuery
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="payloadMap"></param>
        public void Emit(string scope, IDictionary<string, object> payloadMap)
        {
            var objectState = AVObjectCoder.Instance.Decode(payloadMap, AVDecoder.Instance);
            var payloadObject = AVObject.FromState<T>(objectState, Query.GetClassName<T>());
            var args = new AVLiveQueryEventArgs<T>()
            {
                Scope = scope,
                Payload = payloadObject
            };
            OnLiveQueryReceived.Invoke(this, args);
        }

    }
}
