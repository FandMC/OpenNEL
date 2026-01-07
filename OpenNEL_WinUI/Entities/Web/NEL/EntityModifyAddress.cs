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

public class EntityModifyAddress
{
	[JsonPropertyName("interceptor_id")]
	public string InterceptorId { get; set; } = string.Empty;

	[JsonPropertyName("ip")]
	public string IpAddress { get; set; } = string.Empty;

	[JsonPropertyName("port")]
	public string Port { get; set; } = string.Empty;
}
