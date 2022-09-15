using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace LockBasedMessaging.Controllers;

[ApiController]
[Route("")]
public class MainController
{
    private static readonly ConcurrentDictionary<int, Lock?> dic = new();
    private static readonly ConcurrentDictionary<int, List<Notification>?> messageList = new();
    private static int id;

    [HttpGet("send/{roomId}/{message}")]
    public void SendMessage([FromRoute] int roomId, [FromRoute] string message)
    {
        if (!messageList.ContainsKey(roomId)) messageList[roomId] = new List<Notification>();
        messageList[roomId].Add(new Notification { id = id++, text = message });
        if (!dic.ContainsKey(roomId)) dic[roomId] = new Lock();
        lock (dic[roomId])
        {
            Monitor.PulseAll(dic[roomId]);
        }
    }


    [HttpGet("receive/{roomId}/{lastMessageId}")]
    public Notification[] ReceiveMessage([FromRoute] int roomId, [FromRoute] int lastMessageId)
    {
        if (!dic.ContainsKey(roomId)) dic[roomId] = new Lock();
        if (!messageList.ContainsKey(roomId)) messageList[roomId] = new List<Notification>();

        var b = messageList[roomId].Where(m => m.id > lastMessageId).ToArray();
        if (b.Length > 0) return b;

        lock (dic[roomId])
        {
            Monitor.Wait(dic[roomId], TimeSpan.FromSeconds(60));
        }

        return messageList[roomId].Where(m => m.id > lastMessageId).ToArray();
    }
}

public class Lock
{
}

public class Notification
{
    public int id { get; set; }
    public string text { get; set; }
}