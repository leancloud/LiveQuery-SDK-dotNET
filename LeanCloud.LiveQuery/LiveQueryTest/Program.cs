using System;
using LeanCloud;
using LeanCloud.Realtime;
using LeanCloud.LiveQuery;
using System.Threading.Tasks;

namespace LiveQueryTest {
    class MainClass {
        static AVLiveQuery<AVObject> liveQuery = null;

        public static void Main(string[] args) {
            Console.WriteLine("Hello World!");

            Test();

            Console.ReadKey();
        }

        static async void Test() {
            //string appId = "Eohx7L4EMfe4xmairXeT7q1w-gzGzoHsz";
            //string appKey = "GSBSGpYH9FsRdCss8TGQed0F";

            string appId = "FQr8l8LLvdxIwhMHN77sNluX-9Nh9j0Va";
            string appKey = "MJSm46Uu6LjF5eNmqfbuUmt6";

            AVRealtime.WebSocketLog(Console.WriteLine);
            AVClient.HttpLog(Console.WriteLine);

            AVClient.Initialize(appId, appKey);
            Websockets.Net.WebsocketConnection.Link();
            var realtime = new AVRealtime(new AVRealtime.Configuration { 
                ApplicationId = appId,
                ApplicationKey = appKey
            });
            AVLiveQuery.Channel = realtime;

            var query = new AVQuery<AVObject>("GameObject").WhereEqualTo("objectId", "5cedee8e58cf480008de9caa");
            //var query = new AVQuery<AVObject>("GameObject").WhereExists("objectId");
            liveQuery = await query.SubscribeAsync();
            liveQuery.OnLiveQueryReceived += OnLiveQueryReceived;

            Console.WriteLine("done");
        }

        static void OnLiveQueryReceived(object sender, AVLiveQueryEventArgs<AVObject> e) {
            AVObject go = e.Payload;
            if (go.TryGetValue("name", out string name)) {
                Console.WriteLine($"{e.Scope} : {name}");
            }
        }
    }
}
