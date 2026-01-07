/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.Text.Json.Serialization;

namespace OpenNEL_WinUI.Entities.Web.NEL;

public class EntityPluginsResponse
{
	[JsonPropertyName("id")]
	public required string PluginId { get; set; }

	[JsonPropertyName("name")]
	public required string PluginName { get; set; }

	[JsonPropertyName("description")]
	public required string PluginDescription { get; set; }

	[JsonPropertyName("version")]
	public required string PluginVersion { get; set; }

	[JsonPropertyName("author")]
	public required string PluginAuthor { get; set; }

	[JsonPropertyName("status")]
	public required string PluginStatus { get; set; }
}
