using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities.RentalGame;

public class EntityRentalGameRequest
{
    [JsonPropertyName("sort_type")]
    public int SortType { get; set; }

    [JsonPropertyName("order_type")]
    public int OrderType { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}
