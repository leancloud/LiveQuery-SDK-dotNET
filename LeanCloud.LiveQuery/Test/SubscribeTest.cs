using NUnit.Framework;
using LeanCloud;
using LeanCloud.Core.Internal;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Realtime;
using System.Configuration;
using System.Diagnostics;

namespace LeanCloud.LiveQuery.UnitTest.NetFx45
{
    [TestFixture]
    public class SubscribeTest
    {
        public void LogText(string message)
        {
            Trace.WriteLine(message);
        }
        [SetUp]
        public void initApp()
        {
            string appId = ConfigurationManager.AppSettings["appId"];
            string appKey = ConfigurationManager.AppSettings["appKey"];

            // 初始化存储模块
            AVClient.Initialize(appId, appKey);
            AVClient.HttpLog(LogText);

            // 初始化聊天模块
            Websockets.Net.WebsocketConnection.Link();
            var realtime = new AVRealtime(appId, appKey);
            AVRealtime.WebSocketLog(LogText);

            // 需要为 LiveQuery 指定一个 AVRealtime 实例用来接收来自云端的推送
            AVLiveQuery.Channel = realtime;
        }

        [Test, Timeout(300000)]
        public Task TestSubscribe()
        {
            var query = new AVQuery<AVObject>("TodoLiveQuery").WhereEqualTo("name", "livequery");

            return query.SubscribeAsync().ContinueWith(t =>
            {
                var livequeryInstance = t.Result;
                livequeryInstance.OnLiveQueryReceived += LivequeryInstance_OnLiveQueryReceived;
                return Task.FromResult(0);
            }).ContinueWith(s =>
            {
                var testObj = new AVObject("TodoLiveQuery");
                testObj["name"] = "livequery";
                return testObj.SaveAsync();
            }).ContinueWith(z =>
            {
                return Task.Delay(200000);
            }).Unwrap();
        }

        private void LivequeryInstance_OnLiveQueryReceived(object sender, AVLiveQueryEventArgs<AVObject> e)
        {
            Assert.IsTrue(e.Scope == "create");
            Assert.IsNotNull(e.Payload.ObjectId);
        }
    }
}
