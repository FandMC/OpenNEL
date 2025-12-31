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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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

public sealed class WPFLauncherClient : IDisposable
{
    private readonly record struct Endpoints(string Lobby, string Core, string Api, string Gateway, string Rental, string Patch);
    private static readonly Endpoints Urls = new(
        "https://x19mclobt.nie.netease.com",
        "https://x19obtcore.nie.netease.com:8443",
        "https://x19apigatewayobt.nie.netease.com",
        "https://x19apigatewayobt.nie.netease.com",
        "https://x19mclobt.nie.netease.com",
        "https://x19.update.netease.com");

    private static readonly HttpWrapper SharedHttp = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly JsonSerializerOptions EnumOpts = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };

    private readonly HttpWrapper _lobby;
    private readonly HttpWrapper _core;
    private readonly HttpWrapper _api;
    private readonly HttpWrapper _gw;
    private readonly HttpWrapper _rent;
    private readonly MgbSdkClient _mgb;
    private bool _disposed;

    public MPayClient MPay { get; }

    public WPFLauncherClient()
    {
        var ver = ResolveLatestVersion().GetAwaiter().GetResult();
        MPay = new MPayClient("aecfrxodyqaaaajp-g-x19", ver);
        var ua = $"WPFLauncher/{ver}";
        _lobby = new HttpWrapper(Urls.Lobby, b => b.UserAgent(ua));
        _core = new HttpWrapper(Urls.Core, b => b.UserAgent(ua));
        _api = new HttpWrapper(Urls.Api, b => b.UserAgent(ua));
        _gw = new HttpWrapper(Urls.Gateway, b => b.UserAgent(ua));
        _rent = new HttpWrapper(Urls.Rental, b => b.UserAgent(ua));
        _mgb = new MgbSdkClient("x19");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SharedHttp.Dispose();
        _core.Dispose();
        _api.Dispose();
        MPay.Dispose();
        _gw.Dispose();
        _lobby.Dispose();
        _rent.Dispose();
        _mgb.Dispose();
        GC.SuppressFinalize(this);
    }

    public static string GetUserAgent() => $"WPFLauncher/{ResolveLatestVersion().GetAwaiter().GetResult()}";

    private static async Task<Dictionary<string, EntityPatchVersion>> FetchPatchMap()
    {
        var raw = await (await SharedHttp.GetAsync($"{Urls.Patch}/pl/x19_java_patchlist")).Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, EntityPatchVersion>>("{" + raw[..raw.LastIndexOf(',')] + "}")!;
    }

    public static async Task<string> GetLatestVersionAsync() => (await FetchPatchMap()).Keys.Last();
    private static Task<string> ResolveLatestVersion() => GetLatestVersionAsync();

    public Task<EntityMPayUserResponse> LoginWithEmailAsync(string email, string password) => MPay.LoginWithEmailAsync(email, password);

    public static EntityX19CookieRequest GenerateCookie(EntityMPayUserResponse user, EntityDevice device) => new()
    {
        Json = JsonSerializer.Serialize(new EntityX19Cookie
        {
            SdkUid = user.User.Id,
            SessionId = user.User.Token,
            Udid = Guid.NewGuid().ToString("N").ToUpper(),
            DeviceId = device.Id,
            AimInfo = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}"
        }, JsonOpts)
    };

    public (EntityAuthenticationOtp, string) LoginWithCookie(string cookie) => PerformCookieLogin(cookie).GetAwaiter().GetResult();
    public (EntityAuthenticationOtp, string) LoginWithCookie(EntityX19CookieRequest cookie) => PerformCookieLogin(cookie).GetAwaiter().GetResult();

    private async Task<(EntityAuthenticationOtp, string)> PerformCookieLogin(string raw)
    {
        EntityX19CookieRequest req;
        try { req = JsonSerializer.Deserialize<EntityX19CookieRequest>(raw)!; }
        catch { req = new EntityX19CookieRequest { Json = raw }; }
        return await PerformCookieLogin(req);
    }

    private async Task<(EntityAuthenticationOtp, string)> PerformCookieLogin(EntityX19CookieRequest req)
    {
        var parsed = JsonSerializer.Deserialize<EntityX19Cookie>(req.Json)!;
        if (parsed.LoginChannel != "netease") await _mgb.AuthSessionAsync(req.Json);
        var otp = await RequestOtpToken(req);
        var auth = await CompleteAuthentication(req, otp);
        await InterConnClient.LoginStart(auth.EntityId, auth.Token);
        return (auth, parsed.LoginChannel);
    }

    private async Task<EntityLoginOtp> RequestOtpToken(EntityX19CookieRequest req)
    {
        var resp = await (await _core.PostAsync("/login-otp", JsonSerializer.Serialize(req, JsonOpts))).Content.ReadAsStringAsync();
        var wrap = JsonSerializer.Deserialize<Entity<JsonElement?>>(resp) ?? throw new Exception($"Parse failed: {resp}");
        if (wrap.Code != 0 || !wrap.Data.HasValue) throw new Exception($"OTP error: {wrap.Message}");
        return JsonSerializer.Deserialize<EntityLoginOtp>(wrap.Data.Value.GetRawText())!;
    }

    private async Task<EntityAuthenticationOtp> CompleteAuthentication(EntityX19CookieRequest req, EntityLoginOtp otp)
    {
        var cookie = JsonSerializer.Deserialize<EntityX19Cookie>(req.Json)!;
        var disk = StringGenerator.GenerateHexString(4).ToUpper();
        var detail = new EntityAuthenticationDetail { Udid = "0000000000000000" + disk, AppVersion = MPay.GameVersion, PayChannel = cookie.AppChannel, Disk = disk };
        var payload = JsonSerializer.Serialize(new EntityAuthenticationData
        {
            SaData = JsonSerializer.Serialize(detail, JsonOpts),
            AuthJson = req.Json,
            Version = new EntityAuthenticationVersion { Version = MPay.GameVersion },
            Aid = otp.Aid.ToString(),
            OtpToken = otp.OtpToken,
            LockTime = 0
        }, JsonOpts);
        var cipher = HttpUtil.HttpEncrypt(Encoding.UTF8.GetBytes(payload));
        var respBytes = await (await _core.PostAsync("/authentication-otp", cipher)).Content.ReadAsByteArrayAsync();
        var plain = HttpUtil.HttpDecrypt(respBytes) ?? throw new Exception("Decryption failed");
        var result = JsonSerializer.Deserialize<Entity<EntityAuthenticationOtp>>(plain)!;
        return result.Code == 0 ? result.Data! : throw new Exception(result.Message);
    }

    public async Task<EntityAuthenticationUpdate?> AuthenticationUpdateAsync(string uid, string tok)
    {
        var json = JsonSerializer.Serialize(new EntityAuthenticationUpdate { EntityId = uid, IsRegister = true }, JsonOpts);
        var cipher = HttpUtil.HttpEncrypt(Encoding.UTF8.GetBytes(json));
        var resp = await _core.PostAsync("/authentication/update", cipher, b => b.AddHeader(TokenUtil.ComputeHttpRequestToken(b.Url, json, uid, tok)));
        var plain = HttpUtil.HttpDecrypt(await resp.Content.ReadAsByteArrayAsync());
        if (resp.IsSuccessStatusCode && plain != null) try { return JsonSerializer.Deserialize<Entity<EntityAuthenticationUpdate>>(plain)!.Data; } catch { }
        return null;
    }

    public Entities<EntityNetGameItem> GetAvailableNetGames(string uid, string tok, int off, int len) => FetchNetGames(uid, tok, off, len).GetAwaiter().GetResult();
    public Task<Entities<EntityNetGameItem>> GetAvailableNetGamesAsync(string uid, string tok, int off, int len) => FetchNetGames(uid, tok, off, len);

    private async Task<Entities<EntityNetGameItem>> FetchNetGames(string uid, string tok, int off, int len)
    {
        var body = JsonSerializer.Serialize(new EntityNetGameRequest { AvailableMcVersions = Array.Empty<string>(), ItemType = 1, Length = len, Offset = off, MasterTypeId = "2", SecondaryTypeId = "" }, JsonOpts);
        return JsonSerializer.Deserialize<Entities<EntityNetGameItem>>(await PostWithToken(_api, "/item/query/available", body, uid, tok))!;
    }

    public Entities<EntityQueryNetGameItem> QueryNetGameItemByIds(string uid, string tok, string[] ids) => QueryGamesByIds(uid, tok, ids).GetAwaiter().GetResult();
    public Task<Entities<EntityQueryNetGameItem>> QueryNetGameItemByIdsAsync(string uid, string tok, string[] ids) => QueryGamesByIds(uid, tok, ids);

    private async Task<Entities<EntityQueryNetGameItem>> QueryGamesByIds(string uid, string tok, string[] ids)
    {
        var body = JsonSerializer.Serialize(new EntityQueryNetGameRequest { EntityIds = ids }, JsonOpts);
        return JsonSerializer.Deserialize<Entities<EntityQueryNetGameItem>>(await PostWithToken(_api, "/item/query/search-by-ids", body, uid, tok))!;
    }

    public Entity<EntityQueryNetGameDetailItem> QueryNetGameDetailById(string uid, string tok, string gid) => FetchGameDetail(uid, tok, gid).GetAwaiter().GetResult();
    public Task<Entity<EntityQueryNetGameDetailItem>> QueryNetGameDetailByIdAsync(string uid, string tok, string gid) => FetchGameDetail(uid, tok, gid);

    private async Task<Entity<EntityQueryNetGameDetailItem>> FetchGameDetail(string uid, string tok, string gid)
    {
        var body = JsonSerializer.Serialize(new EntityQueryNetGameDetailRequest { ItemId = gid }, JsonOpts);
        return JsonSerializer.Deserialize<Entity<EntityQueryNetGameDetailItem>>(await PostWithToken(_api, "/item-details/get_v2", body, uid, tok))!;
    }

    public Entities<EntityGameCharacter> QueryNetGameCharacters(string uid, string tok, string gid) => FetchCharacters(uid, tok, gid).GetAwaiter().GetResult();
    public Task<Entities<EntityGameCharacter>> QueryNetGameCharactersAsync(string uid, string tok, string gid) => FetchCharacters(uid, tok, gid);

    private async Task<Entities<EntityGameCharacter>> FetchCharacters(string uid, string tok, string gid)
    {
        var body = JsonSerializer.Serialize(new EntityQueryGameCharacters { GameId = gid, UserId = uid }, JsonOpts);
        return JsonSerializer.Deserialize<Entities<EntityGameCharacter>>(await PostWithToken(_api, "/game-character/query/user-game-characters", body, uid, tok))!;
    }

    public Entity<EntityNetGameServerAddress> GetNetGameServerAddress(string uid, string tok, string gid) => FetchServerAddr(uid, tok, gid).GetAwaiter().GetResult();
    public Task<Entity<EntityNetGameServerAddress>> GetNetGameServerAddressAsync(string uid, string tok, string gid) => FetchServerAddr(uid, tok, gid);

    private async Task<Entity<EntityNetGameServerAddress>> FetchServerAddr(string uid, string tok, string gid)
    {
        var body = JsonSerializer.Serialize(new { item_id = gid }, JsonOpts);
        return JsonSerializer.Deserialize<Entity<EntityNetGameServerAddress>>(await PostWithToken(_api, "/item-address/get", body, uid, tok))!;
    }

    public Entities<EntityNetGameItem>? QueryNetGameWithKeyword(string uid, string tok, string kw) => SearchByKeyword(uid, tok, kw).GetAwaiter().GetResult();
    public Task<Entities<EntityNetGameItem>?> QueryNetGameWithKeywordAsync(string uid, string tok, string kw) => SearchByKeyword(uid, tok, kw);

    private async Task<Entities<EntityNetGameItem>?> SearchByKeyword(string uid, string tok, string kw)
    {
        var body = JsonSerializer.Serialize(new EntityNetGameKeyword { Keyword = kw }, JsonOpts);
        var resp = await _api.PostAsync("/item/query/search-by-keyword", body, b => b.AddHeader(TokenUtil.ComputeHttpRequestToken(b.Url, b.Body, uid, tok)));
        return resp.IsSuccessStatusCode ? JsonSerializer.Deserialize<Entities<EntityNetGameItem>>(await resp.Content.ReadAsStringAsync()) : null;
    }

    public void CreateCharacter(string uid, string tok, string gid, string name) => AddCharacter(uid, tok, gid, name).GetAwaiter().GetResult();
    public Task CreateCharacterAsync(string uid, string tok, string gid, string name) => AddCharacter(uid, tok, gid, name);

    private async Task AddCharacter(string uid, string tok, string gid, string name)
    {
        var body = JsonSerializer.Serialize(new EntityCreateCharacter { GameId = gid, UserId = uid, Name = name }, JsonOpts);
        var resp = await _api.PostAsync("/game-character", body, b => b.AddHeader(TokenUtil.ComputeHttpRequestToken(b.Url, b.Body, uid, tok)));
        if (!resp.IsSuccessStatusCode) throw new Exception("Character creation failed");
    }

    public Entities<EntitySkin> GetFreeSkinList(string uid, string tok, int off, int len = 20) => FetchSkins(uid, tok, off, len).GetAwaiter().GetResult();
    public Task<Entities<EntitySkin>> GetFreeSkinListAsync(string uid, string tok, int off, int len = 20) => FetchSkins(uid, tok, off, len);

    private async Task<Entities<EntitySkin>> FetchSkins(string uid, string tok, int off, int len)
    {
        var body = JsonSerializer.Serialize(new EntityFreeSkinListRequest { IsHas = true, ItemType = 2, Length = len, MasterTypeId = 10, Offset = off, PriceType = 3, SecondaryTypeId = 31 }, JsonOpts);
        return JsonSerializer.Deserialize<Entities<EntitySkin>>(await PostWithToken(_gw, "/item/query/available", body, uid, tok))!;
    }

    public Entities<EntitySkin> QueryFreeSkinByName(string uid, string tok, string name) => SearchSkinByName(uid, tok, name).GetAwaiter().GetResult();
    public Task<Entities<EntitySkin>> QueryFreeSkinByNameAsync(string uid, string tok, string name) => SearchSkinByName(uid, tok, name);

    private async Task<Entities<EntitySkin>> SearchSkinByName(string uid, string tok, string name)
    {
        var body = JsonSerializer.Serialize(new EntityQuerySkinByNameRequest { IsHas = true, IsSync = 0, ItemType = 2, Keyword = name, Length = 20, MasterTypeId = 10, Offset = 0, PriceType = 3, SecondaryTypeId = "31", SortType = 1, Year = 0 }, JsonOpts);
        return JsonSerializer.Deserialize<Entities<EntitySkin>>(await PostWithToken(_gw, "/item/query/search-by-keyword", body, uid, tok))!;
    }

    public Entities<EntitySkin> GetSkinDetails(string uid, string tok, Entities<EntitySkin> list) => FetchSkinDetails(uid, tok, list).GetAwaiter().GetResult();
    public Task<Entities<EntitySkin>> GetSkinDetailsAsync(string uid, string tok, Entities<EntitySkin> list) => FetchSkinDetails(uid, tok, list);

    private async Task<Entities<EntitySkin>> FetchSkinDetails(string uid, string tok, Entities<EntitySkin> list)
    {
        var ids = list.Data.Select(e => e.EntityId).ToList();
        var body = JsonSerializer.Serialize(new EntitySkinDetailsRequest { ChannelId = 11, EntityIds = ids, IsHas = true, WithPrice = true, WithTitleImage = true }, JsonOpts);
        return JsonSerializer.Deserialize<Entities<EntitySkin>>(await PostWithToken(_gw, "/item/query/search-by-ids", body, uid, tok))!;
    }

    public EntityResponse PurchaseSkin(string uid, string tok, string eid) => BuySkin(uid, tok, eid).GetAwaiter().GetResult();
    public Task<EntityResponse> PurchaseSkinAsync(string uid, string tok, string eid) => BuySkin(uid, tok, eid);

    private async Task<EntityResponse> BuySkin(string uid, string tok, string eid)
    {
        var body = JsonSerializer.Serialize(new EntitySkinPurchaseRequest { BatchCount = 1, BuyPath = "PC_H5_COMPONENT_DETAIL", Diamond = 0, EntityId = 0, ItemId = eid, ItemLevel = 0, LastPlayTime = 0, PurchaseTime = 0, TotalPlayTime = 0, UserId = uid }, JsonOpts);
        return JsonSerializer.Deserialize<EntityResponse>(await PostWithToken(_gw, "/user-item-purchase", body, uid, tok))!;
    }

    public EntityResponse SetSkin(string uid, string tok, string eid) => ApplySkin(uid, tok, eid).GetAwaiter().GetResult();
    public Task<EntityResponse> SetSkinAsync(string uid, string tok, string eid) => ApplySkin(uid, tok, eid);

    private async Task<EntityResponse> ApplySkin(string uid, string tok, string eid)
    {
        var settings = new[] { 9, 8, 2, 10, 7 }.Select(gt => new EntitySkinSettings { ClientType = "java", GameType = gt, SkinId = eid, SkinMode = 0, SkinType = 31 }).ToList();
        var body = JsonSerializer.Serialize(new { skin_settings = settings }, JsonOpts);
        return JsonSerializer.Deserialize<EntityResponse>(await PostWithToken(_gw, "/user-game-skin-multi", body, uid, tok))!;
    }

    public List<EntityUserGameTexture> GetSkinListInGame(string uid, string tok, EntityUserGameTextureRequest req) => FetchGameSkins(uid, tok, req).GetAwaiter().GetResult();
    public Task<List<EntityUserGameTexture>> GetSkinListInGameAsync(string uid, string tok, EntityUserGameTextureRequest req) => FetchGameSkins(uid, tok, req);

    private async Task<List<EntityUserGameTexture>> FetchGameSkins(string uid, string tok, EntityUserGameTextureRequest req)
    {
        var body = JsonSerializer.Serialize(req, JsonOpts);
        var result = JsonSerializer.Deserialize<Entities<EntityUserGameTexture>>(await PostWithToken(_api, "/user-game-skin/query/search-by-type", body, uid, tok))!;
        return result.Data.ToList();
    }

    public async Task<Entity<EntityQuerySearchByGameResponse>> GetGameCoreModListAsync(string uid, string tok, EnumGameVersion ver, bool isRental)
    {
        var body = JsonSerializer.Serialize(new EntityQuerySearchByGameRequest { McVersionId = (int)ver, GameType = isRental ? 8 : 2 }, JsonOpts);
        return JsonSerializer.Deserialize<Entity<EntityQuerySearchByGameResponse>>(await PostWithToken(_api, "/game-auth-item-list/query/search-by-game", body, uid, tok))!;
    }

    public async Task<Entities<EntityComponentDownloadInfoResponse>> GetGameCoreModDetailsListAsync(string uid, string tok, List<ulong> mods)
    {
        var body = JsonSerializer.Serialize(new EntitySearchByIdsQuery { ItemIdList = mods }, JsonOpts);
        return JsonSerializer.Deserialize<Entities<EntityComponentDownloadInfoResponse>>(await PostWithToken(_api, "/user-item-download-v2/get-list", body, uid, tok))!;
    }

    public Entity<EntityCoreLibResponse> GetMinecraftClientLibs(string uid, string tok, EnumGameVersion? ver = null) => FetchClientLibs(uid, tok, ver).GetAwaiter().GetResult();
    public Task<Entity<EntityCoreLibResponse>> GetMinecraftClientLibsAsync(string uid, string tok, EnumGameVersion? ver = null) => FetchClientLibs(uid, tok, ver);

    private async Task<Entity<EntityCoreLibResponse>> FetchClientLibs(string uid, string tok, EnumGameVersion? ver)
    {
        var body = JsonSerializer.Serialize(new EntityMcDownloadVersion { McVersion = (int)(ver ?? EnumGameVersion.NONE) }, JsonOpts);
        return JsonSerializer.Deserialize<Entity<EntityCoreLibResponse>>(await PostWithToken(_lobby, "/game-patch-info", body, uid, tok))!;
    }

    public Entity<EntityComponentDownloadInfoResponse> GetNetGameComponentDownloadList(string uid, string tok, string gid) => FetchComponentList(uid, tok, gid).GetAwaiter().GetResult();
    public Task<Entity<EntityComponentDownloadInfoResponse>> GetNetGameComponentDownloadListAsync(string uid, string tok, string gid) => FetchComponentList(uid, tok, gid);

    private async Task<Entity<EntityComponentDownloadInfoResponse>> FetchComponentList(string uid, string tok, string gid)
    {
        var body = JsonSerializer.Serialize(new EntitySearchByItemIdQuery { ItemId = gid, Length = 0, Offset = 0 }, JsonOpts);
        return JsonSerializer.Deserialize<Entity<EntityComponentDownloadInfoResponse>>(await PostWithToken(_lobby, "/user-item-download-v2", body, uid, tok))!;
    }

    public Entities<EntityRentalGame> GetRentalGameList(string uid, string tok, int off) => FetchRentals(uid, tok, off).GetAwaiter().GetResult();
    public Task<Entities<EntityRentalGame>> GetRentalGameListAsync(string uid, string tok, int off) => FetchRentals(uid, tok, off);

    private async Task<Entities<EntityRentalGame>> FetchRentals(string uid, string tok, int off)
    {
        var body = JsonSerializer.Serialize(new EntityQueryRentalGame { Offset = off, SortType = 0 }, JsonOpts);
        var resp = await _rent.PostAsync("/rental-server/query/available-public-server", body, b => b.AddHeader(TokenUtil.ComputeHttpRequestToken(b.Url, b.Body, uid, tok)));
        return JsonSerializer.Deserialize<Entities<EntityRentalGame>>(await resp.Content.ReadAsStringAsync(), EnumOpts)!;
    }

    public Entities<EntityRentalGamePlayerList> GetRentalGameRolesList(string uid, string tok, string eid) => FetchRentalRoles(uid, tok, eid).GetAwaiter().GetResult();
    public Task<Entities<EntityRentalGamePlayerList>> GetRentalGameRolesListAsync(string uid, string tok, string eid) => FetchRentalRoles(uid, tok, eid);

    private async Task<Entities<EntityRentalGamePlayerList>> FetchRentalRoles(string uid, string tok, string eid)
    {
        var body = JsonSerializer.Serialize(new EntityQueryRentalGamePlayerList { ServerId = eid, Offset = 0, Length = 10 }, JsonOpts);
        return JsonSerializer.Deserialize<Entities<EntityRentalGamePlayerList>>(await PostWithToken(_rent, "/rental-server-player/query/search-by-user-server", body, uid, tok))!;
    }

    public Entity<EntityRentalGamePlayerList> AddRentalGameRole(string uid, string tok, string sid, string name) => CreateRentalRole(uid, tok, sid, name).GetAwaiter().GetResult();
    public Task<Entity<EntityRentalGamePlayerList>> AddRentalGameRoleAsync(string uid, string tok, string sid, string name) => CreateRentalRole(uid, tok, sid, name);

    private async Task<Entity<EntityRentalGamePlayerList>> CreateRentalRole(string uid, string tok, string sid, string name)
    {
        var body = JsonSerializer.Serialize(new EntityAddRentalGameRole { ServerId = sid, UserId = uid, Name = name, CreateTs = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % int.MaxValue), IsOnline = false, Status = 0 }, JsonOpts);
        return JsonSerializer.Deserialize<Entity<EntityRentalGamePlayerList>>(await PostWithToken(_rent, "/rental-server-player", body, uid, tok))!;
    }

    public Entity<EntityRentalGamePlayerList> DeleteRentalGameRole(string uid, string tok, string eid) => RemoveRentalRole(uid, tok, eid).GetAwaiter().GetResult();
    public Task<Entity<EntityRentalGamePlayerList>> DeleteRentalGameRoleAsync(string uid, string tok, string eid) => RemoveRentalRole(uid, tok, eid);

    private async Task<Entity<EntityRentalGamePlayerList>> RemoveRentalRole(string uid, string tok, string eid)
    {
        var body = JsonSerializer.Serialize(new EntityDeleteRentalGameRole { EntityId = eid }, JsonOpts);
        return JsonSerializer.Deserialize<Entity<EntityRentalGamePlayerList>>(await PostWithToken(_rent, "/rental-server-player/delete", body, uid, tok))!;
    }

    public Entity<EntityRentalGameServerAddress> GetRentalGameServerAddress(string uid, string tok, string eid, string? pwd = null) => FetchRentalAddr(uid, tok, eid, pwd).GetAwaiter().GetResult();
    public Task<Entity<EntityRentalGameServerAddress>> GetRentalGameServerAddressAsync(string uid, string tok, string eid, string? pwd = null) => FetchRentalAddr(uid, tok, eid, pwd);

    private async Task<Entity<EntityRentalGameServerAddress>> FetchRentalAddr(string uid, string tok, string eid, string? pwd)
    {
        var body = JsonSerializer.Serialize(new EntityQueryRentalGameServerAddress { ServerId = eid, Password = pwd ?? "none" }, JsonOpts);
        return JsonSerializer.Deserialize<Entity<EntityRentalGameServerAddress>>(await PostWithToken(_rent, "/rental-server-world-enter/get", body, uid, tok))!;
    }

    public Entity<EntityRentalGameDetails> GetRentalGameDetails(string uid, string tok, string eid) => FetchRentalDetails(uid, tok, eid).GetAwaiter().GetResult();
    public Task<Entity<EntityRentalGameDetails>> GetRentalGameDetailsAsync(string uid, string tok, string eid) => FetchRentalDetails(uid, tok, eid);

    private async Task<Entity<EntityRentalGameDetails>> FetchRentalDetails(string uid, string tok, string eid)
    {
        var body = JsonSerializer.Serialize(new EntityQueryRentalGameDetail { ServerId = eid }, JsonOpts);
        var resp = await _rent.PostAsync("/rental-server-details/get", body, b => b.AddHeader(TokenUtil.ComputeHttpRequestToken(b.Url, b.Body, uid, tok)));
        return JsonSerializer.Deserialize<Entity<EntityRentalGameDetails>>(await resp.Content.ReadAsStringAsync(), EnumOpts)!;
    }

    public Entities<EntityRentalGame> SearchRentalGameByName(string uid, string tok, string wid) => SearchRental(uid, tok, wid).GetAwaiter().GetResult();
    public Task<Entities<EntityRentalGame>> SearchRentalGameByNameAsync(string uid, string tok, string wid) => SearchRental(uid, tok, wid);

    private async Task<Entities<EntityRentalGame>> SearchRental(string uid, string tok, string wid)
    {
        var body = JsonSerializer.Serialize(new EntityQueryRentalGameById { Offset = 0, SortType = EnumSortType.General, WorldNameKey = new List<string> { wid } }, JsonOpts);
        var resp = await _rent.PostAsync("/rental-server/query/available-public-server", body, b => b.AddHeader(TokenUtil.ComputeHttpRequestToken(b.Url, b.Body, uid, tok)));
        return JsonSerializer.Deserialize<Entities<EntityRentalGame>>(await resp.Content.ReadAsStringAsync(), EnumOpts)!;
    }

    private static async Task<string> PostWithToken(HttpWrapper client, string path, string body, string uid, string tok)
    {
        var resp = await client.PostAsync(path, body, b => b.AddHeader(TokenUtil.ComputeHttpRequestToken(b.Url, b.Body, uid, tok)));
        return await resp.Content.ReadAsStringAsync();
    }
}
