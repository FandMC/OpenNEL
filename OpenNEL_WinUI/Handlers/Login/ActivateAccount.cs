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
using System;
using System.Text.Json;
using Codexus.Cipher.Utils.Exception;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.Utils;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Login
{
    public class ActivateAccount
    {
        public object Execute(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return new { type = "activate_account_error", message = "缺少id" };
            var u = UserManager.Instance.GetUserByEntityId(id!);
            if (u == null)
            {
                return new { type = "activate_account_error", message = "账号不存在" };
            }
            try
            {
                if (!u.Authorized)
                {
                    var req = JsonSerializer.Deserialize<Entities.Web.NEL.EntityPasswordRequest>(u.Details);
                    if (req == null)
                    {
                        throw new Exception("无法解析登录信息");
                    }
                    var result = new Login4399().Execute(req.Account, req.Password);
                    var tProp = result?.GetType().GetProperty("type");
                    var tVal = tProp?.GetValue(result) as string;
                    if (tVal == "captcha_required")
                    {
                        Log.Information("[ActivateAccount] 需要验证码");
                        return result;
                    }
                    if (tVal == "login_4399_error")
                    {
                        var mProp = result?.GetType().GetProperty("message");
                        var msg = mProp?.GetValue(result) as string ?? "登录失败";
                        Log.Error("[ActivateAccount] 登录失败: {Msg}", msg);
                        return result;
                    }
                    u.Authorized = true;
                    UserManager.Instance.MarkDirtyAndScheduleSave();
                }
                var list = new System.Collections.ArrayList();
                var items = GetAccount.GetAccountItems();
                list.Add(new { type = "Success_login", entityId = u.UserId, channel = u.Channel });
                list.Add(new { type = "accounts", items });
                return list;
            }
            catch (CaptchaException)
            {
                return HandleCaptchaRequired(u);
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                var lower = msg.ToLowerInvariant();
                if (lower.Contains("parameter") && lower.Contains("'s'"))
                {
                    return HandleCaptchaRequired(u);
                }
                return new { type = "activate_account_error", message = msg.Length == 0 ? "激活失败" : msg };
            }
        }

        private object HandleCaptchaRequired(Entities.Web.EntityUser u)
        {
            try
            {
                var req = JsonSerializer.Deserialize<Entities.Web.NEL.EntityPasswordRequest>(u.Details);
                var captchaSid = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 8);
                var url = "https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + captchaSid;

                try
                {
                    var recognizedCaptcha = CaptchaRecognitionService.RecognizeFromUrlAsync(url).GetAwaiter().GetResult();
                    if (!string.IsNullOrWhiteSpace(recognizedCaptcha))
                    {
                        Log.Information("[ActivateAccount] 验证码自动识别成功: {Captcha}，正在重试登录", recognizedCaptcha);
                        var result = new Login4399().Execute(req?.Account, req?.Password, captchaSid, recognizedCaptcha);
                        var tProp = result?.GetType().GetProperty("type");
                        var tVal = tProp?.GetValue(result) as string;
                        if (tVal != "captcha_required" && tVal != "login_4399_error")
                        {
                            u.Authorized = true;
                            UserManager.Instance.MarkDirtyAndScheduleSave();
                        }
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("[ActivateAccount] 验证码自动识别失败: {Error}", ex.Message);
                }

                Log.Information("[ActivateAccount] 验证码自动识别失败，需要手动输入");
                return new { type = "captcha_required", account = req?.Account ?? string.Empty, password = req?.Password ?? string.Empty, sessionId = captchaSid, captchaUrl = url };
            }
            catch
            {
                return new { type = "captcha_required" };
            }
        }
    }
}
