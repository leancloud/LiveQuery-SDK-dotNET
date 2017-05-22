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

namespace LeanCloud.LiveQuery.UnitTest.NetFx45
{
    [TestFixture]
    public class SubscribeTest
    {
        [SetUp]
        public void initApp()
        {
            string appId = ConfigurationManager.AppSettings["appId"];
            string appKey = ConfigurationManager.AppSettings["appKey"];
            Websockets.Net.WebsocketConnection.Link();
            var realtime = new AVRealtime(appId, appKey);
            AVRealtime.WebSocketLog(Console.WriteLine);
            AVClient.HttpLog(Console.WriteLine);
        }

        [Test]
        public Task TestRealtime()
        {
            return AVRealtime.Instance.CreateClient("junwu").ContinueWith(t =>
             {
                 var client = t.Result;
                 var conversation = AVIMConversation.CreateWithoutData("58be1f5392509726c3dc1c8b", client);
                 return client.SendMessageAsync(conversation, new AVIMTextMessage("hehe"), receipt: false);
             }).Unwrap().ContinueWith(m =>
             {
                 var message = m.Result;
                 Assert.IsNotNull(message.Id);
                 return Task.FromResult(0);
             });
        }
        [Test]
        public Task TestCreate()
        {
            var query = new AVQuery<AVObject>("TodoLiveQuery").WhereEqualTo("name", "livequery");

            return query.CreateAsync<AVObject>().ContinueWith(t =>
            {
                var lqInstance = t.Result;
                lqInstance.OnLiveQueryReceived += LqInstance_OnLiveQueryReceived;
            }).ContinueWith(s =>
            {
                return Task.FromResult(0);
            });
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
