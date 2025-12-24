using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.RentalGame;

public class EntityQueryRentalGameDetail
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;
}
