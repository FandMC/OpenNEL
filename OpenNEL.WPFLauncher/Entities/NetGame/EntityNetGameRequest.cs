using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntityNetGameRequest
{
    [JsonPropertyName("available_mc_versions")]
    public string[] AvailableMcVersions { get; set; } = Array.Empty<string>();

    [JsonPropertyName("item_type")]
    public int ItemType { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("master_type_id")]
    public string MasterTypeId { get; set; } = string.Empty;

    [JsonPropertyName("secondary_type_id")]
    public string SecondaryTypeId { get; set; } = string.Empty;
}
