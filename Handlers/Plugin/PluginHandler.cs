using System.Collections.Generic;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public static class PluginHandler
    {
        public static List<PluginViewModel> GetInstalledPlugins()
        {
            return new ListInstalledPlugins().Execute();
        }

        public static void UninstallPlugin(string pluginId)
        {
            new UninstallPlugin().Execute(pluginId);
        }

        public static void RestartGateway()
        {
            new RestartGateway().Execute();
        }

        public static object InstallPluginByInfo(string infoJson)
        {
            return new InstallPlugin().Execute(infoJson).GetAwaiter().GetResult();
        }

        public static object UpdatePluginByInfo(string pluginId, string oldVersion, string infoJson)
        {
            return new UpdatePlugin().Execute(pluginId, oldVersion, infoJson).GetAwaiter().GetResult();
        }

        public static object ListAvailablePlugins(string url = null)
        {
            return new ListAvailablePlugins().Execute(url).GetAwaiter().GetResult();
        }
    }
}
