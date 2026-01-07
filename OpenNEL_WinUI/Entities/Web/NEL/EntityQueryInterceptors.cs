/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Text.Json.Serialization;

namespace OpenNEL_WinUI.Entities.Web.NEL;

public class EntityQueryInterceptors
{
	[JsonPropertyName("id")]
	public required string Id { get; set; }

	[JsonPropertyName("name")]
	public required Guid Name { get; set; }

	[JsonPropertyName("address")]
	public required string Address { get; set; }

	[JsonPropertyName("role")]
	public required string Role { get; set; }

	[JsonPropertyName("server")]
	public required string Server { get; set; }

	[JsonPropertyName("version")]
	public required string Version { get; set; }

	[JsonPropertyName("local")]
	public required string LocalAddress { get; set; }
}
