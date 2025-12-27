using System.Text;
using System.Text.Json;
using Codexus.OpenSDK.Cipher;
using Codexus.OpenSDK.Entities.MPay;
using Codexus.OpenSDK.Entities.X19;
using Codexus.OpenSDK.Extensions;
using Codexus.OpenSDK.Http;

namespace Codexus.OpenSDK;

public class X19() : UniSdkMPay(Projects.DesktopMinecraft, GetLatestVersion())
{
    private static readonly HttpWrapper ObtCore = new(Urls.X19ObtCore,
        options => { options.WithUserAgent("WPFLauncher/0.0.0.0"); });

    private static readonly HttpWrapper ApiGateway = new(Urls.X19ApiGateway,
        options => { options.WithUserAgent("WPFLauncher/0.0.0.0"); });

    private readonly MgbSdk _sdk = new("x19");

    public async Task<(X19AuthenticationOtp, string)> ContinueAsync(MPayUserWrapper? user, MPayDevice? device)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(device);

        var cookie = new X19SAuthJsonWrapper
        {
            Json = JsonSerializer.Serialize(new X19SAuthJson
            {
                SdkUid = user.User.Id,
                SessionId = user.User.Token,
                Udid = Guid.NewGuid().ToString("N").ToUpper(),
                DeviceId = device.Id
            })
        };

        return await ContinueAsync(cookie);
    }

    public async Task<(X19AuthenticationOtp, string)> ContinueAsync(string json)
    {
        X19SAuthJsonWrapper entity;
        try
        {
            entity = JsonSerializer.Deserialize<X19SAuthJsonWrapper>(json)!;
        }
        catch (Exception)
        {
            entity = new X19SAuthJsonWrapper
            {
                Json = json
            };
        }

        return await ContinueAsync(entity);
    }

    public async Task<(X19AuthenticationOtp, string)> ContinueAsync(X19SAuthJsonWrapper wrapper)
    {
        var authJson = JsonSerializer.Deserialize<X19SAuthJson>(wrapper.Json)!;
        if (authJson.LoginChannel != "netease") await _sdk.AuthenticateSessionAsync(wrapper.Json);

        var otp = await LoginOtpAsync(wrapper);
        var user = await AuthenticationOtpAsync(wrapper, otp);
        await InterconnectionApi.LoginStart(user.EntityId, user.Token);

        return (user, authJson.LoginChannel);
    }

    private async Task<X19AuthenticationOtp> AuthenticationOtpAsync(X19SAuthJsonWrapper wrapper, X19LoginOtp otp)
    {
        var authJson = JsonSerializer.Deserialize<X19SAuthJson>(wrapper.Json)!;
        var detail = new X19AuthenticationDetail
        {
            Udid = authJson.Udid,
            AppVersion = GameVersion,
            PayChannel = authJson.AppChannel
        };

        var data = JsonSerializer.Serialize(new X19AuthenticationData
        {
            SaData = JsonSerializer.Serialize(detail),
            AuthJson = wrapper.Json,
            Version = new X19AuthenticationVersion
            {
                Version = GameVersion
            },
            Aid = otp.Aid.ToString(),
            OtpToken = otp.OtpToken,
            LockTime = otp.LockTime
        });

        var encrypted = HttpCipher.HttpEncrypt(Encoding.UTF8.GetBytes(data));
        var message = await ObtCore.PostAsync("/authentication-otp", encrypted);

        var response = HttpCipher.HttpDecrypt(await message.Content.ReadAsByteArrayAsync());
        if (response == null) throw new Exception("Cannot decrypt data");

        var entity = JsonSerializer.Deserialize<X19Entity<X19AuthenticationOtp>>(response)!;
        return entity.Data!;
    }

    private async Task<X19LoginOtp> LoginOtpAsync(X19SAuthJsonWrapper wrapper)
    {
        var response = await ObtCore.PostAsync("/login-otp", JsonSerializer.Serialize(wrapper));

        var json = await response.Content.ReadAsStringAsync();
        var entity = JsonSerializer.Deserialize<X19Entity<JsonElement?>>(json);

        if (entity == null)
            throw new Exception("Failed to deserialize: " + json);

        if (entity.Code != 0 || entity.Data == null)
            throw new Exception("Failed to deserialize: " + entity.Message);

        return JsonSerializer.Deserialize<X19LoginOtp>(entity.Data.Value.GetRawText())!;
    }

    private static string GetLatestVersion()
    {
        var versions = GetPatchVersionsAsync().GetAwaiter().GetResult();
        return versions.Keys.Last();
    }

    public static async Task<Dictionary<string, X19PatchVersion>> GetPatchVersionsAsync()
    {
        HttpWrapper client = new();

        var response = await client.GetAsync(Urls.X19PatchListUrl);
        var versions = await response.Content.ReadAsStringAsync();
        var lastIndex = versions.LastIndexOf(',');
        var json = string.Concat("{", versions[..lastIndex], "}");

        return JsonSerializer.Deserialize<Dictionary<string, X19PatchVersion>>(json)!;
    }

    public static async Task<HttpResponseMessage> ObtCorePostAsync(string url, string body, string userId,
        string userToken)
    {
        return await PostAsync(url, body, userId, userToken, ObtCore);
    }

    public static async Task<HttpResponseMessage> ApiPostAsync(string url, string body, string userId,
        string userToken)
    {
        return await PostAsync(url, body, userId, userToken, ApiGateway);
    }

    public static async Task<HttpResponseMessage> PostAsync(string url, string body, string userId, string userToken,
        HttpWrapper http)
    {
        return await http.PostAsync(url, body,
            configure: options => { options.AddHeaders(DynamicToken.Compute(url, body, userId, userToken)); });
    }

    public static class InterconnectionApi
    {
        public static async Task LoginStart(string userId, string userToken)
        {
            await ObtCorePostAsync("/interconn/web/game-play-v2/login-start", "{\"strict_mode\":true}", userId,
                userToken);
        }

        public static async Task GameStartAsync(
            string userId,
            string userToken,
            string gameId,
            X19EnumGameType gameType = X19EnumGameType.NetGame,
            string skin = "10000")
        {
            var request = new X19GameStart
            {
                GameType = gameType.ToOrdinalString(),
                GameId = gameId,
                ItemList = [skin]
            };

            var json = JsonSerializer.Serialize(request);
            await ObtCorePostAsync("/interconn/web/game-play-v2/start", json, userId, userToken);
        }
    }
}