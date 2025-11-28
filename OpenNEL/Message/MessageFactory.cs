using OpenNEL.Message.Game;
using OpenNEL.Message.Login;
using OpenNEL.Message.Plugin;
using OpenNEL.Message.Connected;
using OpenNEL.Network;

namespace OpenNEL.Message;

internal static class MessageFactory
{
    private static readonly Dictionary<string, IWsMessage> Map;

    static MessageFactory()
    {
        var login = new LoginMessage();
        var handlers = new IWsMessage[]
        {
            login,
            new DeleteAccountMessage(),
            new DeleteUserMessage(),
            new GetAccountMessage(),
            new SelectAccountMessage(),
            new SearchServersMessage(),
            new ListServersMessage(),
            new OpenServerMessage(),
            new CreateRoleNamedMessage(),
            new JoinGameMessage(),
            new ListChannelsMessage(),
            new ShutdownGameMessage(),
            new GetFreeAccountMessage(),
            new ListInstalledPluginsMessage(),
            new UninstallPluginMessage(),
            new RestartGatewayMessage(),
            new InstallPluginMessage(),
            new UpdatePluginMessage(),
            new ListAvailablePluginsMessage(),
            new QueryGameSessionMessage()
        };
        Map = handlers.ToDictionary(h => h.Type, h => h);
        Map["login_4399"] = login;
        Map["login_x19"] = login;
        Map["cookie_login"] = login;
        Map["activate_account"] = login;
    }

    public static IWsMessage? Get(string type)
    {
        return Map.TryGetValue(type, out var h) ? h : null;
    }
}
