using System.Text.Json.Serialization;

namespace OpenNEL.Entities.Web.NEL;

public class EntityPasswordRequest
{
	[JsonPropertyName("account")]
	public required string Account { get; set; }

	[JsonPropertyName("password")]
	public required string Password { get; set; }

	[JsonPropertyName("captcha_identifier")]
	public string? CaptchaIdentifier { get; set; }

	[JsonPropertyName("captcha")]
	public string? Captcha { get; set; }
}
