using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using Serilog;

namespace OpenNEL.PluginLoader.Services;

#nullable enable

public class PluginUninstaller
{
    private const string UninstallCacheFile = ".ug_cache";
    private readonly Lock _fileLock = new();

    public void EnsureUninstall()
    {
        using (_fileLock.EnterScope())
        {
            if (!File.Exists(UninstallCacheFile))
            {
                File.WriteAllText(UninstallCacheFile, JsonSerializer.Serialize(new HashSet<string>()));
                return;
            }

            var pendingUninstalls = JsonSerializer.Deserialize<HashSet<string>>(
                File.ReadAllText(UninstallCacheFile));
            
            if (pendingUninstalls == null) return;

            foreach (var path in pendingUninstalls)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        Log.Information("Deleted plugin file: {Path}", path);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to delete plugin file: {Path}", path);
                }
            }

            File.Delete(UninstallCacheFile);
            File.WriteAllText(UninstallCacheFile, JsonSerializer.Serialize(new HashSet<string>()));
        }
    }

    public void MarkForUninstall(string pluginPath)
    {
        MarkForUninstall(new List<string> { pluginPath });
    }

    public void MarkForUninstall(List<string> paths)
    {
        using (_fileLock.EnterScope())
        {
            var pendingUninstalls = JsonSerializer.Deserialize<HashSet<string>>(
                File.ReadAllText(UninstallCacheFile));
            
            if (pendingUninstalls == null)
            {
                Log.Error("Failed to read uninstall cache file");
                return;
            }

            foreach (var path in paths)
            {
                pendingUninstalls.Add(path);
            }

            File.WriteAllText(UninstallCacheFile, JsonSerializer.Serialize(pendingUninstalls));
        }
    }
}
