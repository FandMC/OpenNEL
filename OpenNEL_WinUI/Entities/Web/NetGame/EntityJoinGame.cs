/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System.Text.Json.Serialization;
using Codexus.Development.SDK.Entities;

namespace OpenNEL_WinUI.Entities.Web.NetGame;

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
