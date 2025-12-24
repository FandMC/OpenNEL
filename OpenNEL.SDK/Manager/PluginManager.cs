using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using OpenNEL.SDK.Attributes;
using OpenNEL.SDK.Plugin;
using Serilog;

namespace OpenNEL.SDK.Manager;

public class PluginManager
{
	public class PluginState(string id, string name, string description, string version, string author, string[]? dependencies, string path, Assembly? assembly, IPlugin? plugin)
	{
		public string Id { get; } = id;

		public string Name { get; } = name;

		public string Description { get; } = description;

		public string Version { get; } = version;

		public string Author { get; } = author;

		public string[]? Dependencies { get; } = dependencies;

		public string Path { get; } = path;

		public Assembly? Assembly { get; } = assembly;

		public IPlugin? Plugin { get; } = plugin;

		public string Status { get; set; } = "Online";

		public bool IsInitialized { get; set; }
	}

	private const string UninstallPluginFile = ".ug_cache";

	private static PluginManager? _instance;

	private readonly HashSet<string> _loadedFiles = new HashSet<string>();

	private readonly Lock _writeFileLock = new Lock();

	public readonly Dictionary<string, PluginState> Plugins = new Dictionary<string, PluginState>();

	public static string[] PluginExtensions { get; set; } = new string[3] { ".ug", ".dll", ".UG" };

	public static PluginManager Instance => _instance ?? (_instance = new PluginManager());

	public void EnsureUninstall()
	{
		using (_writeFileLock.EnterScope())
		{
			if (!File.Exists(".ug_cache"))
			{
				File.WriteAllText(".ug_cache", JsonSerializer.Serialize(new HashSet<string>()));
				return;
			}
			HashSet<string> hashSet = JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(".ug_cache"));
			if (hashSet == null)
			{
				return;
			}
			foreach (string item in hashSet)
			{
				File.Delete(item);
			}
			File.Delete(".ug_cache");
			File.WriteAllText(".ug_cache", JsonSerializer.Serialize(new HashSet<string>()));
		}
	}

	public void LoadPlugins(string directory)
	{
		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
			return;
		}
		string[] array = (from f in Directory.EnumerateFiles(directory)
			where PluginExtensions.Contains<string>(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase)
			select f).ToArray();
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (_loadedFiles.Contains(text))
			{
				continue;
			}
			try
			{
				Assembly assembly = Assembly.LoadFrom(text);
				foreach (Type item in from type in assembly.GetTypes()
					where typeof(IPlugin).IsAssignableFrom(type) && (object)type != null && !type.IsAbstract && !type.IsInterface
					select type)
				{
					OpenNEL.SDK.Attributes.Plugin customAttribute;
					try
					{
						customAttribute = item.GetCustomAttribute<OpenNEL.SDK.Attributes.Plugin>(inherit: false);
					}
					catch (MissingMemberException)
					{
						Log.Warning("插件 {TypeFullName} 没有插件属性", new object[1] { item.FullName });
						continue;
					}
					if (customAttribute == null)
					{
						Log.Warning("插件 {TypeFullName} 没有插件属性", new object[1] { item.FullName });
					}
					else if (!Plugins.ContainsKey(customAttribute.Id))
					{
						if (!(Activator.CreateInstance(item) is IPlugin plugin))
						{
							Log.Warning("插件 {TypeFullName} 没有继承 IPlugin", new object[1] { item.FullName });
						}
						else
						{
							Plugins.Add(customAttribute.Id.ToUpper(), new PluginState(customAttribute.Id.ToUpper(), customAttribute.Name, customAttribute.Description, customAttribute.Version, customAttribute.Author, customAttribute.Dependencies, text, assembly, plugin));
							_loadedFiles.Add(text);
						}
					}
				}
			}
			catch (Exception ex2)
			{
				Log.Error(ex2, "Failed to load plugin from {File}", new object[1] { text });
			}
		}
		CheckDependencies();
		Log.Information("识别到{Count} 个插件", new object[1] { Plugins.Count });
		InitializePlugins();
	}

	public bool HasPlugin(string id)
	{
		return Plugins.ContainsKey(id.ToUpper());
	}

	public PluginState GetPlugin(string id)
	{
		if (!Plugins.ContainsKey(id.ToUpper()))
		{
			throw new InvalidOperationException("Plugin " + id + " is not loaded");
		}
		return Plugins[id.ToUpper()];
	}

	public List<string> GetPluginAndDependencyPaths(string pluginId, Func<string, bool>? excludeRule = null)
	{
		pluginId = pluginId.ToUpper();
		if (!HasPlugin(pluginId))
		{
			throw new InvalidOperationException("Plugin " + pluginId + " is not loaded");
		}
		HashSet<string> hashSet = new HashSet<string>();
		HashSet<string> visitedPlugins = new HashSet<string>();
		CollectDependencyPaths(pluginId, hashSet, visitedPlugins, excludeRule);
		return hashSet.ToList();
	}

	private void CollectDependencyPaths(string pluginId, HashSet<string> pathSet, HashSet<string> visitedPlugins, Func<string, bool>? excludeRule = null)
	{
		if (!visitedPlugins.Add(pluginId) || (excludeRule != null && excludeRule(pluginId)))
		{
			return;
		}
		if (!HasPlugin(pluginId))
		{
			Log.Warning("Plugin {PluginId} is not loaded", new object[1] { pluginId });
			return;
		}
		PluginState plugin = GetPlugin(pluginId);
		pathSet.Add(plugin.Path);
		if (plugin.Dependencies != null)
		{
			string[] dependencies = plugin.Dependencies;
			string[] array = dependencies;
			foreach (string text in array)
			{
				CollectDependencyPaths(text.ToUpper(), pathSet, visitedPlugins, excludeRule);
			}
		}
	}

	private void CheckDependencies()
	{
		foreach (PluginState value in Plugins.Values)
		{
			if (value.Dependencies == null)
			{
				continue;
			}
			string[] dependencies = value.Dependencies;
			string[] array = dependencies;
			foreach (string text in array)
			{
				if (!Plugins.ContainsKey(text))
				{
					throw new InvalidOperationException($"Plugin {value.Name}({value.Id}) depends on {text}, but it is not loaded");
				}
			}
		}
	}

	private static Version ParseVersion(string version)
	{
		try
		{
			string[] array = version.Split('.');
			int result;
			int major = ((array.Length != 0 && int.TryParse(array[0], out result)) ? result : 0);
			int result2;
			int minor = ((array.Length > 1 && int.TryParse(array[1], out result2)) ? result2 : 0);
			int result3;
			int build = ((array.Length > 2 && int.TryParse(array[2], out result3)) ? result3 : 0);
			return new Version(major, minor, build);
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to parse plugin version: {Version}. Using default 0.0.0", new object[1] { version });
			return new Version(0, 0, 0);
		}
	}

	private void InitializePlugins()
	{
		Dictionary<string, PluginState> dictionary = new Dictionary<string, PluginState>();
		foreach (IGrouping<string, PluginState> item in from p in Plugins.Values
			group p by p.Id.ToUpper())
		{
			PluginState pluginState = item.OrderByDescending((PluginState p) => ParseVersion(p.Version)).First();
			dictionary[pluginState.Id] = pluginState;
			if (item.Count() > 1)
			{
				Log.Information("Multiple versions of plugin {PluginId} found. Using version {Version} from {Path}", new object[3] { pluginState.Id, pluginState.Version, pluginState.Path });
			}
		}
		Plugins.Clear();
		foreach (KeyValuePair<string, PluginState> item2 in dictionary)
		{
			Plugins.Add(item2.Key, item2.Value);
		}
		Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>();
		Dictionary<string, int> inDegree = new Dictionary<string, int>();
		foreach (PluginState value in Plugins.Values)
		{
			dictionary2[value.Id] = new List<string>();
			inDegree[value.Id] = 0;
		}
		foreach (PluginState value2 in Plugins.Values)
		{
			if (value2.Dependencies == null)
			{
				continue;
			}
			string[] dependencies = value2.Dependencies;
			string[] array = dependencies;
			foreach (string key in array)
			{
				if (Plugins.ContainsKey(key))
				{
					dictionary2[key].Add(value2.Id);
					inDegree[value2.Id]++;
				}
			}
		}
		Queue<string> queue = new Queue<string>();
		foreach (PluginState item3 in Plugins.Values.Where((PluginState plugin) => inDegree[plugin.Id] == 0))
		{
			queue.Enqueue(item3.Id);
		}
		List<string> list = new List<string>();
		while (queue.Count > 0)
		{
			string text = queue.Dequeue();
			list.Add(text);
			foreach (string item4 in dictionary2[text])
			{
				inDegree[item4]--;
				if (inDegree[item4] == 0)
				{
					queue.Enqueue(item4);
				}
			}
		}
		if (list.Count != Plugins.Count)
		{
			List<string> values = Plugins.Keys.Except(list).ToList();
			string text2 = string.Join(", ", values);
			Log.Error("Circular dependency detected among plugins: {CircularDependencies}", new object[1] { text2 });
			throw new InvalidOperationException("Circular dependency detected among plugins: " + text2);
		}
		foreach (PluginState item5 in list.Select((string pluginId) => Plugins[pluginId]))
		{
			if (!item5.IsInitialized)
			{
				Log.Information(item5.Name, Array.Empty<object>());
				PacketManager.Instance.RegisterPacketFromAssembly(item5.Assembly);
				item5.Plugin.OnInitialize();
				item5.IsInitialized = true;
			}
		}
	}

	public void UninstallPlugin(string pluginId)
	{
		if (Plugins.TryGetValue(pluginId, out var value))
		{
			value.Status = "Waiting Restart";
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = value.Path;
			UninstallPluginWithPaths(list);
		}
	}

	public void UninstallPluginWithPaths(List<string> paths)
	{
		using (_writeFileLock.EnterScope())
		{
			HashSet<string> hashSet = JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(".ug_cache"));
			if (hashSet == null)
			{
				Log.Error("Failed to read uninstall file", Array.Empty<object>());
				return;
			}
			foreach (string path in paths)
			{
				hashSet.Add(path);
			}
			File.WriteAllText(".ug_cache", JsonSerializer.Serialize(hashSet));
		}
	}

	public static void RestartGateway()
	{
		try
		{
			string text = Environment.ProcessPath;
			if (string.IsNullOrEmpty(text))
			{
				using Process process = Process.GetCurrentProcess();
				text = process.MainModule?.FileName;
			}
			if (string.IsNullOrEmpty(text))
			{
				Log.Error("Failed to determine executable path.", Array.Empty<object>());
				return;
			}
			string text2 = string.Join(" ", Environment.GetCommandLineArgs().Skip(1));
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = text,
				Arguments = text2,
				UseShellExecute = true
			};
			Log.Information("Preparing to restart gateway, Path: {ExecutablePath}, Arguments: {Arguments}", new object[2] { text, text2 });
			Process.Start(startInfo);
			Log.Information("New process started.", Array.Empty<object>());
			Environment.Exit(0);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to restart gateway.", Array.Empty<object>());
		}
	}
}
