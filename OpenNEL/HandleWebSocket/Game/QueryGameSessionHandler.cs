using OpenNEL.network;
using OpenNEL.type;
using System.Text.Json;

namespace OpenNEL.HandleWebSocket.Game;

internal class QueryGameSessionHandler : IWsHandler
{
    public string Type => "query_game_session";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var list = AppState.Channels.Values.Select(ch => new {
            Id = "interceptor-" + ch.Identifier,
            ServerName = ch.ServerName,
            CharacterName = ch.RoleName,
            ServerVersion = string.Empty,
            StatusText = "Running",
            ProgressValue = 0,
            Type = "Interceptor",
            LocalAddress = "127.0.0.1:" + ch.LocalPort,
            Identifier = ch.Identifier.ToString()
        }).ToArray();
        return new { type = "query_game_session", items = list };
    }
}
