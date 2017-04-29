using System;
using LeanCloud;
using LeanCloud.Realtime;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Core.Internal;

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

        public void Emit(string scope, IDictionary<string, object> payloadMap)
        {
            var objectState = AVObjectCoder.Instance.Decode(payloadMap, AVDecoder.Instance);
            var payloadObject = AVObject.FromState<T>(objectState, Query.GetClassName<T>());
            var args = new AVLiveQueryEventArgs<T>()
            {
                Scope = scope,
                Payload = payloadObject
            };
            OnLiveQueryReceived.Invoke(this,args);
        }

    }
}
