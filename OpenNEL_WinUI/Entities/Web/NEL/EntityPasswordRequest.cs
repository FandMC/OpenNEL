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

public class EntityPasswordRequest
{
	[JsonPropertyName("account")]
	public required string Account { get; set; }

	[JsonPropertyName("password")]
	public required string Password { get; set; }

	[JsonPropertyName("captcha_identifier")]
	public string? CaptchaIdentifier { get; set; }

	[JsonPropertyName("captcha")]
	public string? Captcha { get; set; }
}
