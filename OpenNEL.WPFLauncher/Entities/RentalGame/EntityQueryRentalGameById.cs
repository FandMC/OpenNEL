using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.RentalGame;

public class EntityQueryRentalGameById
{
    [JsonPropertyName("offset")]
    public ulong Offset { get; set; }

    [JsonPropertyName("sort_type")]
    public EnumSortType SortType { get; set; }

    [JsonPropertyName("world_name_key")]
    public List<string> WorldNameKey { get; set; } = new();
}
