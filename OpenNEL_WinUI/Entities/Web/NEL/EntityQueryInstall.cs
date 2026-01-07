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

public class EntityQueryInstall
{
	[JsonPropertyName("id")]
	public string PluginId { get; set; } = string.Empty;

	[JsonPropertyName("version")]
	public string PluginVersion { get; set; } = string.Empty;

	[JsonPropertyName("status")]
	public bool PluginIsInstalled { get; set; }

	[JsonPropertyName("installed")]
	public string PluginInstalledVersion { get; set; } = string.Empty;
}
