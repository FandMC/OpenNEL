using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.RentalGame;

public class EntityDeleteRentalGameRole
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;
}
