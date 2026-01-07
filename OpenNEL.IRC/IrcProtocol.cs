/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
namespace OpenNEL.IRC;

public static class IrcProtocol
{
    public const string Host = "api.fandmc.cn";
    public const int Port = 9527;

    public static string Report(string token, string hwid, string serverId, string player) 
        => $"REPORT|{token}|{hwid}|{serverId}|{player}";

    public static string Get(string token, string hwid, string serverId) 
        => $"GET|{token}|{hwid}|{serverId}|";

    public static string Chat(string token, string hwid, string serverId, string player, string msg) 
        => $"CHAT|{token}|{hwid}|{serverId}|{player}|{msg}";

    public static string Del(string token, string hwid, string serverId, string player) 
        => $"DEL|{token}|{hwid}|{serverId}|{player}";

    public static IrcMessage? Parse(string line)
    {
        var p = line.Split('|');
        if (p.Length < 2) return null;

        return new IrcMessage
        {
            Type = p[0],
            Data = p[1],
            Parts = p
        };
    }
}

public class IrcMessage
{
    public string Type { get; set; } = "";
    public string Data { get; set; } = "";
    public string[] Parts { get; set; } = Array.Empty<string>();

    public bool IsOk => Type == "OK";
    public bool IsChat => Type == "CHAT" && Parts.Length >= 4;
    public bool IsPlayerList => IsOk && Data.StartsWith("[");
}
