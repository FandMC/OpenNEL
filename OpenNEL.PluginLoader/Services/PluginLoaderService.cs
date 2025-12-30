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

    private static readonly HashSet<string> BuiltInPluginIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "36D701B3-6E98-3E92-AF53-C4EC327B3A71", 
        "716925E5-FEEE-8199-5A7A-855D8E6BD85F",
        "A03D8FB4-2672-2A94-49DB-D5C0A0F447DB" 
    };

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
        // 读取文件到内存，不锁定文件
        byte[] assemblyBytes;
        try
        {
            assemblyBytes = File.ReadAllBytes(filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "无法读取插件文件 {File}", filePath);
            return false;
        }

        // 从内存加载程序集
        Assembly assembly;
        try
        {
            assembly = Assembly.Load(assemblyBytes);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "无法加载插件程序集 {File}", filePath);
            return false;
        }

        bool loaded = false;

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            Log.Error(ex, "无法获取插件类型 {File}: {LoaderExceptions}", filePath, 
                string.Join(", ", ex.LoaderExceptions?.Select(e => e?.Message) ?? Array.Empty<string>()));
            return false;
        }

        var pluginTypes = types
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

            if (BuiltInPluginIds.Contains(pluginId))
            {
                Log.Debug("跳过内置插件 {PluginName} ({PluginId})，已包含在项目中", attribute.Name, pluginId);
                continue;
            }

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
