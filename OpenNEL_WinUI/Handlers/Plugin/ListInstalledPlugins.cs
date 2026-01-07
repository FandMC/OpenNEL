/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.Collections.Generic;
using Codexus.Development.SDK.Manager;

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
                    NeedUpdate = false
                });
            }
            return list;
        }
    }
}
