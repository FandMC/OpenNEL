using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using OpenNEL.network;
using OpenNEL.type;
using OpenNEL.Utils; 
using System.Text.Json;
using Serilog;

namespace OpenNEL.HandleWebSocket.Game;

internal class OpenServerHandler : IWsHandler
{
    public string Type => "open_server";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var serverId = root.TryGetProperty("serverId", out var sid) ? sid.GetString() : null;
        var sel = AppState.SelectedAccountId;
        if (string.IsNullOrEmpty(sel) || !AppState.Auths.TryGetValue(sel, out var auth))
        {
            return new { type = "notlogin" };
        }
        if (string.IsNullOrWhiteSpace(serverId))
        {
            return new { type = "server_roles_error", message = "参数错误" };
        }
        try
        {
            if(AppState.Debug)Log.Information("打开服务器: serverId={ServerId}, account={AccountId}", serverId, auth.EntityId);
            var roles = await auth.Api<EntityQueryGameCharacters, Codexus.Cipher.Entities.Entities<Codexus.Cipher.Entities.WPFLauncher.NetGame.EntityGameCharacter>>(
                "/game-character/query/user-game-characters",
                new EntityQueryGameCharacters
                {
                    GameId = serverId,
                    UserId = auth.EntityId
                });
            var items = roles.Data.Select(r => new { id = r.Name, name = r.Name }).ToArray();
            return new { type = "server_roles", items, serverId };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取服务器角色失败: serverId={ServerId}", serverId);
            return new { type = "server_roles_error", message = "获取失败" };
        }
    }
}
