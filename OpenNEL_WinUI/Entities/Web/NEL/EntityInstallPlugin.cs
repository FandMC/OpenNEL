/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenNEL_WinUI.Entities.Web.NEL;

public class EntityInstallPlugin
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "";

	[JsonPropertyName("dependencies")]
	public List<EntityInstallPlugin> Dependencies { get; set; } = new List<EntityInstallPlugin>();

	[JsonPropertyName("downloadUrl")]
	public string DownloadUrl { get; set; } = "";

	[JsonPropertyName("fileHash")]
	public string FileHash { get; set; } = "";

	[JsonPropertyName("fileSize")]
	public int FileSize { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("version")]
	public string Version { get; set; } = "";

	public List<EntityInstallPlugin> GetAllDownloadPlugins()
	{
		List<EntityInstallPlugin> list = new List<EntityInstallPlugin> { this };
		foreach (EntityInstallPlugin dependency in Dependencies)
		{
			EntityInstallPlugin reference = dependency;
			list.AddRange(new ReadOnlySpan<EntityInstallPlugin>(in reference));
		}
		return list;
	}
}
