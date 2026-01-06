/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System.Text.Json.Serialization;

namespace OpenNEL_WinUI.type;

public class SettingData
{
    [JsonPropertyName("themeMode")] public string ThemeMode { get; set; } = "image";
    [JsonPropertyName("themeColor")] public string ThemeColor { get; set; } = "#181818";
    [JsonPropertyName("themeImage")] public string ThemeImage { get; set; } = string.Empty;
    [JsonPropertyName("backdrop")] public string Backdrop { get; set; } = "mica";
    [JsonPropertyName("autoCopyIpOnStart")] public bool AutoCopyIpOnStart { get; set; } = false;
    [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
    [JsonPropertyName("autoDisconnectOnBan")] public string AutoDisconnectOnBan { get; set; } = "none";
    [JsonPropertyName("ircEnabled")] public bool IrcEnabled { get; set; } = true;
    [JsonPropertyName("weakNetwork")] public bool WeakNetwork { get; set; } = false;
    [JsonPropertyName("socks5Enabled")] public bool Socks5Enabled { get; set; } = false;

    [JsonPropertyName("socks5Address")] public string Socks5Address { get; set; } = string.Empty;
    [JsonPropertyName("socks5Port")] public int Socks5Port { get; set; } = 1080;
    [JsonPropertyName("socks5Username")] public string Socks5Username { get; set; } = string.Empty;
    [JsonPropertyName("socks5Password")] public string Socks5Password { get; set; } = string.Empty;
}
