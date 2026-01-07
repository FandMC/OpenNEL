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

public class EntityLoginRequest
{
	[JsonPropertyName("channel")]
	public string Channel { get; set; } = string.Empty;

	[JsonPropertyName("type")]
	public string Type { get; set; } = string.Empty;

	[JsonPropertyName("details")]
	public string Details { get; set; } = string.Empty;

	[JsonPropertyName("platform")]
	public Platform Platform { get; set; }

	[JsonPropertyName("token")]
	public string Token { get; set; } = string.Empty;
}
