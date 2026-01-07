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

namespace OpenNEL_WinUI.Entities;

public class Entity
{
	[JsonPropertyName("identify")]
	public string? Identify { get; set; }

	[JsonPropertyName("type")]
	public string? Type { get; set; }

	[JsonPropertyName("payload")]
	public string? Payload { get; set; }

	[JsonPropertyName("sign")]
	public string? Sign { get; set; }

	public Entity()
	{
	}

	public Entity(string type, string payload)
	{
		Type = type ?? throw new ArgumentNullException("type");
		Payload = payload ?? throw new ArgumentNullException("payload");
	}
}
