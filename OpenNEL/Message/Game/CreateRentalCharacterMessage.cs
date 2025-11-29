using OpenNEL.Network;
using OpenNEL.Manager;
using System.Text.Json;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using OpenNEL.Utils;

namespace OpenNEL.Message.Game;

internal class CreateRentalCharacterMessage : IWsMessage
{
    public string Type => "create_rental_character";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var serverId = root.TryGetProperty("serverId", out var sid) ? sid.GetString() : null;
        var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(name))
        {
            return new { type = "create_rental_error", message = "参数错误" };
        }
        var auth = new Codexus.OpenSDK.Entities.X19.X19AuthenticationOtp { EntityId = last.UserId, Token = last.AccessToken };
        try
        {
            await auth.Api<EntityCreateCharacter, JsonElement>(
                "/game-character",
                new EntityCreateCharacter
                {
                    GameId = serverId!,
                    UserId = last.UserId,
                    Name = name!
                });

            var roles = await auth.Api<EntityQueryGameCharacters, Entities<EntityGameCharacter>>(
                "/game-character/query/user-game-characters",
                new EntityQueryGameCharacters
                {
                    GameId = serverId!,
                    UserId = last.UserId
                });
            var items = roles.Data.Select(r => new { type = "rental_roles", id = r.Name, name = r.Name, serverId }).ToArray();
            return items;
        }
        catch (Exception ex)
        {
            return new { type = "create_rental_error", message = ex.Message ?? "创建失败" };
        }
    }
}
