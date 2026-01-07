/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenNEL.IRC;

public class IrcPlayerList
{
    readonly ConcurrentDictionary<string, string> _players = new();

    public int Count => _players.Count;
    public IReadOnlyDictionary<string, string> All => _players;

    public void Update(string json)
    {
        var list = JsonSerializer.Deserialize<PlayerData[]>(json);
        _players.Clear();
        if (list != null)
            foreach (var p in list)
                _players[p.PlayerName] = p.Username;
    }

    public void Clear() => _players.Clear();

    class PlayerData
    {
        [JsonPropertyName("Username")] public string Username { get; set; } = "";
        [JsonPropertyName("PlayerName")] public string PlayerName { get; set; } = "";
    }
}
