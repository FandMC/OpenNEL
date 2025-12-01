using System;
using System.Text.Encodings.Web;
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
    private EntityLoginRequest? _entity;
    private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<object?> ProcessAsync(JsonElement root)
    {
        _entity = JsonSerializer.Deserialize<EntityLoginRequest>(root);
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
            var msg = exception3.Message;
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
            }, DefaultOptions);
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

    public static void LoginWithChannelAndType(string channel, string type, string details, Platform platform, string token)
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
                    var (authOtpCookie, loginChannelCookie) = LoginWithCookieAsync(details).GetAwaiter().GetResult();
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
        LoginWithChannelAndType(channel, "cookie", JsonSerializer.Serialize(value, DefaultOptions), platform, token);
    }

    private static void LoginWith4399Password(string details, Platform platform, string token)
    {
        EntityPasswordRequest? entityPasswordRequest = JsonSerializer.Deserialize<EntityPasswordRequest>(details);
        if (entityPasswordRequest == null)
        {
            throw new ArgumentException("Invalid password login details");
        }
        using Pc4399 pc = new Pc4399();
        string result2;
        try
        {
            result2 = pc.LoginWithPasswordAsync(entityPasswordRequest.Account, entityPasswordRequest.Password, entityPasswordRequest.CaptchaIdentifier, entityPasswordRequest.Captcha).GetAwaiter().GetResult();
        }
        catch (CaptchaException)
        {
            throw new CaptchaException("captcha required");
        }
        if (string.IsNullOrWhiteSpace(result2))
        {
            throw new Exception("cookie empty");
        }
        Codexus.Cipher.Entities.WPFLauncher.EntityX19CookieRequest cookieReq;
        try
        {
            cookieReq = JsonSerializer.Deserialize<Codexus.Cipher.Entities.WPFLauncher.EntityX19CookieRequest>(result2) ?? new Codexus.Cipher.Entities.WPFLauncher.EntityX19CookieRequest { Json = result2 };
        }
        catch
        {
            cookieReq = new Codexus.Cipher.Entities.WPFLauncher.EntityX19CookieRequest { Json = result2 };
        }
        var (entityAuthenticationOtp2, text) = AppState.X19.LoginWithCookie(cookieReq);
        Log.Information("Login with password: {UserId} Channel: {LoginChannel}", entityAuthenticationOtp2.EntityId, text);
        Log.Debug("User details: {UserId},{Token}", entityAuthenticationOtp2.EntityId, entityAuthenticationOtp2.Token);
        UserManager.Instance.AddUserToMaintain(entityAuthenticationOtp2);
        UserManager.Instance.AddUser(new EntityUser
        {
            UserId = entityAuthenticationOtp2.EntityId,
            Authorized = true,
            AutoLogin = false,
            Channel = text,
            Type = "password",
            Details = JsonSerializer.Serialize(new EntityPasswordRequest
            {
                Account = entityPasswordRequest.Account,
                Password = entityPasswordRequest.Password
            }, DefaultOptions)
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
    internal static async Task<(Codexus.Cipher.Entities.WPFLauncher.EntityAuthenticationOtp, string)> LoginWithCookieAsync(string cookie)
    {
        EntityX19CookieRequest cookie1;
        try
        {
            cookie1 = JsonSerializer.Deserialize<EntityX19CookieRequest>(cookie);
        }
        catch (Exception ex)
        {
            cookie1 = new EntityX19CookieRequest()
            {
                Json = cookie
            };
        }
        return AppState.X19.LoginWithCookie(cookie1);
    }
}
