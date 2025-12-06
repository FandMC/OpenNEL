using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Codexus.Development.SDK.Manager;
using Serilog;

namespace OpenNEL.Utils;

public static class PluginLoader
{
    static readonly string Dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
    static readonly HashSet<string> Seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static CancellationTokenSource? Cts;

    public static void Initialize()
    {
        Directory.CreateDirectory(Dir);
        try
        {
            PluginManager.Instance.EnsureUninstall();
            PluginManager.Instance.LoadPlugins(Dir);
            Log.Information("识别了 {Count} 个插件", PluginManager.Instance.Plugins.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "插件加载失败");
        }
        RefreshSeen();
        Start();
    }

    static void RefreshSeen()
    {
        try
        {
            Seen.Clear();
            foreach (var f in Directory.EnumerateFiles(Dir))
            {
                var name = Path.GetFileName(f);
                if (!string.IsNullOrEmpty(name)) Seen.Add(name);
            }
        }
        catch { }
    }

    static void Start()
    {
        Cts?.Cancel();
        Cts = new CancellationTokenSource();
        var token = Cts.Token;
        var msEnv = Environment.GetEnvironmentVariable("OPENNEL_PLUGIN_POLL_MS");
        int interval = 500;
        if (int.TryParse(msEnv, out var v) && v > 0) interval = v;
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    bool hasNew = false;
                    foreach (var f in Directory.EnumerateFiles(Dir))
                    {
                        var name = Path.GetFileName(f);
                        if (string.IsNullOrEmpty(name)) continue;
                        if (!Seen.Contains(name)) { hasNew = true; break; }
                    }
                    if (hasNew)
                    {
                        try { PluginManager.Instance.LoadPlugins(Dir); }
                        catch (Exception ex) { Log.Error(ex, "插件热加载失败"); }
                        finally { RefreshSeen(); }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "插件热加载失败");
                }
                try { await Task.Delay(interval, token); } catch { }
            }
        }, token);
    }
}
