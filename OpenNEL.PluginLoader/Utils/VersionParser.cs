using System;
using Serilog;

namespace OpenNEL.PluginLoader.Utils;

public static class VersionParser
{
    public static Version Parse(string version)
    {
        try
        {
            var parts = version.Split('.');
            int major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 0;
            int minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
            int build = parts.Length > 2 && int.TryParse(parts[2], out var b) ? b : 0;
            return new Version(major, minor, build);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to parse plugin version: {Version}. Using default 0.0.0", version);
            return new Version(0, 0, 0);
        }
    }
}
