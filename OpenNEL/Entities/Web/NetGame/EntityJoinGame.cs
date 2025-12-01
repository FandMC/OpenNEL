using System.Text.Json.Serialization;
using Codexus.Development.SDK.Entities;

namespace OpenNEL.Entities.Web.NetGame;

public class EntityJoinGame
{
	[JsonPropertyName("id")]
	public string UserId { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string GameName { get; set; } = string.Empty;

	[JsonPropertyName("game")]
	public string GameId { get; set; } = string.Empty;

	[JsonPropertyName("role")]
	public string Role { get; set; } = string.Empty;

	[JsonPropertyName("vid")]
	public int VersionId { get; set; }

	[JsonPropertyName("version")]
	public string Version { get; set; } = string.Empty;

	[JsonPropertyName("ip")]
	public string ServerIp { get; set; } = string.Empty;

	[JsonPropertyName("port")]
	public int ServerPort { get; set; }

	[JsonPropertyName("nid")]
	public string NexusId { get; set; } = string.Empty;

	[JsonPropertyName("token")]
	public string NexusToken { get; set; } = string.Empty;
	
	[JsonPropertyName("serverId")]
	public string ServerId { get; set; } = string.Empty;
	
	[JsonPropertyName("serverName")]
	public string ServerName { get; set; } = string.Empty;

	[JsonPropertyName("socks5")]
	public EntitySocks5 Socks5 { get; set; } = new EntitySocks5();
}
