using OpenNEL.SDK.Attributes;
using OpenNEL.SDK.Plugin;

namespace Codexus.Base108X.Plugin;

[Plugin("A03D8FB4-2672-2A94-49DB-D5C0A0F447DB", "OpenNEL.Base108X", "Support for plugin development in Minecraft 1.8.X", "DevCodexus", "1.1.1", null)]
public class Base108X : IPlugin
{
	public const string PluginChannel = "OpenNEL.Base108X";

	public void OnInitialize()
	{
	}
}
