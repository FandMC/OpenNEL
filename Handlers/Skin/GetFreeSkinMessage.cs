using System.Linq;
using OpenNEL.Manager;
using OpenNEL.type;
using Serilog;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame.Skin;
using System.Text.Json;
using OpenNEL.Utils;
using Codexus.OpenSDK.Entities.X19;

namespace OpenNEL_WinUI.Handlers.Skin;

public class GetFreeSkin
{
    public object Execute(int offset, int length = 20)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        try
        {
            Log.Information("免费皮肤请求 offset={Offset} length={Length}", offset, length);
            Entities<EntitySkin> list;
            try
            {
                list = AppState.X19.GetFreeSkinList(last.UserId, last.AccessToken, offset, length);
                // Log.Information("免费皮肤响应: {Json}", JsonSerializer.Serialize(list));
            }
            catch (JsonException ex)
            {
                if (AppState.Debug) Log.Error(ex, "获取皮肤列表失败");
                var otp = new X19AuthenticationOtp { EntityId = last.UserId, Token = last.AccessToken };
                var body = JsonSerializer.Serialize(new EntityFreeSkinListRequest
                {
                    IsHas = true,
                    ItemType = 2,
                    Length = length,
                    MasterTypeId = 10,
                    Offset = offset,
                    PriceType = 3,
                    SecondaryTypeId = 31
                });
                var raw = otp.ApiRaw("/item/query/available", body).GetAwaiter().GetResult();
                // Log.Information("免费皮肤原始响应: {Json}", raw ?? string.Empty);
                try
                {
                    using var doc = JsonDocument.Parse(raw ?? "{}");
                    var root = doc.RootElement;
                    JsonElement listEl = default;
                    foreach (var p in root.EnumerateObject())
                    {
                        if (string.Equals(p.Name, "entities", System.StringComparison.OrdinalIgnoreCase)) { listEl = p.Value; break; }
                        if (string.Equals(p.Name, "data", System.StringComparison.OrdinalIgnoreCase)) { listEl = p.Value; break; }
                    }
                    if (listEl.ValueKind == JsonValueKind.Array)
                    {
                        var rawItems = listEl.EnumerateArray().Select(el =>
                        {
                            string Get(string name)
                            {
                                foreach (var pp in el.EnumerateObject()) if (string.Equals(pp.Name, name, System.StringComparison.OrdinalIgnoreCase)) return pp.Value.GetString() ?? string.Empty;
                                return string.Empty;
                            }
                            var id = Get("entity_id");
                            var name = Get("name");
                            var preview = Get("title_image_url");
                            return new { entityId = id, name, previewUrl = preview };
                        }).ToArray();
                        var hasMore2 = rawItems.Length >= length;
                        try
                        {
                            var stubs = rawItems.Select(e => new EntitySkin { EntityId = e.entityId ?? string.Empty, Name = e.name ?? string.Empty, TitleImageUrl = e.previewUrl ?? string.Empty, BriefSummary = string.Empty, LikeNum = 0 }).ToArray();
                            var stubList = new Entities<EntitySkin> { Data = stubs };
                            var detailed2 = AppState.X19.GetSkinDetails(last.UserId, last.AccessToken, stubList);
                            var data2 = detailed2?.Data ?? stubs;
                            var items2 = data2.Select(s =>
                            {
                                var t = s.GetType();
                                var id = t.GetProperty("EntityId")?.GetValue(s) as string ?? string.Empty;
                                var name = t.GetProperty("Name")?.GetValue(s) as string ?? string.Empty;
                                var preview = t.GetProperty("TitleImageUrl")?.GetValue(s) as string ?? string.Empty;
                                if (string.IsNullOrWhiteSpace(preview))
                                {
                                    var ti = t.GetProperty("TitleImage")?.GetValue(s);
                                    preview = ti?.GetType().GetProperty("Url")?.GetValue(ti) as string ?? string.Empty;
                                }
                                return new { entityId = id, name, previewUrl = preview };
                            }).ToArray();
                            return new { type = "skins", items = items2, hasMore = hasMore2 };
                        }
                        catch (System.Exception ex3)
                        {
                            if (AppState.Debug) Log.Error(ex3, "通过详情获取皮肤图片失败，返回基础列表");
                            return new { type = "skins", items = rawItems, hasMore = hasMore2 };
                        }
                    }
                    return new { type = "skins", items = System.Array.Empty<object>(), hasMore = false };
                }
                catch (System.Exception ex2)
                {
                    if (AppState.Debug) Log.Error(ex2, "皮肤原始响应解析失败");
                    return new { type = "skins_error", message = "解析失败" };
                }
            }
            var baseCount = list.Data?.Length ?? 0;
            Log.Information("免费皮肤基础数量={Count}", baseCount);
            if (baseCount > 0)
            {
                var max = System.Math.Min(5, baseCount);
                for (int i = 0; i < max; i++)
                {
                    var s = list.Data![i];
                    var t0 = s.GetType();
                    var id0 = t0.GetProperty("EntityId")?.GetValue(s) as string ?? string.Empty;
                    var name0 = t0.GetProperty("Name")?.GetValue(s) as string ?? string.Empty;
                    Log.Information("基础样例: {Name} {Id}", name0, id0);
                }
            }
            Entities<EntitySkin>? detailed = null;
            try
            {
                detailed = AppState.X19.GetSkinDetails(last.UserId, last.AccessToken, list);
                // Log.Information("皮肤详情响应: {Json}", JsonSerializer.Serialize(detailed));
            }
            catch (JsonException ex)
            {
                if (AppState.Debug) Log.Error(ex, "皮肤详情反序列化失败，退回基础列表");
            }
            var data = detailed?.Data ?? list.Data ?? System.Array.Empty<EntitySkin>();
            var detCount = detailed?.Data?.Length ?? 0;
            Log.Information("皮肤详情数量={Count}", detCount);
            var items = data.Select(s =>
            {
                var t = s.GetType();
                var id = t.GetProperty("EntityId")?.GetValue(s) as string ?? string.Empty;
                var name = t.GetProperty("Name")?.GetValue(s) as string ?? string.Empty;
                var preview = t.GetProperty("TitleImageUrl")?.GetValue(s) as string ?? string.Empty;
                if (string.IsNullOrWhiteSpace(preview))
                {
                    var ti = t.GetProperty("TitleImage")?.GetValue(s);
                    preview = ti?.GetType().GetProperty("Url")?.GetValue(ti) as string ?? string.Empty;
                }
                return new { entityId = id, name, previewUrl = preview };
            }).ToArray();
            var hasMore = (list.Data?.Length ?? 0) >= length;
            Log.Information("皮肤返回条目数={Count} hasMore={HasMore}", items.Length, hasMore);
            return new { type = "skins", items, hasMore };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取皮肤列表失败");
            return new { type = "skins_error", message = "获取失败" };
        }
    }
}
