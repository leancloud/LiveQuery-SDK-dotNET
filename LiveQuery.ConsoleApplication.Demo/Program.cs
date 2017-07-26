using LeanCloud;
using LeanCloud.LiveQuery;
using LeanCloud.Realtime;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveQuery.ConsoleApplication.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            string appId = ConfigurationManager.AppSettings["appId"];
            string appKey = ConfigurationManager.AppSettings["appKey"];

            // 初始化存储模块
            AVClient.Initialize(appId, appKey);
            AVClient.HttpLog(Console.WriteLine);

            // 初始化聊天模块
            Websockets.Net.WebsocketConnection.Link();
            var realtime = new AVRealtime(appId, appKey);
            AVRealtime.WebSocketLog(Console.WriteLine);

            // 需要为 LiveQuery 指定一个 AVRealtime 实例用来接收来自云端的推送
            AVLiveQuery.Channel = realtime;


            var query = new AVQuery<AVObject>("TodoLiveQuery").WhereEqualTo("name", "livequery");

            query.SubscribeAsync().ContinueWith(t =>
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

            Console.ReadKey();
        }

        private static void LivequeryInstance_OnLiveQueryReceived(object sender, AVLiveQueryEventArgs<AVObject> e)
        {
            Console.WriteLine(e.Scope == "create");
            Console.WriteLine(e.Payload.ObjectId);
        }
    }
}
