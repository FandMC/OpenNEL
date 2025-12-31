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

namespace OpenNEL.MPay.Entities;

public sealed class EntityPcExtInfo
{
    [JsonPropertyName("qr_expire_time")]
    public int QrExpireTime { get; set; }

    [JsonPropertyName("wps_token")]
    public string WpsToken { get; set; } = string.Empty;

    [JsonPropertyName("wps_refresh_token")]
    public string WpsRefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("wps_uid")]
    public string WpsUid { get; set; } = string.Empty;

    [JsonPropertyName("wps_nick_name")]
    public string WpsNickName { get; set; } = string.Empty;

    [JsonPropertyName("wps_avatar")]
    public string WpsAvatar { get; set; } = string.Empty;
}
