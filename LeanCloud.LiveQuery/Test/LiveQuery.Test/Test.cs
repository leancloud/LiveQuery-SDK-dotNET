using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Realtime;
using LeanCloud.LiveQuery;

namespace LiveQuery.Test {
    [TestFixture]
    public class Test {
        readonly string AppId = "BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz";
        readonly string AppKey = "pbf6Nk5seyjilexdpyrPwjSp";
        readonly string Server = "https://bmyv4rks.lc-cn-n1-shared.com";

        [SetUp]
        public void SetUp() {
            AVRealtime.WebSocketLog(Console.WriteLine);
            AVClient.HttpLog(Console.WriteLine);

            AVClient.Initialize(AppId, AppKey, Server);
            Websockets.Net.WebsocketConnection.Link();
            var realtime = new AVRealtime(new AVRealtime.Configuration {
                ApplicationId = AppId,
                ApplicationKey = AppKey
            });
            AVLiveQuery.Channel = realtime;
        }

        [Test]
        public async Task Create() {
            var f = false;
            var query = new AVQuery<AVObject>("Account").WhereExists("objectId");
            var liveQuery = await query.SubscribeAsync();
            liveQuery.OnLiveQueryReceived += (sender, e) => {
                Console.WriteLine(e.Scope);
                f = true;
            };

            var go = new AVObject("Account");
            await go.SaveAsync();
            Console.WriteLine(go.ObjectId);

            while (!f) {
                await Task.Delay(100);
            }

            Console.WriteLine("Test create done!");
        }

        [Test]
        public async Task Enter() {
            var f = false;
            var query = new AVQuery<AVObject>("Account").WhereContains("name", "x");
            var liveQuery = await query.SubscribeAsync();
            liveQuery.OnLiveQueryReceived += (sender, e) => {
                Console.WriteLine(e.Scope);
                Assert.AreEqual(e.Scope, "enter");
                f = true;
            };

            var q = AVObject.GetQuery("Account");
            var go = await q.GetAsync("5df7268cd4b56c00748e91af");
            go["name"] = "xxx";
            await go.SaveAsync();
            Console.WriteLine(go.ObjectId);

            while (!f) {
                await Task.Delay(100);
            }

            go["name"] = "yyy";
            await go.SaveAsync();
            Console.WriteLine("Test enter done!");
        }

        [Test]
        public async Task Leave() {
            var f = false;
            var query = new AVQuery<AVObject>("Account").WhereContains("name", "s");
            var liveQuery = await query.SubscribeAsync();
            liveQuery.OnLiveQueryReceived += (sender, e) => {
                Console.WriteLine(e.Scope);
                Assert.AreEqual(e.Scope, "leave");
                f = true;
            };

            var q = AVObject.GetQuery("Account");
            var go = await q.GetAsync("5df0b08f5620710073a95ccd");
            go["name"] = "aaa";
            await go.SaveAsync();
            Console.WriteLine(go.ObjectId);

            while (!f) {
                await Task.Delay(100);
                f = true;
            }

            go["name"] = "sss";
            await go.SaveAsync();
            Console.WriteLine("Test leave done!");
        }

        [Test]
        public async Task Update() {
            var f = false;
            var query = new AVQuery<AVObject>("Account");
            query = query.WhereLessThan("createdAt", DateTime.Now);
            var liveQuery = await query.SubscribeAsync();
            liveQuery.OnLiveQueryReceived += (sender, e) => {
                Console.WriteLine(e.Scope);
                Assert.AreEqual(e.Scope, "update");
                f = true;
            };

            var go = await query.FirstAsync();
            go["name"] = "bbb";
            await go.SaveAsync();
            Console.WriteLine(go.ObjectId);

            while (!f) {
                await Task.Delay(100);
                f = true;
            }

            go["name"] = "ccc";
            await go.SaveAsync();
            Console.WriteLine("Test update done!");
        }

        [Test]
        public async Task Delete() {
            var f = false;
            var go = new AVObject("Todo");
            await go.SaveAsync();
            var objectId = go.ObjectId;
            var query = new AVQuery<AVObject>("Todo").WhereEqualTo("objectId", objectId);
            var liveQuery = await query.SubscribeAsync();
            liveQuery.OnLiveQueryReceived += (sender, e) => {
                Console.WriteLine(e.Scope);
                Assert.AreEqual(e.Scope, "delete");
                f = true;
            };

            await go.DeleteAsync();
            Console.WriteLine("object delete done");

            while (!f) {
                await Task.Delay(100);
                f = true;
            }
            Console.WriteLine("Test delete done!");
        }
    }
}
