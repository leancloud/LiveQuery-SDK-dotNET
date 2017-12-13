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

    public static class AVLiveQuery
    {

        /// <summary>
        /// LiveQuery 传输数据的 AVRealtime 实例
        /// </summary>
        public static AVRealtime Channel
        {
            get; set;
        }
    }
    /// <summary>
    /// AVLiveQuery 对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
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

        /// <summary>
        /// 创建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="sessionToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).ContinueWith(t =>
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

        /// <summary>
        /// 当前 AVLiveQuery 对象的 Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// LiveQuery 对应的 QueryId
        /// </summary>
        public string QueryId { get; set; }

        /// <summary>
        /// 根据 id 创建 AVLiveQuery 对象
        /// </summary>
        /// <param name="id"></param>
        public AVLiveQuery(string id)
        {
            Id = id;
        }

        /// <summary>
        /// 根据 AVQuery 创建 AVLiveQuery 对象
        /// </summary>
        /// <param name="query"></param>
        public AVLiveQuery(AVQuery<T> query)
        {
            this.Query = query;
        }
        /// <summary>
        /// AVLiveQuery 对应的 AVQuery 对象
        /// </summary>
        public AVQuery<T> Query { get; set; }

        /// <summary>
        /// 数据推送的触发的事件通知
        /// </summary>
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

        /// <summary>
        /// 订阅操作
        /// </summary>
        /// <param name="sessionToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
                 this.Id = subcriptionT.Result.Id;
                 this.QueryId = subcriptionT.Result.QueryId;

                 if (AVLiveQuery.Channel == null) throw new NullReferenceException("before subscribe live query, plaese call new AVRealtime(config) and set it as AVLiveQuery.Channel.");

                 if (AVLiveQuery.Channel.State == AVRealtime.Status.Online)
                 {
                     return Task.FromResult(true);
                 }
                 else
                 {
                     // open the websocket with LeanCloud Realtime cloud service.
                     AVLiveQuery.Channel.ToggleNotification(true);
                     return AVLiveQuery.Channel.OpenAsync();
                 }
             }).Unwrap().OnSuccess(openT =>
             {
                 if (Opend) return Task.FromResult(new Tuple<int, IDictionary<string, object>>(0, null));
                
                 var liveQueryLogInCmd = new AVIMCommand().Command("login")
                          .Argument("installationId", this.Id)
                          .Argument("service", 1).AppId(AVClient.CurrentConfiguration.ApplicationId);
                 // open the session for LiveQuery.
                 return AVLiveQuery.Channel.AVIMCommandRunner.RunCommandAsync(liveQueryLogInCmd);

             }).Unwrap().OnSuccess(runT =>
             {
                 if (runT.Result.Item1 > 0)
                 {
                     AVRealtime.PrintLog("error on login to LiveQuery");
                 }
                 Opend = true;
                 // set the event hanler when received LiveQuery data pushed.
                 ToggleNotice(true);
             });
        }
        private bool registed;
        public bool NoticeSwitch { get; private set; }
        private void ToggleNotice(bool toggle)
        {
            this.NoticeSwitch = toggle;
            if (!this.NoticeSwitch) return;
            if (registed) return;

            AVLiveQuery.Channel.NoticeReceived += (sender, e) =>
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

                                     Dictionary<string, object> payloadMap = null;
                                     if (payload.ContainsKey("object"))
                                         payloadMap = payload["object"] as Dictionary<string, object>;

                                     string[] keys = null;
                                     if (payload.ContainsKey("updatedKeys"))
                                     {
                                         var keyObjs = payload["updatedKeys"] as List<object>;
                                         if (keyObjs != null)
                                         {
                                             keys = keyObjs.Select(item => item.ToString()).ToArray();
                                         }
                                     }
                                     // emit it to livequery instance.
                                     this.Emit(scope, keys, payloadMap);
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
        public void Emit(string scope, string[] keys, IDictionary<string, object> payloadMap)
        {
            var objectState = AVObjectCoder.Instance.Decode(payloadMap, AVDecoder.Instance);
            var payloadObject = AVObject.FromState<T>(objectState, Query.GetClassName<T>());
            var args = new AVLiveQueryEventArgs<T>()
            {
                Scope = scope,
                Keys = keys,
                Payload = payloadObject
            };
            OnLiveQueryReceived.Invoke(this, args);
        }

    }
}
