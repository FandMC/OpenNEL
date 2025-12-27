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
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OpenNEL_WinUI.type;
using CoreHwid = OpenNEL.Core.Utils.Hwid;

namespace OpenNEL_WinUI.Utils;

internal static class Hwid
{
    public static string Compute() => CoreHwid.Compute();

    public static async Task<string?> ReportAsync(string? hwid = null, string? endpoint = null)
    {
        try
        {
            var h = hwid ?? Compute();
            var ip = CoreHwid.GetLocalIp();
            var url = endpoint ?? AppInfo.HwidEndpoint;
            using var client = new HttpClient();
            var payload = "{\"hwid\":\"" + h + "\",\"ip\":\"" + ip + "\"}";
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync(url, content);
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }
            return text;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}
