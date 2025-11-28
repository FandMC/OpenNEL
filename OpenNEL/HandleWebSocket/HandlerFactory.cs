using OpenNEL.HandleWebSocket.Game;
using OpenNEL.HandleWebSocket.Login;
using OpenNEL.HandleWebSocket.Plugin;
using OpenNEL.network;

namespace OpenNEL.HandleWebSocket;

internal static class HandlerFactory
{
    private static readonly Dictionary<string, IWsHandler> Map;

    static HandlerFactory()
    {
        var handlers = new IWsHandler[]
        {
            new CookieLoginHandler(),
            new Login4399Handler(),
            new LoginX19Handler(),
            new DeleteAccountHandler(),
            new ListAccountsHandler(),
            new SelectAccountHandler(),
            new SearchServersHandler(),
            new ListServersHandler(),
            new OpenServerHandler(),
            new CreateRoleNamedHandler(),
            new JoinGameHandler(),
            new ListChannelsHandler(),
            new ShutdownGameHandler(),
            new GetFreeAccountHandler(),
            new ListInstalledPluginsHandler(),
            new UninstallPluginHandler(),
            new RestartGatewayHandler(),
            new InstallPluginHandler(),
            new UpdatePluginHandler(),
            new ListAvailablePluginsHandler(),
            new QueryGameSessionHandler()
        };
        Map = handlers.ToDictionary(h => h.Type, h => h);
    }

    public static IWsHandler? Get(string type)
    {
        return Map.TryGetValue(type, out var h) ? h : null;
    }
}
