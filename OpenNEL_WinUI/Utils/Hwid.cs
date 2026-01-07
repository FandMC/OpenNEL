/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OpenNEL_WinUI.type;
using CoreHwid = OpenNEL.Core.Utils.Hwid;

namespace OpenNEL_WinUI.Utils;

internal static class Hwid
{
    public static string Compute() => CoreHwid.Compute();
}
