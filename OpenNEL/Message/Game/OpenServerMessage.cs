using System.Text.Json;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Entities;
using OpenNEL.Network;
using OpenNEL.type;
using OpenNEL.Utils; 
using OpenNEL.Manager;
using Serilog;

namespace OpenNEL.Message.Game;

internal class OpenServerMessage : IWsMessage
{
    public string Type => "open_server";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var serverId = root.TryGetProperty("serverId", out var sid) ? sid.GetString() : null;
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        if (string.IsNullOrWhiteSpace(serverId))
        {
            return new { type = "server_roles_error", message = "参数错误" };
        }
        try
        {
            if(AppState.Debug)Log.Information("打开服务器: serverId={ServerId}, account={AccountId}", serverId, last.UserId);
            var auth = new Codexus.OpenSDK.Entities.X19.X19AuthenticationOtp { EntityId = last.UserId, Token = last.AccessToken };
            var roles = await auth.Api<EntityQueryGameCharacters, Entities<EntityGameCharacter>>(
                "/game-character/query/user-game-characters",
                new EntityQueryGameCharacters { GameId = serverId!, UserId = last.UserId });
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
