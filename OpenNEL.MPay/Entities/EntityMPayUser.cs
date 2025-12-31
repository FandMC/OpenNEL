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

public sealed class EntityMPayUser
{
    [JsonPropertyName("ext_access_token")]
    public string ExtAccessToken { get; set; } = string.Empty;

    [JsonPropertyName("realname_verify_status")]
    public int RealNameVerifyStatus { get; set; }

    [JsonPropertyName("login_channel")]
    public string LoginChannel { get; set; } = string.Empty;

    [JsonPropertyName("realname_status")]
    public int RealNameStatus { get; set; }

    [JsonPropertyName("related_login_status")]
    public int RelatedLoginStatus { get; set; }

    [JsonPropertyName("need_mask")]
    public bool NeedMask { get; set; }

    [JsonPropertyName("mobile_bind_status")]
    public int MobileBindStatus { get; set; }

    [JsonPropertyName("mask_related_mobile")]
    public string MaskRelatedMobile { get; set; } = string.Empty;

    [JsonPropertyName("display_username")]
    public string DisplayUsername { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("client_username")]
    public string ClientUsername { get; set; } = string.Empty;

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; } = string.Empty;

    [JsonPropertyName("need_aas")]
    public bool NeedAas { get; set; }

    [JsonPropertyName("login_type")]
    public int LoginType { get; set; }

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("pc_ext_info")]
    public EntityPcExtInfo PcExtInfo { get; set; } = new();
}
