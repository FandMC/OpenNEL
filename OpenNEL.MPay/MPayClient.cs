using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using OpenNEL.Core.Extensions;
using OpenNEL.Core.Http;
using OpenNEL.Core.Utils;
using OpenNEL.MPay.Entities;
using OpenNEL.MPay.Exceptions;

namespace OpenNEL.MPay;

public class MPayClient : IDisposable
{
    private readonly EntityDevice _device;
    private readonly HttpWrapper _client = new();
    private readonly HttpWrapper _service = new("https://service.mkey.163.com");

    public readonly string Unique;

    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string GameId { get; }
    public string GameVersion { get; }

    public MPayClient(string gameId, string gameVersion)
    {
        GameId = gameId;
        GameVersion = gameVersion;
        Unique = CreateOrLoadUnique(gameId);
        _device = CreateOrLoadDeviceAsync(gameId, gameVersion).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _client.Dispose();
        _service.Dispose();
        GC.SuppressFinalize(this);
    }

    public EntityDevice GetDevice() => _device;

    public async Task Configure(string appKey, string channel)
    {
        var query = new ParameterBuilder()
            .Append("sub_app_key", "")
            .Append("api_ver", "2")
            .Append("gdpr", "0")
            .Append("app_channel", channel)
            .Append("sdk_version", "c1.0.0")
            .Append("app_key", appKey)
            .Append("device_id", Unique.ToUpper())
            .FormUrlEncode();
        (await _client.GetAsync("https://analytics.mpay.netease.com/config?" + query)).EnsureSuccessStatusCode();
    }

    private static string CreateOrLoadUnique(string gameId)
    {
        var fileName = gameId + "-guid.cds";
        var data = LoadFromFile(fileName);
        if (data != null)
        {
            return Encoding.UTF8.GetString(data);
        }
        return CreateUnique(fileName);
    }

    private static string CreateUnique(string fileName)
    {
        var unique = Guid.NewGuid().ToString().Replace("-", "");
        SaveToFile(fileName, unique);
        return unique;
    }

    private async Task<EntityDevice> CreateOrLoadDeviceAsync(string gameId, string gameVersion)
    {
        var fileName = gameId + "-device.cds";
        var data = LoadFromFile(fileName);
        if (data == null)
        {
            return await CreateDeviceAsync(gameId, gameVersion, fileName);
        }
        return JsonSerializer.Deserialize<EntityDeviceResponse>(Encoding.UTF8.GetString(data))!.Device;
    }

    private async Task<EntityDevice> CreateDeviceAsync(string gameId, string gameVersion, string fileName)
    {
        var response = await _service.PostAsync(
            "/mpay/games/" + gameId + "/devices",
            BuildDeviceParams().Append("unique_id", Unique).FormUrlEncode(),
            "application/x-www-form-urlencoded");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        SaveToFile(fileName, content);
        return JsonSerializer.Deserialize<EntityDeviceResponse>(content)!.Device;
    }

    public async Task<EntityMPayUserResponse> LoginWithEmailAsync(string email, string password)
    {
        var parameters = new EntityUsersParameters
        {
            Password = password.EncodeMd5(),
            Unique = Unique,
            Username = email
        };
        var encrypted = JsonSerializer.Serialize(parameters, DefaultOptions)
            .EncodeAes(_device.Key.DecodeHex())
            .EncodeHex();

        var response = await _service.PostAsync(
            $"/mpay/games/{GameId}/devices/{_device.Id}/users",
            BuildBaseParams()
                .Append("opt_fields", "nickname,avatar,realname_status,mobile_bind_status,mask_related_mobile,related_login_status")
                .Append("params", encrypted)
                .Append("un", email.EncodeBase64())
                .FormUrlEncode(),
            "application/x-www-form-urlencoded");

        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            var verifyResponse = JsonSerializer.Deserialize<EntityVerifyResponse>(content);
            if (verifyResponse != null && verifyResponse.Code == 1351)
            {
                throw new VerifyException(content);
            }
            throw new Exception("Failed to login with email, response: " + content);
        }
        return JsonSerializer.Deserialize<EntityMPayUserResponse>(content)!;
    }

    public async Task<bool> SendSmsCodeAsync(string phoneNumber)
    {
        var response = await _service.PostAsync(
            "/mpay/api/users/login/mobile/get_sms",
            BuildBaseParams()
                .Append("device_id", _device.Id)
                .Append("mobile", phoneNumber)
                .FormUrlEncode(),
            "application/x-www-form-urlencoded");
        return response.IsSuccessStatusCode;
    }

    public async Task<EntitySmsTicket?> VerifySmsCodeAsync(string phoneNumber, string code)
    {
        var response = await _service.PostAsync(
            "/mpay/api/users/login/mobile/verify_sms",
            BuildBaseParams()
                .Append("device_id", _device.Id)
                .Append("mobile", phoneNumber)
                .Append("smscode", code)
                .Append("up_content", "")
                .FormUrlEncode(),
            "application/x-www-form-urlencoded");
        
        var content = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize<EntitySmsTicket>(content);
        }
        return null;
    }

    public async Task<EntityMPayUserResponse?> FinishSmsCodeAsync(string phoneNumber, string ticket)
    {
        var encodedPhone = phoneNumber.EncodeBase64();
        var response = await _service.PostAsync(
            "/mpay/api/users/login/mobile/finish?un=" + encodedPhone,
            BuildBaseParams()
                .Append("device_id", _device.Id)
                .Append("opt_fields", "nickname,avatar,realname_status,mobile_bind_status,mask_related_mobile,related_login_status")
                .Append("ticket", ticket)
                .FormUrlEncode(),
            "application/x-www-form-urlencoded");
        
        var content = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize<EntityMPayUserResponse>(content);
        }
        return null;
    }

    private ParameterBuilder BuildBaseParams()
    {
        return new ParameterBuilder()
            .Append("app_channel", "netease")
            .Append("app_mode", "2")
            .Append("app_type", "games")
            .Append("arch", "win_x64")
            .Append("cv", "c4.2.0")
            .Append("mcount_app_key", "EEkEEXLymcNjM42yLY3Bn6AO15aGy4yq")
            .Append("mcount_transaction_id", "0")
            .Append("process_id", $"{Environment.ProcessId}")
            .Append("sv", "10.0.22621")
            .Append("updater_cv", "c1.0.0")
            .Append("game_id", GameId)
            .Append("gv", GameVersion);
    }

    private ParameterBuilder BuildDeviceParams()
    {
        return BuildBaseParams()
            .Append("brand", "Microsoft")
            .Append("device_model", "pc_mode")
            .Append("device_name", "PC-" + StringGenerator.GenerateRandomString(12))
            .Append("device_type", "Computer")
            .Append("init_urs_device", "0")
            .Append("mac", StringGenerator.GenerateRandomMacAddress())
            .Append("resolution", "1920x1080")
            .Append("system_name", "windows")
            .Append("system_version", "10.0.22621");
    }

    public static void SaveToFile(string filename, string content)
    {
        File.WriteAllText(filename, content);
    }

    public static byte[]? LoadFromFile(string filename)
    {
        try
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            using var fs = new FileStream(filename, FileMode.Open);
            var buffer = new byte[fs.Length];
            fs.ReadExactly(buffer, 0, buffer.Length);
            return buffer;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
