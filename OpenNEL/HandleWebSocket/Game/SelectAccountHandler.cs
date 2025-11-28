using OpenNEL.network;
using OpenNEL.type;

using System.Text.Json;

namespace OpenNEL.HandleWebSocket.Game;

internal class SelectAccountHandler : IWsHandler
{
    public string Type => "select_account";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var id = root.TryGetProperty("entityId", out var idProp2) ? idProp2.GetString() : null;
        if (string.IsNullOrWhiteSpace(id) || !AppState.Auths.ContainsKey(id))
        {
            return new { type = "notlogin" };
        }
        AppState.SelectedAccountId = id;
        return new { type = "selected_account", entityId = id };
    }
}
