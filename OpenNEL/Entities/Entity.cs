using System;
using System.Text.Json.Serialization;
using OpenNEL.Extensions;

namespace OpenNEL.Entities;

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

	public void PrepareSignatureAndIdentify(string? identify)
	{
		Identify = identify;
		if (Payload == null)
		{
			throw new InvalidOperationException("Cannot generate signature:  Payload is null.");
		}
		Sign = Payload.ToSha256();
	}
}
