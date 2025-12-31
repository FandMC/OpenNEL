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
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using OpenNEL.Core.Extensions;
using OpenNEL.Core.Http;
using OpenNEL.Core.Utils;
using OpenNEL.MPay.Entities;
using OpenNEL.MPay.Exceptions;

namespace OpenNEL.MPay;

public sealed class MPayClient : IDisposable
{
    private const string ServiceEndpoint = "https://service.mkey.163.com";
    private const string AnalyticsEndpoint = "https://analytics.mpay.netease.com";

    private readonly HttpWrapper _httpClient;
    private readonly HttpWrapper _serviceClient;
    private EntityDevice? _deviceCache;
    private bool _disposed;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string GameId { get; }
    public string GameVersion { get; }
    public string Unique { get; }

    public MPayClient(string gameId, string gameVersion)
    {
        GameId = gameId ?? throw new ArgumentNullException(nameof(gameId));
        GameVersion = gameVersion ?? throw new ArgumentNullException(nameof(gameVersion));
        
        _httpClient = new HttpWrapper();
        _serviceClient = new HttpWrapper(ServiceEndpoint);
        
        Unique = LoadOrCreateUniqueId();
        _deviceCache = LoadOrCreateDevice();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _httpClient.Dispose();
        _serviceClient.Dispose();
        GC.SuppressFinalize(this);
    }

    public EntityDevice GetDevice() => _deviceCache ?? throw new InvalidOperationException("Device not initialized");

    public async Task Configure(string appKey, string channel)
    {
        ThrowIfDisposed();
        
        var queryParams = new ParameterBuilder()
            .Append("sub_app_key", "")
            .Append("api_ver", "2")
            .Append("gdpr", "0")
            .Append("app_channel", channel)
            .Append("sdk_version", "c1.0.0")
            .Append("app_key", appKey)
            .Append("device_id", Unique.ToUpperInvariant())
            .FormUrlEncode();
            
        var response = await _httpClient.GetAsync($"{AnalyticsEndpoint}/config?{queryParams}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<EntityMPayUserResponse> LoginWithEmailAsync(string email, string password)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var device = GetDevice();
        var loginParams = new EntityUsersParameters
        {
            Password = password.EncodeMd5(),
            Unique = Unique,
            Username = email
        };

        var encryptedParams = JsonSerializer.Serialize(loginParams, SerializerOptions)
            .EncodeAes(device.Key.DecodeHex())
            .EncodeHex();

        var requestParams = BuildRequestParams()
            .Append("opt_fields", "nickname,avatar,realname_status,mobile_bind_status,mask_related_mobile,related_login_status")
            .Append("params", encryptedParams)
            .Append("un", email.EncodeBase64())
            .FormUrlEncode();

        var response = await _serviceClient.PostAsync(
            $"/mpay/games/{GameId}/devices/{device.Id}/users",
            requestParams,
            "application/x-www-form-urlencoded");

        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = JsonSerializer.Deserialize<EntityVerifyResponse>(responseContent);
            if (errorResponse?.Code == 1351)
                throw new VerifyException(responseContent);
            throw new Exception($"Email login failed: {responseContent}");
        }

        return JsonSerializer.Deserialize<EntityMPayUserResponse>(responseContent) 
            ?? throw new Exception("Failed to parse login response");
    }

    public async Task<bool> SendSmsCodeAsync(string phoneNumber)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        var requestParams = BuildRequestParams()
            .Append("device_id", GetDevice().Id)
            .Append("mobile", phoneNumber)
            .FormUrlEncode();

        var response = await _serviceClient.PostAsync(
            "/mpay/api/users/login/mobile/get_sms",
            requestParams,
            "application/x-www-form-urlencoded");

        return response.IsSuccessStatusCode;
    }

    public async Task<EntitySmsTicket?> VerifySmsCodeAsync(string phoneNumber, string code)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var requestParams = BuildRequestParams()
            .Append("device_id", GetDevice().Id)
            .Append("mobile", phoneNumber)
            .Append("smscode", code)
            .Append("up_content", "")
            .FormUrlEncode();

        var response = await _serviceClient.PostAsync(
            "/mpay/api/users/login/mobile/verify_sms",
            requestParams,
            "application/x-www-form-urlencoded");

        if (!response.IsSuccessStatusCode) return null;
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EntitySmsTicket>(content);
    }

    public async Task<EntityMPayUserResponse?> FinishSmsCodeAsync(string phoneNumber, string ticket)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(ticket);

        var encodedPhone = phoneNumber.EncodeBase64();
        var requestParams = BuildRequestParams()
            .Append("device_id", GetDevice().Id)
            .Append("opt_fields", "nickname,avatar,realname_status,mobile_bind_status,mask_related_mobile,related_login_status")
            .Append("ticket", ticket)
            .FormUrlEncode();

        var response = await _serviceClient.PostAsync(
            $"/mpay/api/users/login/mobile/finish?un={encodedPhone}",
            requestParams,
            "application/x-www-form-urlencoded");

        if (!response.IsSuccessStatusCode) return null;
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EntityMPayUserResponse>(content);
    }

    private string LoadOrCreateUniqueId()
    {
        var fileName = $"{GameId}-guid.cds";
        var existingData = LoadFromFile(fileName);
        if (existingData != null)
            return Encoding.UTF8.GetString(existingData);
        
        var newId = Guid.NewGuid().ToString("N");
        SaveToFile(fileName, newId);
        return newId;
    }

    private EntityDevice LoadOrCreateDevice()
    {
        var fileName = $"{GameId}-device.cds";
        var existingData = LoadFromFile(fileName);
        
        if (existingData != null)
        {
            var cached = JsonSerializer.Deserialize<EntityDeviceResponse>(Encoding.UTF8.GetString(existingData));
            if (cached?.Device != null) return cached.Device;
        }

        return RegisterNewDevice(fileName);
    }

    private EntityDevice RegisterNewDevice(string cacheFileName)
    {
        var deviceParams = BuildRequestParams()
            .Append("brand", "Microsoft")
            .Append("device_model", "pc_mode")
            .Append("device_name", $"PC-{StringGenerator.GenerateRandomString(12)}")
            .Append("device_type", "Computer")
            .Append("init_urs_device", "0")
            .Append("mac", StringGenerator.GenerateRandomMacAddress())
            .Append("resolution", "1920x1080")
            .Append("system_name", "windows")
            .Append("system_version", "10.0.22621")
            .Append("unique_id", Unique)
            .FormUrlEncode();

        var response = _serviceClient.PostAsync(
            $"/mpay/games/{GameId}/devices",
            deviceParams,
            "application/x-www-form-urlencoded").GetAwaiter().GetResult();
        
        response.EnsureSuccessStatusCode();
        
        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        SaveToFile(cacheFileName, content);
        
        return JsonSerializer.Deserialize<EntityDeviceResponse>(content)?.Device
            ?? throw new Exception("Failed to register device");
    }

    private ParameterBuilder BuildRequestParams()
    {
        return new ParameterBuilder()
            .Append("app_channel", "netease")
            .Append("app_mode", "2")
            .Append("app_type", "games")
            .Append("arch", "win_x64")
            .Append("cv", "c4.2.0")
            .Append("mcount_app_key", "EEkEEXLymcNjM42yLY3Bn6AO15aGy4yq")
            .Append("mcount_transaction_id", "0")
            .Append("process_id", Environment.ProcessId.ToString())
            .Append("sv", "10.0.22621")
            .Append("updater_cv", "c1.0.0")
            .Append("game_id", GameId)
            .Append("gv", GameVersion);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public static void SaveToFile(string filename, string content)
    {
        File.WriteAllText(filename, content);
    }

    public static byte[]? LoadFromFile(string filename)
    {
        if (!File.Exists(filename)) return null;
        try
        {
            return File.ReadAllBytes(filename);
        }
        catch
        {
            return null;
        }
    }
}
