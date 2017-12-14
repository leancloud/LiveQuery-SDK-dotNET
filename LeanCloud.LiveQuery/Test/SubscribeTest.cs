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
using LeanCloud.Storage.Internal;

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
            }).Unwrap().ContinueWith(s =>
            {
                var testObj = new AVObject("TodoLiveQuery");
                testObj["name"] = "livequery";
                return testObj.SaveAsync();
            }).Unwrap().ContinueWith(z =>
            {
                return Task.Delay(200000);
            }).Unwrap();
        }

        private void LivequeryInstance_OnLiveQueryReceived(object sender, AVLiveQueryEventArgs<AVObject> e)
        {
            Assert.IsTrue(e.Scope == "create");
            Assert.IsNotNull(e.Payload.ObjectId);
        }

        private readonly object mutex = new object();
        [Test, Timeout(300000)]
        public Task TestDelete()
        {
            var query = new AVQuery<AVObject>("TodoLiveQuery").WhereEqualTo("name", "livequery");
            var deleteTick = 0;
            var leaveTick = 0;
            return query.SubscribeAsync().ContinueWith(subT =>
            {
                var livequery = subT.Result;
                livequery.OnLiveQueryReceived += (sender, e) =>
                {
                    switch (e.Scope)
                    {
                        case "create": //符合查询条件的对象创建
                            break;
                        case "update": //符合查询条件的对象属性修改
                            break;
                        case "enter": //对象被修改后，从不符合查询条件变成符合
                            break;
                        case "leave": //对象被修改后，从符合查询条件变成不符合
                            lock (mutex)
                            {
                                leaveTick++;
                            }
                            LogText("leave:" + leaveTick);
                            break;
                        case "delete": //对象删除
                            lock (mutex)
                            {
                                deleteTick++;
                            }
                            LogText("delete:" + deleteTick);
                            break;
                    }
                };
                return Task.Delay(200000);
            }).Unwrap();
        }

    }
}
