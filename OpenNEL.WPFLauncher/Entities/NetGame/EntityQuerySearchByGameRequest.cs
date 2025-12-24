using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntityQuerySearchByGameRequest
{
    [JsonPropertyName("mc_version_id")]
    public int McVersionId { get; set; }

    [JsonPropertyName("game_type")]
    public int GameType { get; set; }
}
