using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.X19;

public class X19GameStart
{
    [JsonPropertyName("game_id")] public required string GameId { get; set; }

    [JsonPropertyName("game_type")] public string GameType { get; set; } = "2";

    [JsonPropertyName("strict_mode")] public bool StrictMode { get; set; } = true;

    [JsonPropertyName("item_list")] public required string[] ItemList { get; set; }
}