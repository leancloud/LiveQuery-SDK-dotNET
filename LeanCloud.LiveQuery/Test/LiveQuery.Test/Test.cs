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
        string appId = "Eohx7L4EMfe4xmairXeT7q1w-gzGzoHsz";
        string appKey = "GSBSGpYH9FsRdCss8TGQed0F";

        void Init() {
            AVRealtime.WebSocketLog(Console.WriteLine);
            AVClient.HttpLog(Console.WriteLine);

            AVClient.Initialize(appId, appKey, "https://avoscloud.com");
            Websockets.Net.WebsocketConnection.Link();
            var realtime = new AVRealtime(new AVRealtime.Configuration {
                ApplicationId = appId,
                ApplicationKey = appKey
            });
            AVLiveQuery.Channel = realtime;
        }

        [Test]
        public async Task Create() {
            Init();

            var f = false;
            var query = new AVQuery<AVObject>("GameObject").WhereExists("objectId");
            var liveQuery = await query.SubscribeAsync();
            liveQuery.OnLiveQueryReceived += (sender, e) => {
                Console.WriteLine(e.Scope);
                f = true;
            };

            var go = new AVObject("GameObject");
            await go.SaveAsync();
            Console.WriteLine(go.ObjectId);

            while (!f) {
                await Task.Delay(100);
            }

            Console.WriteLine("Test create done!");
        }

        [Test]
        public async Task Enter() {
            Init();

            var f = false;
            var query = new AVQuery<AVObject>("GameObject").WhereContains("name", "x");
            var liveQuery = await query.SubscribeAsync();
            liveQuery.OnLiveQueryReceived += (sender, e) => {
                Console.WriteLine(e.Scope);
                Assert.AreEqual(e.Scope, "enter");
                f = true;
            };

            var q = AVObject.GetQuery("GameObject");
            var go = await q.GetAsync("5cd3e68830863b00701e2876");
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
            Init();

            var f = false;
            var query = new AVQuery<AVObject>("GameObject").WhereContains("name", "s");
            var liveQuery = await query.SubscribeAsync();
            liveQuery.OnLiveQueryReceived += (sender, e) => {
                Console.WriteLine(e.Scope);
                Assert.AreEqual(e.Scope, "leave");
                f = true;
            };

            var q = AVObject.GetQuery("GameObject");
            var go = await q.GetAsync("5cd3e75fc8959c006843886a");
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
            Init();

            var f = false;
            var query = new AVQuery<AVObject>("GameObject").WhereEqualTo("objectId", "5cdcd79bc8959c0068de3df2");
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
            Init();

            var f = false;
            var go = new AVObject("GameObject");
            await go.SaveAsync();
            var objectId = go.ObjectId;
            var query = new AVQuery<AVObject>("GameObject").WhereEqualTo("objectId", objectId);
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
