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

namespace OpenNEL_WinUI.Entities.Web.NEL;

public class EntityUpdateUserAlias
{
	[JsonPropertyName("id")]
	public required string EntityId { get; set; }

	[JsonPropertyName("platform")]
	public required Platform Platform { get; set; }

	[JsonPropertyName("alias")]
	public required string Alias { get; set; }
}
