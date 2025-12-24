using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.RentalGame;

public class EntityQueryRentalGamePlayerList
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }
}
