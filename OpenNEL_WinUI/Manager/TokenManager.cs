/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Collections.Generic;
using Serilog;

namespace OpenNEL_WinUI.Manager;

public class TokenManager
{
	private static TokenManager? _instance;

	private readonly Dictionary<string, string> _tokens = new Dictionary<string, string>();

	public static TokenManager Instance => _instance ?? (_instance = new TokenManager());

	public void UpdateToken(string id, string token)
	{
		try
		{
			if (!_tokens.TryAdd(id, token))
			{
				_tokens[id] = token;
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error while updating access token, {exception}", ex.Message);
		}
	}

	public void RemoveToken(string entityId)
	{
		_tokens.Remove(entityId);
	}

	public string GetToken(string entityId)
	{
		if (!_tokens.TryGetValue(entityId, out string value))
		{
			return string.Empty;
		}
		return value;
	}
}
