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

            AVLiveQueryExtensions.GetCurrentSessionToken(sessionToken);

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
            AVLiveQueryExtensions.GetCurrentSessionToken(sessionToken);
            var command = new AVCommand("LiveQuery/unsubscribe/" + Id,
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
