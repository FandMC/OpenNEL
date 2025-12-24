using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenNEL.Core.Cipher;
using OpenNEL.Core.Http;
using OpenNEL.Core.Utils;
using OpenNEL.MPay;
using OpenNEL.MPay.Entities;
using OpenNEL.WPFLauncher.Entities;
using OpenNEL.WPFLauncher.Entities.NetGame;
using OpenNEL.WPFLauncher.Entities.Skin;
using OpenNEL.WPFLauncher.Entities.Texture;
using OpenNEL.WPFLauncher.Entities.Minecraft;
using OpenNEL.WPFLauncher.Entities.RentalGame;

namespace OpenNEL.WPFLauncher;

public class WPFLauncherClient : IDisposable
{
    private static readonly HttpWrapper Http = new();
    private readonly HttpWrapper _client;
    private readonly HttpWrapper _core;
    private readonly HttpWrapper _game;
    private readonly HttpWrapper _gateway;
    private readonly HttpWrapper _rental;
    private readonly MgbSdkClient _sdk = new("x19");

    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions EnumOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public MPayClient MPay { get; }

    public WPFLauncherClient()
    {
        MPay = new MPayClient("aecfrxodyqaaaajp-g-x19", GetLatestVersionAsync().GetAwaiter().GetResult());
        var userAgent = "WPFLauncher/" + MPay.GameVersion;
        _client = new HttpWrapper("https://x19mclobt.nie.netease.com", builder => builder.UserAgent(userAgent));
        _core = new HttpWrapper("https://x19obtcore.nie.netease.com:8443", builder => builder.UserAgent(userAgent));
        _game = new HttpWrapper("https://x19apigatewayobt.nie.netease.com", builder => builder.UserAgent(userAgent));
        _gateway = new HttpWrapper("https://x19apigatewayobt.nie.netease.com", builder => builder.UserAgent(userAgent));
        _rental = new HttpWrapper("https://x19mclobt.nie.netease.com", builder => builder.UserAgent(userAgent));
    }

    public void Dispose()
    {
        Http.Dispose();
        _core.Dispose();
        _game.Dispose();
        MPay.Dispose();
        _gateway.Dispose();
        _client.Dispose();
        _rental.Dispose();
        _sdk.Dispose();
        GC.SuppressFinalize(this);
    }

    public static string GetUserAgent()
    {
        return "WPFLauncher/" + GetLatestVersionAsync().GetAwaiter().GetResult();
    }

    #region Version & Login

    private static async Task<Dictionary<string, EntityPatchVersion>> GetPatchVersionsAsync()
    {
        var content = await (await Http.GetAsync("https://x19.update.netease.com/pl/x19_java_patchlist")).Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, EntityPatchVersion>>("{" + content[..content.LastIndexOf(',')] + "}")!;
    }

    public static async Task<string> GetLatestVersionAsync()
    {
        return (await GetPatchVersionsAsync()).Keys.Last();
    }

    public async Task<EntityMPayUserResponse> LoginWithEmailAsync(string email, string password)
    {
        return await MPay.LoginWithEmailAsync(email, password);
    }

    public static EntityX19CookieRequest GenerateCookie(EntityMPayUserResponse user, EntityDevice device)
    {
        return new EntityX19CookieRequest
        {
            Json = JsonSerializer.Serialize(new EntityX19Cookie
            {
                SdkUid = user.User.Id,
                SessionId = user.User.Token,
                Udid = Guid.NewGuid().ToString("N").ToUpper(),
                DeviceId = device.Id,
                AimInfo = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}"
            }, DefaultOptions)
        };
    }

    public (EntityAuthenticationOtp, string) LoginWithCookie(string cookie)
    {
        return LoginWithCookieAsync(cookie).GetAwaiter().GetResult();
    }

    public (EntityAuthenticationOtp, string) LoginWithCookie(EntityX19CookieRequest cookie)
    {
        return LoginWithCookieAsync(cookie).GetAwaiter().GetResult();
    }

    private async Task<(EntityAuthenticationOtp, string)> LoginWithCookieAsync(string cookie)
    {
        EntityX19CookieRequest cookieRequest;
        try
        {
            cookieRequest = JsonSerializer.Deserialize<EntityX19CookieRequest>(cookie)!;
        }
        catch (Exception)
        {
            cookieRequest = new EntityX19CookieRequest { Json = cookie };
        }
        return await LoginWithCookieAsync(cookieRequest);
    }

    private async Task<(EntityAuthenticationOtp, string)> LoginWithCookieAsync(EntityX19CookieRequest cookie)
    {
        var entity = JsonSerializer.Deserialize<EntityX19Cookie>(cookie.Json)!;
        if (entity.LoginChannel != "netease")
        {
            await _sdk.AuthSessionAsync(cookie.Json);
        }
        var user = await AuthenticationOtpAsync(cookie, await LoginOtpAsync(cookie));
        await InterConnClient.LoginStart(user.EntityId, user.Token);
        return (user, entity.LoginChannel);
    }

    private async Task<EntityLoginOtp> LoginOtpAsync(EntityX19CookieRequest cookieRequest)
    {
        var content = await (await _core.PostAsync("/login-otp", JsonSerializer.Serialize(cookieRequest, DefaultOptions))).Content.ReadAsStringAsync();
        var entity = JsonSerializer.Deserialize<Entity<JsonElement?>>(content);
        if (entity == null)
            throw new Exception("Failed to deserialize: " + content);
        if (entity.Code != 0 || !entity.Data.HasValue)
            throw new Exception("Failed to deserialize: " + entity.Message);
        return JsonSerializer.Deserialize<EntityLoginOtp>(entity.Data.Value.GetRawText())!;
    }

    private async Task<EntityAuthenticationOtp> AuthenticationOtpAsync(EntityX19CookieRequest cookieRequest, EntityLoginOtp otp)
    {
        var entityX19Cookie = JsonSerializer.Deserialize<EntityX19Cookie>(cookieRequest.Json)!;
        var disk = StringGenerator.GenerateHexString(4).ToUpper();
        var detail = new EntityAuthenticationDetail
        {
            Udid = "0000000000000000" + disk,
            AppVersion = MPay.GameVersion,
            PayChannel = entityX19Cookie.AppChannel,
            Disk = disk
        };
        var data = JsonSerializer.Serialize(new EntityAuthenticationData
        {
            SaData = JsonSerializer.Serialize(detail, DefaultOptions),
            AuthJson = cookieRequest.Json,
            Version = new EntityAuthenticationVersion { Version = MPay.GameVersion },
            Aid = otp.Aid.ToString(),
            OtpToken = otp.OtpToken,
            LockTime = 0
        }, DefaultOptions);
        
        var body = HttpUtil.HttpEncrypt(Encoding.UTF8.GetBytes(data));
        var response = await (await _core.PostAsync("/authentication-otp", body)).Content.ReadAsByteArrayAsync();
        var decrypted = HttpUtil.HttpDecrypt(response) ?? throw new Exception("Cannot decrypt data");
        var entity = JsonSerializer.Deserialize<Entity<EntityAuthenticationOtp>>(decrypted)!;
        if (entity.Code == 0)
            return entity.Data!;
        throw new Exception(entity.Message);
    }

    public async Task<EntityAuthenticationUpdate?> AuthenticationUpdateAsync(string userId, string userToken)
    {
        var entityJson = JsonSerializer.Serialize(new EntityAuthenticationUpdate
        {
            EntityId = userId,
            IsRegister = true
        }, DefaultOptions);
        var body = HttpUtil.HttpEncrypt(Encoding.UTF8.GetBytes(entityJson));
        var response = await _core.PostAsync("/authentication/update", body, builder =>
        {
            builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, entityJson, userId, userToken));
        });
        var decrypted = HttpUtil.HttpDecrypt(await response.Content.ReadAsByteArrayAsync());
        if (response.IsSuccessStatusCode && decrypted != null)
        {
            try
            {
                return JsonSerializer.Deserialize<Entity<EntityAuthenticationUpdate>>(decrypted)!.Data;
            }
            catch { }
        }
        return null;
    }

    #endregion

    #region NetGame API

    public Entities<EntityNetGameItem> GetAvailableNetGames(string userId, string userToken, int offset, int length)
    {
        return GetAvailableNetGamesAsync(userId, userToken, offset, length).GetAwaiter().GetResult();
    }

    public async Task<Entities<EntityNetGameItem>> GetAvailableNetGamesAsync(string userId, string userToken, int offset, int length)
    {
        var body = JsonSerializer.Serialize(new EntityNetGameRequest
        {
            AvailableMcVersions = Array.Empty<string>(),
            ItemType = 1,
            Length = length,
            Offset = offset,
            MasterTypeId = "2",
            SecondaryTypeId = ""
        }, DefaultOptions);
        return JsonSerializer.Deserialize<Entities<EntityNetGameItem>>(
            await (await _game.PostAsync("/item/query/available", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entities<EntityQueryNetGameItem> QueryNetGameItemByIds(string userId, string userToken, string[] ids)
    {
        return QueryNetGameItemByIdsAsync(userId, userToken, ids).GetAwaiter().GetResult();
    }

    public async Task<Entities<EntityQueryNetGameItem>> QueryNetGameItemByIdsAsync(string userId, string userToken, string[] ids)
    {
        var body = JsonSerializer.Serialize(new EntityQueryNetGameRequest { EntityIds = ids }, DefaultOptions);
        return JsonSerializer.Deserialize<Entities<EntityQueryNetGameItem>>(
            await (await _game.PostAsync("/item/query/search-by-ids", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entity<EntityQueryNetGameDetailItem> QueryNetGameDetailById(string userId, string userToken, string gameId)
    {
        return QueryNetGameDetailByIdAsync(userId, userToken, gameId).GetAwaiter().GetResult();
    }

    public async Task<Entity<EntityQueryNetGameDetailItem>> QueryNetGameDetailByIdAsync(string userId, string userToken, string gameId)
    {
        var body = JsonSerializer.Serialize(new EntityQueryNetGameDetailRequest { ItemId = gameId }, DefaultOptions);
        return JsonSerializer.Deserialize<Entity<EntityQueryNetGameDetailItem>>(
            await (await _game.PostAsync("/item-details/get_v2", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entities<EntityGameCharacter> QueryNetGameCharacters(string userId, string userToken, string gameId)
    {
        return QueryNetGameCharactersAsync(userId, userToken, gameId).GetAwaiter().GetResult();
    }

    public async Task<Entities<EntityGameCharacter>> QueryNetGameCharactersAsync(string userId, string userToken, string gameId)
    {
        var body = JsonSerializer.Serialize(new EntityQueryGameCharacters
        {
            GameId = gameId,
            UserId = userId
        }, DefaultOptions);
        return JsonSerializer.Deserialize<Entities<EntityGameCharacter>>(
            await (await _game.PostAsync("/game-character/query/user-game-characters", body, builder =>
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
        var body = JsonSerializer.Serialize(new { item_id = gameId }, DefaultOptions);
        return JsonSerializer.Deserialize<Entity<EntityNetGameServerAddress>>(
            await (await _game.PostAsync("/item-address/get", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entities<EntityNetGameItem>? QueryNetGameWithKeyword(string userId, string userToken, string keyword)
    {
        return QueryNetGameWithKeywordAsync(userId, userToken, keyword).GetAwaiter().GetResult();
    }

    public async Task<Entities<EntityNetGameItem>?> QueryNetGameWithKeywordAsync(string userId, string userToken, string keyword)
    {
        var response = await _game.PostAsync("/item/query/search-by-keyword",
            JsonSerializer.Serialize(new EntityNetGameKeyword { Keyword = keyword }, DefaultOptions),
            builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            });
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return null;
        return JsonSerializer.Deserialize<Entities<EntityNetGameItem>>(content);
    }

    public void CreateCharacter(string userId, string userToken, string gameId, string roleName)
    {
        CreateCharacterAsync(userId, userToken, gameId, roleName).GetAwaiter().GetResult();
    }

    public async Task CreateCharacterAsync(string userId, string userToken, string gameId, string roleName)
    {
        var response = await _game.PostAsync("/game-character",
            JsonSerializer.Serialize(new EntityCreateCharacter
            {
                GameId = gameId,
                UserId = userId,
                Name = roleName
            }, DefaultOptions),
            builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            });
        if (!response.IsSuccessStatusCode)
            throw new Exception("Failed to create character");
    }

    #endregion

    #region Skin API

    public Entities<EntitySkin> GetFreeSkinList(string userId, string userToken, int offset, int length = 20)
    {
        return GetFreeSkinListAsync(userId, userToken, offset, length).GetAwaiter().GetResult();
    }

    public async Task<Entities<EntitySkin>> GetFreeSkinListAsync(string userId, string userToken, int offset, int length = 20)
    {
        var body = JsonSerializer.Serialize(new EntityFreeSkinListRequest
        {
            IsHas = true,
            ItemType = 2,
            Length = length,
            MasterTypeId = 10,
            Offset = offset,
            PriceType = 3,
            SecondaryTypeId = 31
        }, DefaultOptions);
        return JsonSerializer.Deserialize<Entities<EntitySkin>>(
            await (await _gateway.PostAsync("/item/query/available", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entities<EntitySkin> QueryFreeSkinByName(string userId, string userToken, string name)
    {
        return QueryFreeSkinByNameAsync(userId, userToken, name).GetAwaiter().GetResult();
    }

    public async Task<Entities<EntitySkin>> QueryFreeSkinByNameAsync(string userId, string userToken, string name)
    {
        var body = JsonSerializer.Serialize(new EntityQuerySkinByNameRequest
        {
            IsHas = true,
            IsSync = 0,
            ItemType = 2,
            Keyword = name,
            Length = 20,
            MasterTypeId = 10,
            Offset = 0,
            PriceType = 3,
            SecondaryTypeId = "31",
            SortType = 1,
            Year = 0
        }, DefaultOptions);
        return JsonSerializer.Deserialize<Entities<EntitySkin>>(
            await (await _gateway.PostAsync("/item/query/search-by-keyword", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entities<EntitySkin> GetSkinDetails(string userId, string userToken, Entities<EntitySkin> skinList)
    {
        return GetSkinDetailsAsync(userId, userToken, skinList).GetAwaiter().GetResult();
    }

    public async Task<Entities<EntitySkin>> GetSkinDetailsAsync(string userId, string userToken, Entities<EntitySkin> skinList)
    {
        var entityIds = skinList.Data.Select(e => e.EntityId).ToList();
        var body = JsonSerializer.Serialize(new EntitySkinDetailsRequest
        {
            ChannelId = 11,
            EntityIds = entityIds,
            IsHas = true,
            WithPrice = true,
            WithTitleImage = true
        }, DefaultOptions);
        return JsonSerializer.Deserialize<Entities<EntitySkin>>(
            await (await _gateway.PostAsync("/item/query/search-by-ids", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public EntityResponse PurchaseSkin(string userId, string userToken, string entityId)
    {
        return PurchaseSkinAsync(userId, userToken, entityId).GetAwaiter().GetResult();
    }

    public async Task<EntityResponse> PurchaseSkinAsync(string userId, string userToken, string entityId)
    {
        var body = JsonSerializer.Serialize(new EntitySkinPurchaseRequest
        {
            BatchCount = 1,
            BuyPath = "PC_H5_COMPONENT_DETAIL",
            Diamond = 0,
            EntityId = 0,
            ItemId = entityId,
            ItemLevel = 0,
            LastPlayTime = 0,
            PurchaseTime = 0,
            TotalPlayTime = 0,
            UserId = userId
        }, DefaultOptions);
        return JsonSerializer.Deserialize<EntityResponse>(
            await (await _gateway.PostAsync("/user-item-purchase", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public EntityResponse SetSkin(string userId, string userToken, string entityId)
    {
        return SetSkinAsync(userId, userToken, entityId).GetAwaiter().GetResult();
    }

    public async Task<EntityResponse> SetSkinAsync(string userId, string userToken, string entityId)
    {
        var skinSettings = new List<EntitySkinSettings>();
        foreach (var gameType in new[] { 9, 8, 2, 10, 7 })
        {
            skinSettings.Add(new EntitySkinSettings
            {
                ClientType = "java",
                GameType = gameType,
                SkinId = entityId,
                SkinMode = 0,
                SkinType = 31
            });
        }
        var body = JsonSerializer.Serialize(new { skin_settings = skinSettings }, DefaultOptions);
        return JsonSerializer.Deserialize<EntityResponse>(
            await (await _gateway.PostAsync("/user-game-skin-multi", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public List<EntityUserGameTexture> GetSkinListInGame(string userId, string userToken, EntityUserGameTextureRequest entity)
    {
        return GetSkinListInGameAsync(userId, userToken, entity).GetAwaiter().GetResult();
    }

    public async Task<List<EntityUserGameTexture>> GetSkinListInGameAsync(string userId, string userToken, EntityUserGameTextureRequest entity)
    {
        var body = JsonSerializer.Serialize(entity, DefaultOptions);
        var result = JsonSerializer.Deserialize<Entities<EntityUserGameTexture>>(
            await (await _game.PostAsync("/user-game-skin/query/search-by-type", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
        return result.Data.ToList();
    }

    #endregion

    #region Minecraft Client Libs

    public async Task<Entity<EntityQuerySearchByGameResponse>> GetGameCoreModListAsync(string userId, string userToken, EnumGameVersion gameVersion, bool isRental)
    {
        var body = JsonSerializer.Serialize(new EntityQuerySearchByGameRequest
        {
            McVersionId = (int)gameVersion,
            GameType = isRental ? 8 : 2
        }, DefaultOptions);
        return JsonSerializer.Deserialize<Entity<EntityQuerySearchByGameResponse>>(
            await (await _game.PostAsync("/game-auth-item-list/query/search-by-game", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public async Task<Entities<EntityComponentDownloadInfoResponse>> GetGameCoreModDetailsListAsync(string userId, string userToken, List<ulong> gameModList)
    {
        var body = JsonSerializer.Serialize(new EntitySearchByIdsQuery { ItemIdList = gameModList }, DefaultOptions);
        return JsonSerializer.Deserialize<Entities<EntityComponentDownloadInfoResponse>>(
            await (await _game.PostAsync("/user-item-download-v2/get-list", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entity<EntityCoreLibResponse> GetMinecraftClientLibs(string userId, string userToken, EnumGameVersion? gameVersion = null)
    {
        return GetMinecraftClientLibsAsync(userId, userToken, gameVersion).GetAwaiter().GetResult();
    }

    public async Task<Entity<EntityCoreLibResponse>> GetMinecraftClientLibsAsync(string userId, string userToken, EnumGameVersion? gameVersion = null)
    {
        gameVersion ??= EnumGameVersion.NONE;
        var body = JsonSerializer.Serialize(new EntityMcDownloadVersion { McVersion = (int)gameVersion.Value }, DefaultOptions);
        return JsonSerializer.Deserialize<Entity<EntityCoreLibResponse>>(
            await (await _client.PostAsync("/game-patch-info", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entity<EntityComponentDownloadInfoResponse> GetNetGameComponentDownloadList(string userId, string userToken, string gameId)
    {
        return GetNetGameComponentDownloadListAsync(userId, userToken, gameId).GetAwaiter().GetResult();
    }

    public async Task<Entity<EntityComponentDownloadInfoResponse>> GetNetGameComponentDownloadListAsync(string userId, string userToken, string gameId)
    {
        var body = JsonSerializer.Serialize(new EntitySearchByItemIdQuery
        {
            ItemId = gameId,
            Length = 0,
            Offset = 0
        }, DefaultOptions);
        return JsonSerializer.Deserialize<Entity<EntityComponentDownloadInfoResponse>>(
            await (await _client.PostAsync("/user-item-download-v2", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    #endregion

    #region Rental Game API

    public Entities<EntityRentalGame> GetRentalGameList(string userId, string userToken, int offset)
    {
        return GetRentalGameListAsync(userId, userToken, offset).GetAwaiter().GetResult();
    }

    public async Task<Entities<EntityRentalGame>> GetRentalGameListAsync(string userId, string userToken, int offset)
    {
        var body = JsonSerializer.Serialize(new EntityQueryRentalGame
        {
            Offset = offset,
            SortType = 0
        }, DefaultOptions);
        var response = await _rental.PostAsync("/rental-server/query/available-public-server", body, builder =>
        {
            builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
        });
        return JsonSerializer.Deserialize<Entities<EntityRentalGame>>(await response.Content.ReadAsStringAsync(), EnumOptions)!;
    }

    public Entities<EntityRentalGamePlayerList> GetRentalGameRolesList(string userId, string userToken, string entityId)
    {
        return GetRentalGameRolesListAsync(userId, userToken, entityId).GetAwaiter().GetResult();
    }

    public async Task<Entities<EntityRentalGamePlayerList>> GetRentalGameRolesListAsync(string userId, string userToken, string entityId)
    {
        var body = JsonSerializer.Serialize(new EntityQueryRentalGamePlayerList
        {
            ServerId = entityId,
            Offset = 0,
            Length = 10
        }, DefaultOptions);
        return JsonSerializer.Deserialize<Entities<EntityRentalGamePlayerList>>(
            await (await _rental.PostAsync("/rental-server-player/query/search-by-user-server", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entity<EntityRentalGamePlayerList> AddRentalGameRole(string userId, string userToken, string serverId, string roleName)
    {
        return AddRentalGameRoleAsync(userId, userToken, serverId, roleName).GetAwaiter().GetResult();
    }

    public async Task<Entity<EntityRentalGamePlayerList>> AddRentalGameRoleAsync(string userId, string userToken, string serverId, string roleName)
    {
        var body = JsonSerializer.Serialize(new EntityAddRentalGameRole
        {
            ServerId = serverId,
            UserId = userId,
            Name = roleName,
            CreateTs = 555555,
            IsOnline = false,
            Status = 0
        }, DefaultOptions);
        return JsonSerializer.Deserialize<Entity<EntityRentalGamePlayerList>>(
            await (await _rental.PostAsync("/rental-server-player", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entity<EntityRentalGamePlayerList> DeleteRentalGameRole(string userId, string userToken, string entityId)
    {
        return DeleteRentalGameRoleAsync(userId, userToken, entityId).GetAwaiter().GetResult();
    }

    public async Task<Entity<EntityRentalGamePlayerList>> DeleteRentalGameRoleAsync(string userId, string userToken, string entityId)
    {
        var body = JsonSerializer.Serialize(new EntityDeleteRentalGameRole { EntityId = entityId }, DefaultOptions);
        return JsonSerializer.Deserialize<Entity<EntityRentalGamePlayerList>>(
            await (await _rental.PostAsync("/rental-server-player/delete", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entity<EntityRentalGameServerAddress> GetRentalGameServerAddress(string userId, string userToken, string entityId, string? pwd = null)
    {
        return GetRentalGameServerAddressAsync(userId, userToken, entityId, pwd).GetAwaiter().GetResult();
    }

    public async Task<Entity<EntityRentalGameServerAddress>> GetRentalGameServerAddressAsync(string userId, string userToken, string entityId, string? pwd = null)
    {
        var body = JsonSerializer.Serialize(new EntityQueryRentalGameServerAddress
        {
            ServerId = entityId,
            Password = pwd ?? "none"
        }, DefaultOptions);
        return JsonSerializer.Deserialize<Entity<EntityRentalGameServerAddress>>(
            await (await _rental.PostAsync("/rental-server-world-enter/get", body, builder =>
            {
                builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
            })).Content.ReadAsStringAsync())!;
    }

    public Entity<EntityRentalGameDetails> GetRentalGameDetails(string userId, string userToken, string entityId)
    {
        return GetRentalGameDetailsAsync(userId, userToken, entityId).GetAwaiter().GetResult();
    }

    public async Task<Entity<EntityRentalGameDetails>> GetRentalGameDetailsAsync(string userId, string userToken, string entityId)
    {
        var body = JsonSerializer.Serialize(new EntityQueryRentalGameDetail { ServerId = entityId }, DefaultOptions);
        var content = await (await _rental.PostAsync("/rental-server-details/get", body, builder =>
        {
            builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
        })).Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Entity<EntityRentalGameDetails>>(content, EnumOptions)!;
    }

    public Entities<EntityRentalGame> SearchRentalGameByName(string userId, string userToken, string worldId)
    {
        return SearchRentalGameByNameAsync(userId, userToken, worldId).GetAwaiter().GetResult();
    }

    public async Task<Entities<EntityRentalGame>> SearchRentalGameByNameAsync(string userId, string userToken, string worldId)
    {
        var body = JsonSerializer.Serialize(new EntityQueryRentalGameById
        {
            Offset = 0,
            SortType = EnumSortType.General,
            WorldNameKey = new List<string> { worldId }
        }, DefaultOptions);
        var response = await _rental.PostAsync("/rental-server/query/available-public-server", body, builder =>
        {
            builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, userId, userToken));
        });
        return JsonSerializer.Deserialize<Entities<EntityRentalGame>>(await response.Content.ReadAsStringAsync(), EnumOptions)!;
    }

    #endregion
}
