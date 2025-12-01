namespace OpenNEL.Utils;

public class Dev
{
    public static bool Get()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var a in args)
            {
                if (string.Equals(a, "--dev", StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        catch { }
        var env = Environment.GetEnvironmentVariable("NEL_DEV");
        return string.Equals(env, "1") || string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
    }
}