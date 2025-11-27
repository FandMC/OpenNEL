using Codexus.Cipher.Protocol;

namespace OpenNEL.type;
using System.Collections.Concurrent;
using Codexus.OpenSDK.Entities.X19;

internal static class AppState
{
    public static readonly Com4399 Com4399 = new Com4399();

    public static readonly G79 G79 = new G79();

    public static readonly WPFLauncher X19 = new WPFLauncher();
    
    public static Services? Services;
    public static ConcurrentDictionary<string, string> Accounts { get; } = new();
    public static ConcurrentDictionary<string, X19AuthenticationOtp> Auths { get; } = new();
    public static string? SelectedAccountId;
    public static ConcurrentDictionary<string, ChannelInfo> Channels { get; } = new();
    public static ConcurrentDictionary<string, (string account, string password)> PendingCaptchas { get; } = new();
    public static ConcurrentDictionary<string, bool> WaitRestartPlugins { get; } = new();
    public static bool Debug;
}
