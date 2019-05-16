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
            string appId = "Eohx7L4EMfe4xmairXeT7q1w-gzGzoHsz";
            string appKey = "GSBSGpYH9FsRdCss8TGQed0F";

            AVRealtime.WebSocketLog(Console.WriteLine);
            AVClient.HttpLog(Console.WriteLine);

            AVClient.Initialize(appId, appKey);
            Websockets.Net.WebsocketConnection.Link();
            var realtime = new AVRealtime(new AVRealtime.Configuration { 
                ApplicationId = appId,
                ApplicationKey = appKey,
                RealtimeServer = new Uri("wss://rtm51.leancloud.cn/")
            });
            AVLiveQuery.Channel = realtime;

            var query = new AVQuery<AVObject>("GameObject").WhereEqualTo("objectId", "5c7f954912215f00728f10ff");
            //var query = new AVQuery<AVObject>("GameObject").WhereExists("objectId");
            liveQuery = await query.SubscribeAsync();
            liveQuery.OnLiveQueryReceived += OnLiveQueryReceived;
            await liveQuery.UnsubscribeAsync();

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
