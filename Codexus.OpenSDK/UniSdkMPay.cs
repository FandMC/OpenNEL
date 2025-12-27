using System.Text;
using System.Text.Json;
using Codexus.OpenSDK.Entities.MPay;
using Codexus.OpenSDK.Exceptions;
using Codexus.OpenSDK.Extensions;
using Codexus.OpenSDK.Generator;
using Codexus.OpenSDK.Http;

namespace Codexus.OpenSDK;

public class UniSdkMPay : IDisposable
{
    private readonly string _id;
    private readonly HttpWrapper _service = new(Urls.ServiceMKey);

    public UniSdkMPay(string project, string version)
    {
        ProjectId = project;
        GameVersion = version;

        _id = LoadOrCreateId();
    }

    public string ProjectId { get; }
    public string GameVersion { get; }
    public MPayDevice? Device { get; private set; }

    public void Dispose()
    {
        _service.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<MPayDevice> InitializeDeviceAsync()
    {
        if (Device != null)
            return Device;

        Device = await LoadOrCreateDeviceAsync();
        return Device;
    }

    private async Task<MPayDevice> LoadOrCreateDeviceAsync()
    {
        var fileName = GetDeviceFileName();
        var cachedData = LoadFromCache(fileName);

        if (cachedData == null) return await RegisterNewDeviceAsync(fileName);

        try
        {
            var deviceResponse = JsonSerializer.Deserialize<MPayDeviceWrapper>(cachedData);
            if (deviceResponse?.Device != null) return deviceResponse.Device;
        }
        catch (Exception)
        {
            // ignored
        }

        return await RegisterNewDeviceAsync(fileName);
    }

    private async Task<MPayDevice> RegisterNewDeviceAsync(string fileName)
    {
        var parameters = BuildDeviceRegistrationParams();
        var response = await _service.PostAsync($"/mpay/games/{ProjectId}/devices", parameters.BuildQueryString(),
            "application/x-www-form-urlencoded");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        SaveToCache(fileName, json);

        var deviceResponse = JsonSerializer.Deserialize<MPayDeviceWrapper>(json)!;

        return deviceResponse.Device;
    }

    public async Task<MPayUserWrapper?> LoginWithEmailAsync(string email, string password)
    {
        EnsureDeviceInitialized();

        var encryptedParams = EncryptLoginParams(email, password);
        var parameters = BuildBaseParams()
            .Add("opt_fields",
                "nickname,avatar,realname_status,mobile_bind_status,mask_related_mobile,related_login_status")
            .Add("params", encryptedParams)
            .Add("un", email.EncodeBase64());

        var response = await _service.PostAsync($"/mpay/games/{ProjectId}/devices/{Device!.Id}/users",
            parameters.BuildQueryString(), "application/x-www-form-urlencoded");

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return HandleLoginError(json);

        return JsonSerializer.Deserialize<MPayUserWrapper>(json);
    }

    private string EncryptLoginParams(string email, string password)
    {
        var loginParams = new MPayParameters
        {
            Username = email,
            Password = password.EncodeMd5(),
            UniqueId = _id
        };

        var json = JsonSerializer.Serialize(loginParams);
        var encrypted = json.AesEncrypt(Device!.Key.DecodeHex());
        return encrypted.EncodeHex();
    }

    private static MPayUserWrapper HandleLoginError(string errorJson)
    {
        var errorResponse = JsonSerializer.Deserialize<MPayVerifyResponse>(errorJson);
        if (errorResponse?.Code == 1351)
            throw new VerifyException(errorJson);

        throw new Exception($"Email login failed: {errorJson}");
    }

    public async Task<bool> SendSmsCodeAsync(string phoneNumber)
    {
        EnsureDeviceInitialized();

        var parameters = BuildBaseParams()
            .Add("device_id", Device!.Id)
            .Add("mobile", phoneNumber);

        var response = await _service.PostAsync("/mpay/api/users/login/mobile/get_sms", parameters.BuildQueryString(),
            "application/x-www-form-urlencoded");

        return response.IsSuccessStatusCode;
    }

    public async Task<MPaySMSTicket?> VerifySmsCodeAsync(string phoneNumber, string code)
    {
        EnsureDeviceInitialized();

        var parameters = BuildBaseParams()
            .Add("device_id", Device!.Id)
            .Add("mobile", phoneNumber)
            .Add("smscode", code)
            .Add("up_content", "");

        var response = await _service.PostAsync("/mpay/api/users/login/mobile/verify_sms",
            parameters.BuildQueryString(), "application/x-www-form-urlencoded");
        var json = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<MPaySMSTicket>(json)
            : null;
    }

    public async Task<MPayUserWrapper?> CompleteSmsLoginAsync(string phoneNumber, string ticket)
    {
        EnsureDeviceInitialized();

        var encodedPhone = phoneNumber.EncodeBase64();
        var parameters = BuildBaseParams()
            .Add("device_id", Device!.Id)
            .Add("opt_fields",
                "nickname,avatar,realname_status,mobile_bind_status,mask_related_mobile,related_login_status")
            .Add("ticket", ticket);

        var response = await _service.PostAsync(
            $"/mpay/api/users/login/mobile/finish?un={encodedPhone}",
            parameters.BuildQueryString(),
            "application/x-www-form-urlencoded"
        );
        var json = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<MPayUserWrapper>(json)
            : null;
    }

    private QueryBuilder BuildDeviceRegistrationParams()
    {
        return BuildBaseParams()
            .Add("unique_id", _id)
            .Add("brand", "Microsoft")
            .Add("device_model", "pc_mode")
            .Add("device_name", $"PC-{StringGenerator.GenerateRandomString(12)}")
            .Add("device_type", "Computer")
            .Add("init_urs_device", "0")
            .Add("mac", StringGenerator.GenerateRandomMacAddress())
            .Add("resolution", "1920x1080")
            .Add("system_name", "windows")
            .Add("system_version", "10.0.22621");
    }

    private QueryBuilder BuildBaseParams()
    {
        return new QueryBuilder()
            .Add("app_channel", "netease")
            .Add("app_mode", "2")
            .Add("app_type", "games")
            .Add("arch", "win_x64")
            .Add("cv", "c4.2.0")
            .Add("mcount_app_key", "EEkEEXLymcNjM42yLY3Bn6AO15aGy4yq")
            .Add("mcount_transaction_id", "0")
            .Add("process_id", Environment.ProcessId.ToString())
            .Add("sv", "10.0.22621")
            .Add("updater_cv", "c1.0.0")
            .Add("game_id", ProjectId)
            .Add("gv", GameVersion);
    }

    private void EnsureDeviceInitialized()
    {
        if (Device == null)
            throw new InvalidOperationException("Device not initialized. Call InitializeDeviceAsync() first.");
    }

    private string LoadOrCreateId()
    {
        var fileName = GetUniqueIdFileName();
        var cached = LoadFromCache(fileName);

        if (cached != null)
            return Encoding.UTF8.GetString(cached);

        var uniqueId = Guid.NewGuid().ToString("N");
        SaveToCache(fileName, uniqueId);
        return uniqueId;
    }

    private string GetUniqueIdFileName()
    {
        return $"{ProjectId}.id";
    }

    private string GetDeviceFileName()
    {
        return $"{ProjectId}.device";
    }

    private static void SaveToCache(string fileName, string content)
    {
        try
        {
            File.WriteAllText(fileName, content);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save cache file {fileName}: {ex.Message}");
        }
    }

    private static byte[]? LoadFromCache(string fileName)
    {
        try
        {
            return !File.Exists(fileName) ? null : File.ReadAllBytes(fileName);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load cache file {fileName}: {ex.Message}");
        }
    }
}