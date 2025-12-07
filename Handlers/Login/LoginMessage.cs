using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
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
using OpenNEL.type;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Login
{
    public static class LoginHandler
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

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
            catch (Exception)
            {
                cookie1 = new EntityX19CookieRequest()
                {
                    Json = cookie
                };
            }
            return AppState.X19.LoginWithCookie(cookie1);
        }
    }
}
