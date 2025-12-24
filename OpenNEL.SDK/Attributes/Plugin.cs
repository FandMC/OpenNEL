using System;

namespace OpenNEL.SDK.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class Plugin : Attribute
{
	public string Id { get; }

	public string Name { get; }

	public string Author { get; }

	public string Description { get; }

	public string Version { get; }

	public string[]? Dependencies { get; }

	public Plugin(string id, string name, string description, string author, string version, string[]? dependencies = null)
	{
		Id = id;
		Name = name;
		Author = author;
		Description = description;
		Version = version;
		Dependencies = dependencies;
	}
}
