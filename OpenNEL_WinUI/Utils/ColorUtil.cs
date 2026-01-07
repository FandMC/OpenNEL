/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using Microsoft.UI.Xaml;
using Windows.UI;
using Microsoft.UI;

namespace OpenNEL_WinUI.Utils
{
    public static class ColorUtil
    {
        public static Color ParseHex(string hex)
        {
            var s = hex?.Trim('#').Trim() ?? string.Empty;
            byte a = 255, r = 0, g = 0, b = 0;
            if (s.Length == 6)
            {
                r = Convert.ToByte(s.Substring(0, 2), 16);
                g = Convert.ToByte(s.Substring(2, 2), 16);
                b = Convert.ToByte(s.Substring(4, 2), 16);
            }
            else if (s.Length == 8)
            {
                a = Convert.ToByte(s.Substring(0, 2), 16);
                r = Convert.ToByte(s.Substring(2, 2), 16);
                g = Convert.ToByte(s.Substring(4, 2), 16);
                b = Convert.ToByte(s.Substring(6, 2), 16);
            }
            return Color.FromArgb(a, r, g, b);
        }

        public static Color ForegroundForTheme(ElementTheme theme)
        {
            return theme == ElementTheme.Dark ? Colors.White : Colors.Black;
        }

        public static Color Transparent => Colors.Transparent;

        public static Color HoverBackgroundForTheme(ElementTheme theme)
        {
            return theme == ElementTheme.Dark ? Color.FromArgb(32, 255, 255, 255) : Color.FromArgb(32, 0, 0, 0);
        }

        public static Color PressedBackgroundForTheme(ElementTheme theme)
        {
            return theme == ElementTheme.Dark ? Color.FromArgb(64, 255, 255, 255) : Color.FromArgb(64, 0, 0, 0);
        }
    }
}
