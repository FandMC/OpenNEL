using OpenNEL.network;
using OpenNEL.type;
using System.Text.Json;
using Serilog;
namespace OpenNEL.HandleWebSocket.Login;

internal class DeleteAccountHandler : IWsHandler
{
    public string Type => "delete_account";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var id = root.TryGetProperty("entityId", out var idProp) ? idProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(id))
        {
            return new { type = "delete_error", message = "entityId为空" };
        }
        if (AppState.Accounts.TryRemove(id, out _))
        {
            if(AppState.Debug)Log.Information("已删除账号: {Id}", id);
            
            if (AppState.SelectedAccountId == id) AppState.SelectedAccountId = null;
            AppState.Auths.TryRemove(id, out _);
        }
        else
        {
            Log.Warning("删除账号失败，未找到: {Id}", id);
        }
        var items = AppState.Accounts.Select(kv => new { entityId = kv.Key, channel = kv.Value }).ToArray();
        return new { type = "accounts", items };
    }
}
