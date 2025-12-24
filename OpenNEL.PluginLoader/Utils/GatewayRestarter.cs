using System;
using System.Diagnostics;
using System.Linq;
using Serilog;

namespace OpenNEL.PluginLoader.Utils;

#nullable enable

public static class GatewayRestarter
{
    public static void Restart()
    {
        try
        {
            string? executablePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(executablePath))
            {
                using var process = Process.GetCurrentProcess();
                executablePath = process.MainModule?.FileName;
            }

            if (string.IsNullOrEmpty(executablePath))
            {
                Log.Error("Failed to determine executable path.");
                return;
            }

            var arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1));
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                UseShellExecute = true
            };

            Log.Information("Preparing to restart gateway, Path: {ExecutablePath}, Arguments: {Arguments}", 
                executablePath, arguments);
            
            Process.Start(startInfo);
            Log.Information("New process started.");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to restart gateway.");
        }
    }
}
