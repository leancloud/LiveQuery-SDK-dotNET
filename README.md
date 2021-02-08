# LeanCloud LiveQuery for C#

## Deprecation Announcement

This project is deprecated, please refer to [Standard SDK](https://github.com/leancloud/csharp-sdk) for the latest updates.

## 官方文档

[实时数据同步 LiveQuery 开发指南](https://leancloud.cn/docs/livequery-guide.html)

## 在 Unity 中使用

访问 [Release](https://github.com/leancloud/LiveQuery-SDK-dotNET/releases) 获取最新版本的依赖

解压之后，在 dlls 文件夹下面的如下都需要引入到您的项目中：

```sh
AssemblyLister.dll
LeanCloud.Core.dll
LeanCloud.LiveQuery.dll
LeanCloud.Realtime.dll
LeanCloud.Storage.dll
Unity.Compat.dll
Unity.Tasks.dll
websocket-sharp.dll
```

## 初始化

```cs
public void initApp()
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
}
```

## 实例代码

### 单元测试

```cs
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

```
