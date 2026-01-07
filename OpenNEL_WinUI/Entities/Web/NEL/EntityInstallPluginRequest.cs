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

public class EntityInstallPluginRequest
{
	[JsonPropertyName("id")]
	public required string Id { get; set; }

	[JsonPropertyName("plugin")]
	public required EntityInstallPlugin Plugin { get; set; }
}
