using NUnit.Framework;
using System;
using LeanCloud.Realtime;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Collections;
using System.Linq;

namespace LeanCloud.LiveQuery.UnitTest.Mono45
{
    [TestFixture()]
    public class Test
    {
        [SetUp()]
        public void SetUp()
        {
			string appId = "uay57kigwe0b6f5n0e1d4z4xhydsml3dor24bzwvzr57wdap";
			string appKey = "kfgz7jjfsk55r5a8a3y4ttd3je1ko11bkibcikonk32oozww";
			Websockets.Net.WebsocketConnection.Link();
			var realtime = new AVRealtime(appId, appKey);
			AVRealtime.WebSocketLog(Console.WriteLine);
			AVClient.HttpLog(Console.WriteLine);
        }

        [Test()]
        public async Task TestSubscribe()
		{
			var doingQuery = new AVQuery<AVObject>("Todo").WhereEqualTo("state", "doing");
            var doneQuery = new AVQuery<AVObject>("Todo").WhereEqualTo("state", "done");

			// 假设 doingList 对应的是某一个列表控件绑定的数据源
			

            var doing = await doingQuery.FindAsync();

            doingList = doing.ToList();

            var livequery = await doingQuery.SubscribeAsync();

            livequery.OnLiveQueryReceived += (sender, e) => 
            {
                if(e.Scope == "create")
                {
                    doingList.Add(e.Payload);
                }
            };

			var testObj = new AVObject("Todo");
			testObj["state"] = "doing";
			await testObj.SaveAsync();

            var oneDoing = AVObject.CreateWithoutData("Todo", "5915bb92a22b9d005804a4ee");
            oneDoing["state"] = "done";
            await oneDoing.SaveAsync();
		}
        public List<AVObject> doingList = new List<AVObject>();
		private void LivequeryInstance_OnLiveQueryReceived(object sender, AVLiveQueryEventArgs<AVObject> e)
		{
			Assert.IsTrue(e.Scope == "create");
			Assert.IsNotNull(e.Payload.ObjectId);

            if(e.Scope == "update")
            {

            }
		}

    }
}
