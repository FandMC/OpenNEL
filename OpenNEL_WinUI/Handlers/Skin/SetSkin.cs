/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.type;
using Serilog;
using System.Text.Json;

namespace OpenNEL_WinUI.Handlers.Skin;

public class SetSkinResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public bool NotLogin { get; set; }
}

public class SetSkin
{
    public SetSkinResult Execute(string entityId)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new SetSkinResult { NotLogin = true };
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
            return new SetSkinResult { Success = succ, Message = msg };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "设置皮肤失败");
            return new SetSkinResult { Success = false, Message = "设置失败" };
        }
    }
}
