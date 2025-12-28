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
using System.Collections.Generic;
using OpenNEL_WinUI.Manager;

namespace OpenNEL_WinUI.Handlers.Game.NetServer;

public class ShutdownGame
{
    public object[] Execute(IEnumerable<string> identifiers)
    {
        var closed = new List<string>();
        foreach (var s in identifiers)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            if (Guid.TryParse(s, out var id))
            {
                GameManager.Instance.ShutdownInterceptor(id);
                closed.Add(s);
            }
        }
        var payloads = new object[]
        {
            new { type = "shutdown_ack", identifiers = closed.ToArray() },
            new { type = "channels_updated" }
        };
        return payloads;
    }
}
