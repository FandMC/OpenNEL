using System;
using System.Reflection;
using OpenNEL.SDK.Attributes;
using OpenNEL.SDK.Enums;

namespace OpenNEL.SDK.RakNet;

public static class RakNetLoader
{
	private static Type? _loader;

	public static void FindLoader()
	{
		Type[] types = Assembly.LoadFrom("Codexus.RakNet.ug").GetTypes();
		Type[] array = types;
		foreach (Type type in array)
		{
			ComponentLoader customAttribute = type.GetCustomAttribute<ComponentLoader>(inherit: false);
			if (customAttribute != null && customAttribute.Type == EnumLoaderType.RakNet && typeof(IRakNetCreate).IsAssignableFrom(type))
			{
				_loader = type;
				break;
			}
		}
		if (_loader == null)
		{
			throw new Exception("Could not initialize RakNet");
		}
	}

	public static IRakNetCreate ConstructLoader()
	{
		if (_loader == null)
		{
			throw new Exception("You must call FindLoader() before ConstructLoader()");
		}
		return (IRakNetCreate)Activator.CreateInstance(_loader);
	}
}
