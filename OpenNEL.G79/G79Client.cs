using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using OpenNEL.Core.Cipher;
using OpenNEL.Core.Http;
using OpenNEL.G79.Entities;
using OpenNEL.G79.Entities.NetGame;
using OpenNEL.G79.Entities.RentalGame;
using OpenNEL.WPFLauncher;
using OpenNEL.WPFLauncher.Entities;

namespace OpenNEL.G79;

public class G79Client : IDisposable
{
    private readonly HttpWrapper _client = new("https://g79mclobt.minecraft.cn", null, new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip,
        ServerCertificateCustomValidationCallback = (HttpRequestMessage _, X509Certificate2? _, X509Chain? _, SslPolicyErrors _) => true,
        UseProxy = false
    });

    private readonly HttpWrapper _core = new("https://g79obtcore.nie.netease.com:8443", builder =>
    {
        builder.UserAgent("okhttp/3.12.12");
    }, new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip
    }, new Version(2, 0));

    private readonly MgbSdkClient _mgbSdk = new("x19");

    public void Dispose()
    {
        _core.Dispose();
        _client.Dispose();
        _mgbSdk.Dispose();
        GC.SuppressFinalize(this);
    }

    public Entity<EntityUserDetails> GetUserDetail(string userId, string userToken)
    {
        return GetUserDetailAsync(userId, userToken).GetAwaiter().GetResult();
    }

    public async Task<Entity<EntityUserDetails>> GetUserDetailAsync(string userId, string userToken)
    {
        var body = JsonSerializer.Serialize(new EntityQueryUserDetail
        {
            Version = new Version(2, 0)
        });
        var content = await (await _core.PostAsync("/pe-user-detail/get", body, builder =>
        {
            builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
        })).Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Entity<EntityUserDetails>>(content) ?? throw new Exception("Failed to deserialize: " + content);
    }

    public OpenNEL.G79.Entities.Entities<EntityNetGame> GetAvailableNetGames(string userId, string userToken)
    {
        return GetAvailableNetGamesAsync(userId, userToken).GetAwaiter().GetResult();
    }

    public async Task<OpenNEL.G79.Entities.Entities<EntityNetGame>> GetAvailableNetGamesAsync(string userId, string userToken)
    {
        var body = JsonSerializer.Serialize(new EntityNetGameRequest
        {
            Version = "2.12",
            ChannelId = 5
        });
        return JsonSerializer.Deserialize<OpenNEL.G79.Entities.Entities<EntityNetGame>>(
            await (await _client.PostAsync("/pe-game/query/get-list-v4", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entity<EntityNetGameServerAddress> GetNetGameServerAddress(string userId, string userToken, string gameId)
    {
        return GetNetGameServerAddressAsync(userId, userToken, gameId).GetAwaiter().GetResult();
    }

    public async Task<Entity<EntityNetGameServerAddress>> GetNetGameServerAddressAsync(string userId, string userToken, string gameId)
    {
        var body = JsonSerializer.Serialize(new EntityNetGameServerAddressRequest { ItemId = gameId });
        return JsonSerializer.Deserialize<Entity<EntityNetGameServerAddress>>(
            await (await _client.PostAsync("/pe-game/query/get-server-address", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public string GetAvailableRentalGames(string userId, string userToken, int offset)
    {
        return GetAvailableRentalGamesAsync(userId, userToken, offset).GetAwaiter().GetResult();
    }

    public async Task<string> GetAvailableRentalGamesAsync(string userId, string userToken, int offset)
    {
        var body = JsonSerializer.Serialize(new EntityRentalGameRequest
        {
            SortType = 0,
            OrderType = 0,
            Offset = offset
        });
        return await (await _client.PostAsync("/rental-server/query/available-by-sort-type", body, builder =>
        {
            builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
        })).Content.ReadAsStringAsync();
    }

    public Entity<EntityRentalGameServerAddress> GetRentalGameServerAddress(string userId, string userToken, string gameId, string password = "")
    {
        return GetRentalGameServerAddressAsync(userId, userToken, gameId, password).GetAwaiter().GetResult();
    }

    public async Task<Entity<EntityRentalGameServerAddress>> GetRentalGameServerAddressAsync(string userId, string userToken, string gameId, string password = "")
    {
        var body = JsonSerializer.Serialize(new EntityRentalGameServerAddressRequest
        {
            ServerId = gameId,
            Password = password
        });
        return JsonSerializer.Deserialize<Entity<EntityRentalGameServerAddress>>(
            await (await _client.PostAsync("/rental-server-world-enter/get", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entity<EntitySetNickName> SetNickName(string userId, string userToken, string nickName)
    {
        return SetNickNameAsync(userId, userToken, nickName).GetAwaiter().GetResult();
    }

    public async Task<Entity<EntitySetNickName>> SetNickNameAsync(string userId, string userToken, string nickName)
    {
        var body = JsonSerializer.Serialize(new EntitySetNickNameRequest
        {
            Name = nickName,
            EntityId = userId
        });
        return JsonSerializer.Deserialize<Entity<EntitySetNickName>>(
            await (await _client.PostAsync("/nickname-setting", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }
}
