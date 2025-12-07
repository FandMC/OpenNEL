using System.Collections.Generic;
using Codexus.Development.SDK.Manager;
using OpenNEL.type;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public class ListInstalledPlugins
    {
        public List<PluginViewModel> Execute()
        {
            var list = new List<PluginViewModel>();
            foreach (var plugin in PluginManager.Instance.Plugins.Values)
            {
                list.Add(new PluginViewModel
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    Description = plugin.Description,
                    Version = plugin.Version,
                    Author = plugin.Author,
                    Status = plugin.Status,
                    IsWaitingRestart = AppState.WaitRestartPlugins.ContainsKey(plugin.Id),
                    NeedUpdate = false
                });
            }
            return list;
        }
    }
}
