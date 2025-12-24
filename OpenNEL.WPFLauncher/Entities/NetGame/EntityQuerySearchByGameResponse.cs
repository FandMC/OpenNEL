using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntityQuerySearchByGameResponse
{
    [JsonPropertyName("mc_version_id")]
    public int McVersionId { get; set; }

    [JsonPropertyName("game_type")]
    public int GameType { get; set; }

    [JsonPropertyName("iid_list")]
    public List<ulong> IidList { get; set; } = new();

    [JsonPropertyName("item_id_list")]
    public List<ulong> ItemIdList { get; set; } = new();
}
