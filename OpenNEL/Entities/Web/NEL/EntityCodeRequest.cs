using System.Text.Json.Serialization;

namespace OpenNEL.Entities.Web.NEL;

public class EntityCodeRequest
{
	[JsonPropertyName("phone")]
	public required string Phone { get; set; }

	[JsonPropertyName("code")]
	public required string Code { get; set; }
}
