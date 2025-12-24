using System.Text.Json;
using OpenNEL.Core.Cipher;
using OpenNEL.Core.Http;
using OpenNEL.WPFLauncher.Entities.InterConn;

namespace OpenNEL.WPFLauncher;

public static class InterConnClient
{
    private static readonly HttpWrapper Core = new HttpWrapper("https://x19obtcore.nie.netease.com:8443", builder =>
    {
        builder.UserAgent(WPFLauncherClient.GetUserAgent());
    });

    public static async Task LoginStart(string entityId, string entityToken)
    {
        var response = await Core.PostAsync("/interconn/web/game-play-v2/login-start", "{\"strict_mode\":true}", builder =>
        {
            builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, entityId, entityToken));
        });
        await response.Content.ReadAsStringAsync();
    }

    public static async Task GameStart(string entityId, string entityToken, string gameId)
    {
        var body = JsonSerializer.Serialize(new InterConnGameStart
        {
            GameId = gameId,
            ItemList = new[] { "10000" }
        });
        var response = await Core.PostAsync("/interconn/web/game-play-v2/start", body, builder =>
        {
            builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, entityId, entityToken));
        });
        await response.Content.ReadAsStringAsync();
    }
}
