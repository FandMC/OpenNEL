using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenNEL.Entities;

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
