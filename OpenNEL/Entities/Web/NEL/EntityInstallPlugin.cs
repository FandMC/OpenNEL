using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenNEL.Entities.Web.NEL;

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
