using System;
using System.Text.Json;
using Codexus.Cipher.Entities.G79;
using Codexus.Cipher.Entities.MPay;
using Codexus.Cipher.Entities.WPFLauncher;
using Codexus.Cipher.Protocol;
using Codexus.Cipher.Utils.Exception;
using OpenNEL.Entities;
using OpenNEL.Entities.Web;
using OpenNEL.Entities.Web.NEL;
using OpenNEL.Enums;
using OpenNEL.Manager;
using OpenNEL.Network;
using OpenNEL.type;
using Serilog;

namespace OpenNEL.Message.Login;

public class LoginMessage : IWsMessage
{
    public string Type => "login";

    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var msgType = root.TryGetProperty("type", out var tp) ? tp.GetString() : null;
        if (string.Equals(msgType, "cookie_login", StringComparison.OrdinalIgnoreCase))
        {
            var cookie = root.TryGetProperty("cookie", out var ck) ? ck.GetString() : string.Empty;
            LoginWithChannelAndType("netease", "cookie", cookie ?? string.Empty, Enums.Platform.Desktop, string.Empty);
            var last = UserManager.Instance.GetLastAvailableUser();
            var list = new System.Collections.ArrayList();
            if (last != null)
            {
                var u = UserManager.Instance.GetUserByEntityId(last.UserId);
                if (u != null) list.Add(new { type = "Success_login", entityId = u.UserId, channel = u.Channel });
            }
            var users = UserManager.Instance.GetUsersNoDetails();
            var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
            list.Add(new { type = "accounts", items });
            return list;
        }
        if (string.Equals(msgType, "login_4399", StringComparison.OrdinalIgnoreCase))
        {
            var account = root.TryGetProperty("account", out var acc) ? acc.GetString() : string.Empty;
            var password = root.TryGetProperty("password", out var pwd) ? pwd.GetString() : string.Empty;
            var sessionId = root.TryGetProperty("sessionId", out var sid) ? sid.GetString() : null;
            var captcha = root.TryGetProperty("captcha", out var cap) ? cap.GetString() : null;
            var req = new EntityPasswordRequest { Account = account ?? string.Empty, Password = password ?? string.Empty, CaptchaIdentifier = sessionId, Captcha = captcha };
            LoginWithChannelAndType("4399pc", "password", JsonSerializer.Serialize(req), Enums.Platform.Desktop, string.Empty);
            var last = UserManager.Instance.GetLastAvailableUser();
            var list = new System.Collections.ArrayList();
            if (last != null)
            {
                var u = UserManager.Instance.GetUserByEntityId(last.UserId);
                if (u != null) list.Add(new { type = "Success_login", entityId = u.UserId, channel = u.Channel });
            }
            var users = UserManager.Instance.GetUsersNoDetails();
            var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
            list.Add(new { type = "accounts", items });
            return list;
        }
        if (string.Equals(msgType, "login_x19", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var email = root.TryGetProperty("email", out var e) ? e.GetString() : string.Empty;
                var pwd = root.TryGetProperty("password", out var p) ? p.GetString() : string.Empty;
                var req = new EntityPasswordRequest { Account = email ?? string.Empty, Password = pwd ?? string.Empty };
                LoginWithChannelAndType("netease", "password", JsonSerializer.Serialize(req), Enums.Platform.Desktop, string.Empty);
                var last = UserManager.Instance.GetLastAvailableUser();
                var list = new System.Collections.ArrayList();
                if (last != null)
                {
                    var u = UserManager.Instance.GetUserByEntityId(last.UserId);
                    if (u != null) list.Add(new { type = "Success_login", entityId = u.UserId, channel = u.Channel });
                }
                var users = UserManager.Instance.GetUsersNoDetails();
                var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
                list.Add(new { type = "accounts", items });
                return list;
            }
            catch (Exception ex)
            {
                return new { type = "login_error", message = ex.Message ?? "登录失败" };
            }
        }
        if (string.Equals(msgType, "activate_account", StringComparison.OrdinalIgnoreCase))
        {
            var id = root.TryGetProperty("id", out var idp) ? idp.GetString() : (root.TryGetProperty("entityId", out var idp2) ? idp2.GetString() : null);
            if (string.IsNullOrWhiteSpace(id))
            {
                return new { type = "activate_account_error", message = "缺少id" };
            }
            var u = UserManager.Instance.GetUserByEntityId(id!);
            if (u == null)
            {
                return new { type = "activate_account_error", message = "账号不存在" };
            }
            try
            {
                if (!u.Authorized)
                {
                    LoginWithChannelAndType(u.Channel, u.Type, u.Details, u.Platform, string.Empty);
                }
                var list = new System.Collections.ArrayList();
                var users = UserManager.Instance.GetUsersNoDetails();
                var items = users.Select(x => new { entityId = x.UserId, channel = x.Channel, status = x.Authorized ? "online" : "offline" }).ToArray();
                list.Add(new { type = "Success_login", entityId = u.UserId, channel = u.Channel });
                list.Add(new { type = "accounts", items });
                return list;
            }
            catch (Exception ex)
            {
                return new { type = "activate_account_error", message = ex.Message ?? "激活失败" };
            }
        }
        EntityLoginRequest? entity = JsonSerializer.Deserialize<EntityLoginRequest>(root.GetRawText());
        if (entity == null || string.IsNullOrWhiteSpace(entity.Channel))
        {
            if (root.TryGetProperty("cookie", out var ck2) && ck2.ValueKind == JsonValueKind.String)
            {
                entity = new EntityLoginRequest { Channel = "netease", Type = "cookie", Details = ck2.GetString() ?? string.Empty, Platform = Enums.Platform.Desktop, Token = string.Empty };
            }
        }
        if (entity == null)
        {
            return new Entity("login", "Success");
        }
        try
        {
            switch (entity.Channel)
            {
                case "send_code":
                    return HandleSendCode(entity);
                case "active":
                    return HandleActive(entity);
                case "active_with_captcha":
                    return HandleActiveWithCaptcha(entity);
                default:
                    return HandleDefaultLogin(entity);
            }
        }
        catch (VerifyException exception)
        {
            return new { type = "login_error", message = exception.Message };
        }
        catch (CaptchaException exception2)
        {
            return new { type = "login_error", message = exception2.Message };
        }
        catch (Exception exception3)
        {
            Log.Error(exception3, "Login failed");
            var msg = exception3.Message ?? string.Empty;
            if (msg.IndexOf("token expired", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new { type = "login_error", message = "当前cookie过期了" };
            }
            return new { type = "login_error", message = msg };
        }
    }

    private static object HandleSendCode(EntityLoginRequest entity)
    {
        if (!AppState.X19.MPay.SendSmsCodeAsync(entity.Details).GetAwaiter().GetResult())
        {
            throw new Exception("Failed to send SMS code");
        }
        return new Entity("login", EntityResponse.Success(36, string.Empty));
    }

    private static object HandleActive(EntityLoginRequest entity)
    {
        EntityUser? entityUser = UserManager.Instance.GetUserByEntityId(entity.Details);
        if (entityUser == null)
        {
            throw new Exception("User not found");
        }
        if (!entityUser.Authorized)
        {
            LoginWithChannelAndType(entityUser.Channel, entityUser.Type, entityUser.Details, entity.Platform, entity.Token);
        }
        var last = UserManager.Instance.GetLastAvailableUser();
        var list = new System.Collections.ArrayList();
        if (last != null)
        {
            var u = UserManager.Instance.GetUserByEntityId(last.UserId);
            if (u != null)
            {
                list.Add(new { type = "Success_login", entityId = u.UserId, channel = u.Channel });
            }
        }
            var users = UserManager.Instance.GetUsersNoDetails();
            var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
            list.Add(new { type = "accounts", items });
            return list;
        }

    private static object HandleActiveWithCaptcha(EntityLoginRequest entity)
    {
        EntityActiveWithCaptcha? entityActiveWithCaptcha = JsonSerializer.Deserialize<EntityActiveWithCaptcha>(entity.Details);
        if (entityActiveWithCaptcha == null)
        {
            throw new Exception("Invalid captcha");
        }
        EntityUser? userByEntityId = UserManager.Instance.GetUserByEntityId(entityActiveWithCaptcha.UserId);
        if (userByEntityId == null)
        {
            throw new Exception("User not found");
        }
        if (!userByEntityId.Authorized)
        {
            EntityPasswordRequest? entityPasswordRequest = JsonSerializer.Deserialize<EntityPasswordRequest>(userByEntityId.Details);
            if (entityPasswordRequest == null)
            {
                throw new Exception("Invalid user");
            }
            string details = JsonSerializer.Serialize(new EntityPasswordRequest
            {
                Account = entityPasswordRequest.Account,
                Password = entityPasswordRequest.Password,
                Captcha = entityActiveWithCaptcha.Captcha,
                CaptchaIdentifier = entityActiveWithCaptcha.Identifier
            });
            LoginWithChannelAndType(userByEntityId.Channel, userByEntityId.Type, details, entity.Platform, entity.Token);
        }
        var last = UserManager.Instance.GetLastAvailableUser();
        var list = new System.Collections.ArrayList();
        if (last != null)
        {
            var u = UserManager.Instance.GetUserByEntityId(last.UserId);
            if (u != null)
            {
                list.Add(new { type = "Success_login", entityId = u.UserId, channel = u.Channel });
            }
        }
        var users = UserManager.Instance.GetUsersNoDetails();
        var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
        list.Add(new { type = "accounts", items });
        return list;
    }

    private static object HandleDefaultLogin(EntityLoginRequest entity)
    {
        LoginWithChannelAndType(entity.Channel, entity.Type, entity.Details, entity.Platform, entity.Token);
        var last = UserManager.Instance.GetLastAvailableUser();
        var list = new System.Collections.ArrayList();
        if (last != null)
        {
            var u = UserManager.Instance.GetUserByEntityId(last.UserId);
            if (u != null)
            {
                list.Add(new { type = "Success_login", entityId = u.UserId, channel = u.Channel });
            }
        }
        var users = UserManager.Instance.GetUsersNoDetails();
        var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
        list.Add(new { type = "accounts", items });
        return list;
    }

    private static void LoginWithChannelAndType(string channel, string type, string details, Platform platform, string token)
    {
        if (channel != "netease")
        {
            if (channel == "4399pc" && type == "password")
            {
                LoginWith4399Password(details, platform, token);
            }
            return;
        }
        switch (type)
        {
            case "sms":
                LoginWithPhone(channel, details, platform, token);
                break;
            case "cookie":
                {
                    var (authOtpCookie, loginChannelCookie) = AppState.X19.LoginWithCookie(details);
                    Log.Information("Login with cookie: {UserId} Channel: {LoginChannel}", authOtpCookie.EntityId, loginChannelCookie);
                    Log.Debug("User details: {UserId},{Token}", authOtpCookie.Token, authOtpCookie.Token);
                    UserManager.Instance.AddUserToMaintain(authOtpCookie);
                    UserManager.Instance.AddUser(new EntityUser
                    {
                        UserId = authOtpCookie.EntityId,
                        Authorized = true,
                        AutoLogin = false,
                        Channel = loginChannelCookie,
                        Type = type,
                        Details = details
                    }, loginChannelCookie == "netease");
                }
                break;
            case "password":
                LoginWithEmail(details, platform, token);
                break;
        }
    }

    private static void LoginWithPhone(string channel, string details, Platform platform, string token)
    {
        EntityCodeRequest? entityCodeRequest = JsonSerializer.Deserialize<EntityCodeRequest>(details);
        if (entityCodeRequest == null)
        {
            throw new ArgumentException("Invalid phone login details");
        }
        WPFLauncher x = AppState.X19;
        EntitySmsTicket result = x.MPay.VerifySmsCodeAsync(entityCodeRequest.Phone, entityCodeRequest.Code).GetAwaiter().GetResult();
        if (result == null)
        {
            throw new Exception("Failed to verify SMS code");
        }
        EntityX19CookieRequest value = WPFLauncher.GenerateCookie(x.MPay.FinishSmsCodeAsync(entityCodeRequest.Phone, result.Ticket).GetAwaiter().GetResult() ?? throw new Exception("Failed to finish SMS code"), x.MPay.GetDevice());
        LoginWithChannelAndType(channel, "cookie", JsonSerializer.Serialize(value), platform, token);
    }

    private static void LoginWith4399Password(string details, Platform platform, string token)
    {
        EntityPasswordRequest? entityPasswordRequest = JsonSerializer.Deserialize<EntityPasswordRequest>(details);
        if (entityPasswordRequest == null)
        {
            throw new ArgumentException("Invalid password login details");
        }
        if (platform == Platform.Mobile)
        {
            Log.Information("Waiting for 4399 login...");
            string result = AppState.Com4399.LoginAndAuthorize(entityPasswordRequest.Account, entityPasswordRequest.Password).GetAwaiter().GetResult();
            var (authOtp4399Mobile, loginChannel4399Mobile) = AppState.X19.LoginWithCookie(result);
            Log.Information("Login with 4399 password on mobile: {UserId}", authOtp4399Mobile.EntityId);
            Log.Debug("User details: {UserId},{Token}", authOtp4399Mobile.EntityId, authOtp4399Mobile.Token);
            UserManager.Instance.AddUserToMaintain(authOtp4399Mobile);
            UserManager.Instance.AddUser(new EntityUser
            {
                UserId = authOtp4399Mobile.EntityId,
                Authorized = true,
                AutoLogin = false,
                Channel = loginChannel4399Mobile,
                Type = "password",
                Details = JsonSerializer.Serialize(new EntityPasswordRequest
                {
                    Account = entityPasswordRequest.Account,
                    Password = entityPasswordRequest.Password
                })
            });
            return;
        }
        AppState.Services!.X19.InitializeDeviceAsync().GetAwaiter().GetResult();
        string result2 = (!string.IsNullOrWhiteSpace(entityPasswordRequest.CaptchaIdentifier) && !string.IsNullOrWhiteSpace(entityPasswordRequest.Captcha))
            ? AppState.Services!.C4399.LoginWithPasswordAsync(entityPasswordRequest.Account, entityPasswordRequest.Password, entityPasswordRequest.CaptchaIdentifier!, entityPasswordRequest.Captcha!).GetAwaiter().GetResult()
            : AppState.Services!.C4399.LoginWithPasswordAsync(entityPasswordRequest.Account, entityPasswordRequest.Password).GetAwaiter().GetResult();
        var (authOtp4399Pc, loginChannel4399Pc) = AppState.X19.LoginWithCookie(result2);
        Log.Information("Login with password: {UserId} Channel: {LoginChannel}", authOtp4399Pc.EntityId, loginChannel4399Pc);
        Log.Debug("User details: {UserId},{Token}", authOtp4399Pc.EntityId, authOtp4399Pc.Token);
        UserManager.Instance.AddUserToMaintain(authOtp4399Pc);
        UserManager.Instance.AddUser(new EntityUser
        {
            UserId = authOtp4399Pc.EntityId,
            Authorized = true,
            AutoLogin = false,
            Channel = loginChannel4399Pc,
            Type = "password",
            Details = JsonSerializer.Serialize(new EntityPasswordRequest
            {
                Account = entityPasswordRequest.Account,
                Password = entityPasswordRequest.Password
            })
        });
    }

    private static void LoginWithEmail(string details, Platform platform, string token)
    {
        EntityPasswordRequest? entityPasswordRequest = JsonSerializer.Deserialize<EntityPasswordRequest>(details);
        if (entityPasswordRequest == null)
        {
            throw new ArgumentException("Invalid email login details");
        }
        WPFLauncher x = AppState.X19;
        EntityX19CookieRequest entityX19CookieRequest = WPFLauncher.GenerateCookie(x.LoginWithEmailAsync(entityPasswordRequest.Account, entityPasswordRequest.Password).GetAwaiter().GetResult(), x.MPay.GetDevice());
        var (authOtpEmail, loginChannelEmail) = x.LoginWithCookie(entityX19CookieRequest);
        Log.Information("Login with email: {UserId} Channel: {LoginChannel}", authOtpEmail.EntityId, loginChannelEmail);
        Log.Debug("User details: {UserId},{Token}", authOtpEmail.EntityId, authOtpEmail.Token);
        UserManager.Instance.AddUserToMaintain(authOtpEmail);
        UserManager.Instance.AddUser(new EntityUser
        {
            UserId = authOtpEmail.EntityId,
            Authorized = true,
            AutoLogin = false,
            Channel = loginChannelEmail,
            Type = "cookie",
            Details = JsonSerializer.Serialize(entityX19CookieRequest)
        });
    }
}
