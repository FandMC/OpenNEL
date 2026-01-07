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

public class EntityAddMod
{
	[JsonPropertyName("file_name")]
	public string FileName { get; set; } = string.Empty;

	[JsonPropertyName("base64")]
	public string Base64 { get; set; } = string.Empty;
}
