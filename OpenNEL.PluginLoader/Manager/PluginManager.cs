using System;
using System.Collections.Generic;
using System.Reflection;
using OpenNEL.PluginLoader.Entities;
using OpenNEL.PluginLoader.Services;
using OpenNEL.PluginLoader.Utils;
using Serilog;

namespace OpenNEL.PluginLoader.Manager;

#nullable enable

public class PluginManager
{
    private static PluginManager? _instance;

    public readonly Dictionary<string, PluginState> Plugins = new();

    public Action<Assembly>? OnAssemblyLoaded { get; set; }

    private readonly PluginLoaderService _loader;
    private readonly PluginUninstaller _uninstaller;
    private readonly DependencyResolver _resolver;

    public static PluginManager Instance => _instance ??= new PluginManager();

    private PluginManager()
    {
        _loader = new PluginLoaderService(Plugins);
        _uninstaller = new PluginUninstaller();
        _resolver = new DependencyResolver(Plugins);
    }

    public void EnsureUninstall()
    {
        _uninstaller.EnsureUninstall();
    }

    public void LoadPlugins(string directory)
    {
        _loader.LoadFromDirectory(directory);

        _resolver.CheckDependencies();

        Log.Information("识别到 {Count} 个插件", Plugins.Count);

        _resolver.ResolveVersionConflicts();

        var order = _resolver.GetInitializationOrder();
        _resolver.InitializePlugins(order, OnAssemblyLoaded);
    }

    public bool HasPlugin(string id)
    {
        return Plugins.ContainsKey(id.ToUpper());
    }

    public PluginState GetPlugin(string id)
    {
        var upperId = id.ToUpper();
        if (!Plugins.ContainsKey(upperId))
        {
            throw new InvalidOperationException($"Plugin {id} is not loaded");
        }
        return Plugins[upperId];
    }

    public List<string> GetPluginAndDependencyPaths(string pluginId, Func<string, bool>? excludeRule = null)
    {
        return _resolver.GetPluginAndDependencyPaths(pluginId, excludeRule);
    }

    public void UninstallPlugin(string pluginId)
    {
        if (Plugins.TryGetValue(pluginId.ToUpper(), out var plugin))
        {
            plugin.Status = "Waiting Restart";
            _uninstaller.MarkForUninstall(plugin.Path);
        }
    }

    public static void RestartGateway()
    {
        GatewayRestarter.Restart();
    }
}
