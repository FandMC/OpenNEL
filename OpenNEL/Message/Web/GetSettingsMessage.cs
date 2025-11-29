using System.Text.Json;
using OpenNEL.Manager;
using OpenNEL.Network;

namespace OpenNEL.Message.Web;

internal class GetSettingsMessage : IWsMessage
{
    public string Type => "get_settings";
    public Task<object?> ProcessAsync(JsonElement root)
    {
        var s = SettingManager.Instance.Get();
        var payload = new { type = "settings", mode = s.ThemeMode, color = s.ThemeColor, image = s.ThemeImage };
        return Task.FromResult<object?>(payload);
    }
}
