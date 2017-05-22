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
        public static Task<AVLiveQuery<T>> SubscribeAsync<T>(this AVQuery<T> query, string sessionToken = "", CancellationToken cancellationToken = default(CancellationToken)) where T : AVObject
        {
            var liveQuery = new AVLiveQuery<T>(query);
            return liveQuery.SubscribeAsync().OnSuccess(t =>
            {
                return liveQuery;
            });
        }
    }
}
