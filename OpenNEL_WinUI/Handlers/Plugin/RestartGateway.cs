/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using Codexus.Development.SDK.Manager;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public class RestartGateway
    {
        public object Execute()
        {
            PluginManager.RestartGateway();
            return new { type = "restart_ack" };
        }
    }
}
