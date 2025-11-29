using Codexus.Cipher.Protocol;

namespace OpenNEL.type;
using System.Collections.Concurrent;
using Codexus.OpenSDK.Entities.X19;

internal static class AppState
{
    public static readonly Com4399 Com4399 = new Com4399();

    public static readonly G79 G79 = new G79();
    private static WPFLauncher? _x19;
    public static WPFLauncher X19 => _x19 ??= new WPFLauncher();
    
    public static Services? Services;
    public static ConcurrentDictionary<string, bool> WaitRestartPlugins { get; } = new();
    public static bool Debug;
}
