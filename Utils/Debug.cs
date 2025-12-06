using System;

namespace OpenNEL.Utils;

public class Debug
{
    public static bool Get()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var a in args)
            {
                if (string.Equals(a, "--debug", StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        catch { }
        var env = Environment.GetEnvironmentVariable("NEL_DEBUG");
        return string.Equals(env, "1") || string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
    }
}