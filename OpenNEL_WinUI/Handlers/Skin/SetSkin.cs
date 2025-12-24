/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.type;
using Serilog;
using System.Text.Json;

namespace OpenNEL_WinUI.Handlers.Skin;

public class SetSkin
{
    public object Execute(string entityId)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        try
        {
            var r = AppState.X19.SetSkin(last.UserId, last.AccessToken, entityId);
            var t = r?.GetType();
            var codeObj = t?.GetProperty("Code")?.GetValue(r);
            var msg = t?.GetProperty("Message")?.GetValue(r) as string ?? string.Empty;
            int code = 0;
            if (codeObj != null)
            {
                try { code = System.Convert.ToInt32(codeObj); } catch { }
            }
            var succ = code == 0;
            Log.Debug("设置皮肤响应: code={Code} message={Message}", code, msg);
            try { Log.Debug("设置皮肤响应对象: {Json}", JsonSerializer.Serialize(r)); } catch { }
            return new { type = "set_skin_result", success = succ, message = msg };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "设置皮肤失败");
            return new { type = "set_skin_error", message = "设置失败" };
        }
    }
}
