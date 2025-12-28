using DotNetty.Buffers;
using OpenNEL.SDK.Event;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Utils;
using Serilog;

namespace OpenNEL.IRC;

public static class IrcEventHandler
{
    public static void Register(Func<string> tokenProvider, string hwid)
    {
        IrcManager.TokenProvider = tokenProvider;
        IrcManager.Hwid = hwid;
        
        foreach (var ch in MessageChannels.AllVersions)
        {
            EventManager.Instance.RegisterHandler<EventLoginSuccess>(ch, OnLogin);
            EventManager.Instance.RegisterHandler<EventConnectionClosed>(ch, OnDisconnect);
        }
    }

    static void OnLogin(EventLoginSuccess e)
    {
        var name = e.Connection.NickName;
        if (string.IsNullOrEmpty(name)) return;
        
        var client = IrcManager.GetOrCreate(e.Connection);
        client.ChatReceived -= OnChat;
        client.ChatReceived += OnChat;
        client.Start();
        
        Task.Run(() => client.ReportPlayer(name));
    }

    static void OnDisconnect(EventConnectionClosed e)
    {
        IrcManager.Remove(e.Connection);
    }

    static void OnChat(object? sender, IrcChatEventArgs e)
    {
        try
        {
            var msg = $"§b[OpenNEL {e.Username}]§r <{e.PlayerName}> {e.Message}";
            var buf = Unpooled.Buffer();
            buf.WriteVarInt(108);
            var bytes = System.Text.Encoding.UTF8.GetBytes(msg);
            buf.WriteByte(0x08);
            buf.WriteShort(bytes.Length);
            buf.WriteBytes(bytes);
            buf.WriteBoolean(false);
            
            // 广播给所有连接
            foreach (var client in IrcManager.GetAllClients())
            {
                client.Connection.ClientChannel?.WriteAndFlushAsync(buf.Duplicate());
            }
        }
        catch (Exception ex) { Log.Error(ex, "[IRC] 发送失败"); }
    }
}
