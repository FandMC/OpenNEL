using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenNEL.SDK.Attributes;
using OpenNEL.PluginLoader.Entities;
using OpenNEL.SDK.Plugin;
using Serilog;

namespace OpenNEL.PluginLoader.Services;

#nullable enable

public class PluginLoaderService
{
    public static string[] PluginExtensions { get; set; } = { ".ug", ".dll", ".UG" };

    private readonly HashSet<string> _loadedFiles = new();
    private readonly Dictionary<string, PluginState> _plugins;

    public PluginLoaderService(Dictionary<string, PluginState> plugins)
    {
        _plugins = plugins;
    }

    public int LoadFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            return 0;
        }

        int loadedCount = 0;
        var pluginFiles = Directory.EnumerateFiles(directory)
            .Where(f => PluginExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .ToArray();

        foreach (var filePath in pluginFiles)
        {
            if (_loadedFiles.Contains(filePath)) continue;

            try
            {
                if (TryLoadPlugin(filePath))
                {
                    loadedCount++;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load plugin from {File}", filePath);
            }
        }

        return loadedCount;
    }

    private bool TryLoadPlugin(string filePath)
    {
        var assembly = Assembly.LoadFrom(filePath);
        bool loaded = false;

        var pluginTypes = assembly.GetTypes()
            .Where(type => typeof(IPlugin).IsAssignableFrom(type) 
                           && type != null 
                           && !type.IsAbstract 
                           && !type.IsInterface);

        foreach (var type in pluginTypes)
        {
            var attribute = GetPluginAttribute(type);
            if (attribute == null) continue;

            var pluginId = attribute.Id.ToUpper();
            if (_plugins.ContainsKey(pluginId)) continue;

            var plugin = CreatePluginInstance(type);
            if (plugin == null) continue;

            var state = new PluginState(
                pluginId,
                attribute.Name,
                attribute.Description,
                attribute.Version,
                attribute.Author,
                attribute.Dependencies,
                filePath,
                assembly,
                plugin);

            _plugins.Add(pluginId, state);
            _loadedFiles.Add(filePath);
            loaded = true;
        }

        return loaded;
    }

    private static PluginAttribute? GetPluginAttribute(Type type)
    {
        try
        {
            var attribute = type.GetCustomAttribute<PluginAttribute>(inherit: false);
            if (attribute == null)
            {
                Log.Warning("插件 {TypeFullName} 没有插件属性", type.FullName);
            }
            return attribute;
        }
        catch (MissingMemberException)
        {
            Log.Warning("插件 {TypeFullName} 没有插件属性", type.FullName);
            return null;
        }
    }

    private static IPlugin? CreatePluginInstance(Type type)
    {
        var instance = Activator.CreateInstance(type);
        if (instance is not IPlugin plugin)
        {
            Log.Warning("插件 {TypeFullName} 没有继承 IPlugin", type.FullName);
            return null;
        }
        return plugin;
    }
}
