using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenNEL.PluginLoader.Entities;
using OpenNEL.PluginLoader.Utils;
using Serilog;

namespace OpenNEL.PluginLoader.Services;

#nullable enable

public class DependencyResolver
{
    private readonly Dictionary<string, PluginState> _plugins;

    public DependencyResolver(Dictionary<string, PluginState> plugins)
    {
        _plugins = plugins;
    }

    public void CheckDependencies()
    {
        foreach (var plugin in _plugins.Values)
        {
            if (plugin.Dependencies == null) continue;

            foreach (var depId in plugin.Dependencies)
            {
                if (!_plugins.ContainsKey(depId.ToUpper()))
                {
                    throw new InvalidOperationException(
                        $"Plugin {plugin.Name}({plugin.Id}) depends on {depId}, but it is not loaded");
                }
            }
        }
    }

    public void ResolveVersionConflicts()
    {
        var resolved = new Dictionary<string, PluginState>();

        foreach (var group in _plugins.Values.GroupBy(p => p.Id.ToUpper()))
        {
            var newest = group.OrderByDescending(p => VersionParser.Parse(p.Version)).First();
            resolved[newest.Id] = newest;

            if (group.Count() > 1)
            {
                Log.Information(
                    "Multiple versions of plugin {PluginId} found. Using version {Version} from {Path}",
                    newest.Id, newest.Version, newest.Path);
            }
        }

        _plugins.Clear();
        foreach (var kvp in resolved)
        {
            _plugins.Add(kvp.Key, kvp.Value);
        }
    }

    public List<string> GetInitializationOrder()
    {
        var adjacency = new Dictionary<string, List<string>>();
        var inDegree = new Dictionary<string, int>();

        foreach (var plugin in _plugins.Values)
        {
            adjacency[plugin.Id] = new List<string>();
            inDegree[plugin.Id] = 0;
        }

        foreach (var plugin in _plugins.Values)
        {
            if (plugin.Dependencies == null) continue;

            foreach (var depId in plugin.Dependencies)
            {
                var upperDepId = depId.ToUpper();
                if (_plugins.ContainsKey(upperDepId))
                {
                    adjacency[upperDepId].Add(plugin.Id);
                    inDegree[plugin.Id]++;
                }
            }
        }

        var queue = new Queue<string>();
        foreach (var plugin in _plugins.Values.Where(p => inDegree[p.Id] == 0))
        {
            queue.Enqueue(plugin.Id);
        }

        var order = new List<string>();
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            order.Add(current);

            foreach (var dependent in adjacency[current])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        if (order.Count != _plugins.Count)
        {
            var circular = _plugins.Keys.Except(order).ToList();
            var circularList = string.Join(", ", circular);
            Log.Error("Circular dependency detected among plugins: {CircularDependencies}", circularList);
            throw new InvalidOperationException("Circular dependency detected among plugins: " + circularList);
        }

        return order;
    }

    public void InitializePlugins(List<string> order, Action<Assembly>? onAssemblyLoaded)
    {
        foreach (var pluginId in order)
        {
            var plugin = _plugins[pluginId];
            if (plugin.IsInitialized) continue;

            Log.Information("Initializing plugin: {PluginName}", plugin.Name);
            
            if (plugin.Assembly != null)
            {
                onAssemblyLoaded?.Invoke(plugin.Assembly);
            }

            plugin.Plugin?.OnInitialize();
            plugin.IsInitialized = true;
        }
    }

    public List<string> GetPluginAndDependencyPaths(string pluginId, Func<string, bool>? excludeRule = null)
    {
        pluginId = pluginId.ToUpper();
        if (!_plugins.ContainsKey(pluginId))
        {
            throw new InvalidOperationException($"Plugin {pluginId} is not loaded");
        }

        var paths = new HashSet<string>();
        var visited = new HashSet<string>();
        CollectDependencyPaths(pluginId, paths, visited, excludeRule);
        return paths.ToList();
    }

    private void CollectDependencyPaths(
        string pluginId,
        HashSet<string> paths,
        HashSet<string> visited,
        Func<string, bool>? excludeRule)
    {
        if (!visited.Add(pluginId)) return;
        if (excludeRule != null && excludeRule(pluginId)) return;

        if (!_plugins.TryGetValue(pluginId, out var plugin))
        {
            Log.Warning("Plugin {PluginId} is not loaded", pluginId);
            return;
        }

        paths.Add(plugin.Path);

        if (plugin.Dependencies == null) return;

        foreach (var depId in plugin.Dependencies)
        {
            CollectDependencyPaths(depId.ToUpper(), paths, visited, excludeRule);
        }
    }
}
