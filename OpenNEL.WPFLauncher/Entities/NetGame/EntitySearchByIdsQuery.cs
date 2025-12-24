using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntitySearchByIdsQuery
{
    [JsonPropertyName("item_id_list")]
    public List<ulong> ItemIdList { get; set; } = new();
}
