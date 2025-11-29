using System.Text.Json;
using OpenNEL.Manager;
using OpenNEL.Network;
using OpenNEL.type;
using Serilog;

namespace OpenNEL.Message.Web;

internal class UpdateSettingsMessage : IWsMessage
{
    public string Type => "update_settings";
    public Task<object?> ProcessAsync(JsonElement root)
    {
        try
        {
            var mode = root.TryGetProperty("mode", out var m) ? m.GetString() : null;
            var color = root.TryGetProperty("color", out var c) ? c.GetString() : null;
            var image = root.TryGetProperty("image", out var i) ? i.GetString() : null;
            if (string.IsNullOrWhiteSpace(mode)) mode = "image";
            if (mode != "color" && mode != "image") mode = "image";
            var data = new SettingData { ThemeMode = mode, ThemeColor = color ?? "#181818", ThemeImage = image ?? string.Empty };
            SettingManager.Instance.Update(data);
            var upd = new { type = "settings_updated" };
            var s = SettingManager.Instance.Get();
            var payload = new { type = "settings", mode = s.ThemeMode, color = s.ThemeColor, image = s.ThemeImage };
            return Task.FromResult<object?>(new object[] { upd, payload });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新设置失败");
            return Task.FromResult<object?>(new { type = "update_settings_error", message = ex.Message });
        }
    }
}
