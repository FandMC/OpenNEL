using System;
using System.Collections.Generic;
using OpenNEL.Manager;

namespace OpenNEL_WinUI.Handlers.Game;

public class ShutdownGame
{
    public object[] Execute(IEnumerable<string> identifiers)
    {
        var closed = new List<string>();
        foreach (var s in identifiers ?? Array.Empty<string>())
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
