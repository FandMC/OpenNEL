using System.Text.Json.Serialization;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;

namespace OpenNEL.Entities.Web.NetGame;

public class EntityNetGamesResponse
{
	[JsonPropertyName("entities")]
	public required EntityNetGameItem[] Entities { get; set; }

	[JsonPropertyName("total")]
	public required int Total { get; set; }
}
