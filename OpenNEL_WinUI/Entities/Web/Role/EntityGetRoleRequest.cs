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

public class EntityGetRoleRequest
{
	[JsonPropertyName("id")]
	public string UserId { get; set; } = string.Empty;

	[JsonPropertyName("game")]
	public string GameId { get; set; } = string.Empty;

	[JsonPropertyName("type")]
	public string GameType { get; set; } = string.Empty;
}
