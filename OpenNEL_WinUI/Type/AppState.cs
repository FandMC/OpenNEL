/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using OpenNEL.Com4399;
using OpenNEL.G79;
using OpenNEL.WPFLauncher;

namespace OpenNEL_WinUI.type;

internal static class AppState
{
    private static Com4399Client? _com4399;
    public static Com4399Client Com4399 => _com4399 ??= new Com4399Client();

    private static G79Client? _g79;
    public static G79Client G79 => _g79 ??= new G79Client();

    private static WPFLauncherClient? _x19;
    public static WPFLauncherClient X19 => _x19 ??= new WPFLauncherClient();
    
    public static Services? Services;
    public static bool Debug;
    public static bool AutoDisconnectOnBan;
    public static bool Pre = AppInfo.AppVersion.Contains("pre", StringComparison.OrdinalIgnoreCase);
}
