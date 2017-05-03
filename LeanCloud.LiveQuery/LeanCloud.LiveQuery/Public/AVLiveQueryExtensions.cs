using System;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;
using LeanCloud.Realtime;
using LeanCloud.Core.Internal;
using LeanCloud.Realtime.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;


namespace LeanCloud.LiveQuery
{
    public static class AVLiveQueryExtensions
    {
        public static bool Open { get; internal set; }
        public static Task<AVLiveQuery<T>> SubscribeAsync<T>(this AVQuery<T> query, string sessionToken = "", CancellationToken cancellationToken = default(CancellationToken)) where T : AVObject
        {
            AVLiveQuery<T> rtn = null;
			if (sessionToken == "")
			{
				if (AVUser.CurrentUser != null)
				{
					sessionToken = AVUser.CurrentUser.SessionToken;
				}
			}

            return CreateAsync(query, sessionToken, cancellationToken).OnSuccess(_ =>
             {
                 rtn = _.Result;
                 if (Open) return Task.FromResult(rtn);

                 if (AVRealtime.Instance == null) throw new NullReferenceException("before subscribe live query, plaese call new AVRealtime(config) to initalize Realtime module.");
                 return AVRealtime.Instance.OpenAsync().OnSuccess(t =>
                 {
                     var liveQueryLogInCmd = new AVIMCommand()
                        .Option("login")
                        .Argument("installationId", rtn.Id)
                        .Argument("service", 1);

                     return AVRealtime.AVIMCommandRunner.RunCommandAsync(liveQueryLogInCmd);
                 }).Unwrap().OnSuccess(s =>
                 {
                     Open = true;
                     AVRealtime.Instance.NoticeReceived += (sender, e) =>
                     {
                         if (e.CommandName == "data")
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
                                         return query_id == rtn.QueryId;
                                     });
                                     foreach (var payload in matchedPayloads)
                                     {
                                         var scope = payload["op"].ToString();
                                         var payloadMap = payload["object"] as Dictionary<string, object>;
                                         rtn.Emit(scope, payloadMap);
                                     }
                                 }
                             }
                         }
                     };
                     return rtn;
                 });
             }).Unwrap();
        }

        public static Task<AVLiveQuery<T>> CreateAsync<T>(this AVQuery<T> query, string sessionToken = "", CancellationToken cancellationToken = default(CancellationToken)) where T : AVObject
        {
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "where", query.Condition },
                { "className", query.GetClassName<T>() },
                { "sessionToken", sessionToken }
            };

            var command = new AVCommand("LiveQuery/subscribe",
                                        "POST",
                                        sessionToken: sessionToken,
                                        data: strs);

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t =>
            {
                var subscriptionId = t.Result.Item2["id"] as string;
                var queryId = t.Result.Item2["query_id"] as string;
                var rtn = new AVLiveQuery<T>(subscriptionId)
                {
                    Query = query,
                    QueryId = queryId
                };
                return rtn;
            });
        }

    }
}
