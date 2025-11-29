using OpenNEL.Network;
using OpenNEL.type;
using OpenNEL.Utils;
using System.Text.Json;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Serilog;
using OpenNEL.Manager;

namespace OpenNEL.Message.Game;

internal class CreateRoleNamedMessage : IWsMessage
{
    public string Type => "create_role_named";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var serverId = root.TryGetProperty("serverId", out var sid) ? sid.GetString() : null;
        var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        var auth = new Codexus.OpenSDK.Entities.X19.X19AuthenticationOtp { EntityId = last.UserId, Token = last.AccessToken };
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(name))
        {
            return new { type = "server_roles_error", message = "参数错误" };
        }
        try
        {
            if(AppState.Debug) Log.Information("创建角色请求: serverId={ServerId}, name={Name}, account={AccountId}", serverId, name, auth.EntityId);
            await CreateCharacterByIdAsync(auth, serverId, name);
            if(AppState.Debug)Log.Information("角色创建成功: serverId={ServerId}, name={Name}", serverId, name);
            var roles = await GetServerRolesByIdAsync(auth, serverId);
            if(AppState.Debug)Log.Information("角色列表返回: count={Count}, serverId={ServerId}", roles.Length, serverId);
            var items = roles.Select(r => new { id = r.Name, name = r.Name }).ToArray();
            return new { type = "server_roles", items, serverId, createdName = name };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "角色创建失败: serverId={ServerId}, name={Name}", serverId, name);
            return new { type = "server_roles_error", message = "创建角色失败" };
        }
    }

    private static async Task CreateCharacterByIdAsync(Codexus.OpenSDK.Entities.X19.X19AuthenticationOtp authOtp, string serverId, string name)
    {
        await authOtp.Api<EntityCreateCharacter, JsonElement>(
            "/game-character",
            new EntityCreateCharacter
            {
                GameId = serverId,
                UserId = authOtp.EntityId,
                Name = name
            });
    }

    private static async Task<EntityGameCharacter[]> GetServerRolesByIdAsync(Codexus.OpenSDK.Entities.X19.X19AuthenticationOtp authOtp, string serverId)
    {
        var roles = await authOtp.Api<EntityQueryGameCharacters, Entities<EntityGameCharacter>>(
            "/game-character/query/user-game-characters",
            new EntityQueryGameCharacters
            {
                GameId = serverId,
                UserId = authOtp.EntityId
            });
        return roles.Data;
    }
}
