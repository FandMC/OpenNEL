using OpenNEL.Network;
using OpenNEL.Manager;
using System.Text.Json;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.RentalGame;
using OpenNEL.Utils;

namespace OpenNEL.Message.Game;

internal class ListRentalServersMessage : IWsMessage
{
    public string Type => "list_rental_servers";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        var auth = new Codexus.OpenSDK.Entities.X19.X19AuthenticationOtp { EntityId = last.UserId, Token = last.AccessToken };
        try
        {
            const int pageSize = 15;
            var offset = root.TryGetProperty("offset", out var off) && off.ValueKind == JsonValueKind.Number ? off.GetInt32() : 0;
            var raw = await auth.Api<EntityQueryRentalGame, JsonElement>(
                "/rental-server/query/available-public-server",
                new EntityQueryRentalGame { Offset = offset, SortType = 0 });
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            var rentals = JsonSerializer.Deserialize<Entities<EntityRentalGame>>(raw.GetRawText(), options)!;
            var items = rentals.Data.Select(s => new { entityId = s.EntityId, name = string.IsNullOrWhiteSpace(s.ServerName) ? s.Name : s.ServerName }).ToArray();
            return new { type = "rentals", items };
        }
        catch (Exception ex)
        {
            return new { type = "rentals_error", message = ex.Message };
        }
    }
}
