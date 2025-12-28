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
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using OpenNEL_WinUI.Manager;
using System.Reflection;

namespace OpenNEL_WinUI
{
    public sealed partial class AddRoleContent : UserControl
    {
        private readonly Random _random = new Random();

        public AddRoleContent()
        {
            InitializeComponent();
            try
            {
                var mode = SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
                ElementTheme t = ElementTheme.Default;
                if (mode == "light") t = ElementTheme.Light;
                else if (mode == "dark") t = ElementTheme.Dark;
                this.RequestedTheme = t;
            }
            catch { }
        }

        public string RoleName => RoleNameInput.Text;

        private void RandomBtn_Click(object sender, RoutedEventArgs e)
        {
            var asm = typeof(AddRoleContent).Assembly;
            var names = asm.GetManifestResourceNames();
            var r1 = names.FirstOrDefault(x => x.EndsWith(".Assets.prefix.txt", StringComparison.OrdinalIgnoreCase));
            var r2 = names.FirstOrDefault(x => x.EndsWith(".Assets.character.txt", StringComparison.OrdinalIgnoreCase));
            var r3 = names.FirstOrDefault(x => x.EndsWith(".Assets.verb.txt", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(r1) || string.IsNullOrEmpty(r2) || string.IsNullOrEmpty(r3)) return;

            string[] ReadLines(string rn)
            {
                using var s = asm.GetManifestResourceStream(rn);
                if (s == null) return Array.Empty<string>();
                using var sr = new StreamReader(s);
                var all = sr.ReadToEnd();
                var arr = all.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return arr.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            }

            var l1 = ReadLines(r1);
            var l2 = ReadLines(r2);
            var l3 = ReadLines(r3);
            if (l1.Length == 0 || l2.Length == 0 || l3.Length == 0) return;
            var s1 = l1[_random.Next(l1.Length)].Trim();
            var s2 = l2[_random.Next(l2.Length)].Trim();
            var s3 = l3[_random.Next(l3.Length)].Trim();
            RoleNameInput.Text = s1 + s2 + s3;
        }
    }
}
