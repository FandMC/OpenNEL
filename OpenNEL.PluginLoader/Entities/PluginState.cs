using System.Reflection;
using OpenNEL.SDK.Plugin;

namespace OpenNEL.PluginLoader.Entities;

#nullable enable

public class PluginState
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Version { get; }
    public string Author { get; }
    public string[]? Dependencies { get; }
    public string Path { get; }
    public Assembly? Assembly { get; }
    public IPlugin? Plugin { get; }
    public string Status { get; set; } = "Online";
    public bool IsInitialized { get; set; }

    public PluginState(
        string id,
        string name,
        string description,
        string version,
        string author,
        string[]? dependencies,
        string path,
        Assembly? assembly,
        IPlugin? plugin)
    {
        Id = id;
        Name = name;
        Description = description;
        Version = version;
        Author = author;
        Dependencies = dependencies;
        Path = path;
        Assembly = assembly;
        Plugin = plugin;
    }
}
