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

public class EntityActiveWithCaptcha
{
	[JsonPropertyName("id")]
	public required string UserId { get; set; }

	[JsonPropertyName("identifier")]
	public required string Identifier { get; set; }

	[JsonPropertyName("captcha")]
	public required string Captcha { get; set; }
}
