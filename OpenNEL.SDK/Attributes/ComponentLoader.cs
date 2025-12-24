using System;
using OpenNEL.SDK.Enums;

namespace OpenNEL.SDK.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ComponentLoader(EnumLoaderType type) : Attribute()
{
	public EnumLoaderType Type { get; } = type;
}
