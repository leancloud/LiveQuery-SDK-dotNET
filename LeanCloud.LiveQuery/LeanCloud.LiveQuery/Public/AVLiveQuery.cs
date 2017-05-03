using System;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Core.Internal;
using LeanCloud.Realtime.Internal;

namespace LeanCloud.LiveQuery
{
    public class AVLiveQuery<T> where T : AVObject
    {
        public string Id { get; set; }

        public string QueryId { get; set; }

        public AVLiveQuery(string id)
        {
            Id = id;
        }

        public AVQuery<T> Query { get; internal set; }

        public event EventHandler<AVLiveQueryEventArgs<T>> OnLiveQueryReceived;

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

        public Task<bool> RenewAsync(string sessionToken = "")
        {
            Dictionary<string, object> strs = new Dictionary<string, object>()
            {
                { "id", Id },
                { "query_id", QueryId },
            };

            if (sessionToken == "")
            {
                if (AVUser.CurrentUser != null)
                {
                    sessionToken = AVUser.CurrentUser.SessionToken;
                }
            }
            var command = new AVCommand("LiveQuery/subscribe",
                                      "POST",
                                      sessionToken: sessionToken,
                                      data: strs);

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t =>
            {
                return t.Result.Item1 == System.Net.HttpStatusCode.NotFound;
            });
        }

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
