/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenNEL_WinUI.Entities;

public class EntityResponse
{
	[JsonPropertyName("code")]
	public int Code { get; set; }

	[JsonPropertyName("message")]
	public string Message { get; set; } = string.Empty;

	[JsonPropertyName("payload")]
	public string Payload { get; set; } = string.Empty;

	public static string Success(int code, string payload)
	{
		return JsonSerializer.Serialize(new EntityResponse
		{
			Code = code,
			Message = "Success",
			Payload = payload
		});
	}

	public static string Success(string payload)
	{
		return JsonSerializer.Serialize(new EntityResponse
		{
			Code = 0,
			Message = "Success",
			Payload = payload
		});
	}

	public static string Error(int code, Exception exception)
	{
		return JsonSerializer.Serialize(new EntityResponse
		{
			Code = code,
			Message = exception.Message,
			Payload = string.Empty
		});
	}

	public static string Error(int code, string message)
	{
		return JsonSerializer.Serialize(new EntityResponse
		{
			Code = code,
			Message = message,
			Payload = string.Empty
		});
	}
}
