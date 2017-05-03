using NUnit.Framework;
using System;
using LeanCloud.Realtime;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.LiveQuery.Test.UnitTest
{
    [TestFixture()]
    public class Test
    {
        [SetUp()]
        public void Init()
        {
            Websockets.Net.WebsocketConnection.Link();
            var realtime = new AVRealtime("uay57kigwe0b6f5n0e1d4z4xhydsml3dor24bzwvzr57wdap", "kfgz7jjfsk55r5a8a3y4ttd3je1ko11bkibcikonk32oozww");
        }
        [Test()]
        public Task TestCase()
        {
            return AVRealtime.Instance.CreateClient("junwu").ContinueWith(t =>
             {
                 var client = t.Result;
                 var conversation = AVIMConversation.CreateWithoutData("", t.Result);
                 return t.Result.SendMessageAsync(conversation, new AVIMTextMessage("haha"), options: new AVIMSendOptions());
             });
        }
    }
}
