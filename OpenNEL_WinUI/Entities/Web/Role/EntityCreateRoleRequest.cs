/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.Text.Json.Serialization;

namespace OpenNEL_WinUI.Entities.Web.Role;

public class EntityCreateRoleRequest
{
	[JsonPropertyName("id")]
	public required string UserId { get; set; }

	[JsonPropertyName("name")]
	public required string RoleName { get; set; }

	[JsonPropertyName("game")]
	public required string GameId { get; set; }

	[JsonPropertyName("type")]
	public required string GameType { get; set; }
}
