using System;

namespace OpenNEL.SDK.Attributes;

#nullable enable
[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string Id { get; }
    public string Name { get; }
    public string Author { get; }
    public string Description { get; }
    public string Version { get; }
    public string[]? Dependencies { get; }

    public PluginAttribute(
        string id,
        string name,
        string description,
        string author,
        string version,
        string[]? dependencies = null)
    {
        Id = id;
        Name = name;
        Author = author;
        Description = description;
        Version = version;
        Dependencies = dependencies;
    }
}
