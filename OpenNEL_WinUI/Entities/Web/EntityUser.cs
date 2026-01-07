/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.Text.Json.Serialization;
using OpenNEL_WinUI.Enums;

namespace OpenNEL_WinUI.Entities.Web;

public class EntityUser
{
	[JsonPropertyName("id")]
	public required string UserId { get; set; }

	[JsonPropertyName("authorized")]
	public required bool Authorized { get; set; }

	[JsonPropertyName("auto_login")]
	public required bool AutoLogin { get; set; }

	[JsonPropertyName("channel")]
	public required string Channel { get; set; }

	[JsonPropertyName("type")]
	public required string Type { get; set; }

	[JsonPropertyName("details")]
	public required string Details { get; set; }

	[JsonPropertyName("platform")]
	public Platform Platform { get; set; }

	[JsonPropertyName("alias")]
	public string Alias { get; set; } = string.Empty;
}
