# LeanCloud LiveQuery for C#

## 官方文档

[实时数据同步 LiveQuery 开发指南](https://leancloud.cn/docs/livequery-guide.html)

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
