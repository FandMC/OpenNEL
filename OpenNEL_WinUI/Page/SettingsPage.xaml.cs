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
using Microsoft.UI.Xaml;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.type;
using System;

namespace OpenNEL_WinUI
{
    public sealed partial class SettingsPage : Page
    {
        public static string PageTitle => "设置";
        bool _initing;

        public SettingsPage()
        {
            _initing = true;
            InitializeComponent();
            var s = SettingManager.Instance.Get();
            var mode = (s?.ThemeMode ?? string.Empty).Trim().ToLowerInvariant();
            if (mode == "light") ThemeRadios.SelectedIndex = 1;
            else if (mode == "dark") ThemeRadios.SelectedIndex = 2;
            else ThemeRadios.SelectedIndex = 0;

            var bd = (s?.Backdrop ?? string.Empty).Trim().ToLowerInvariant();
            if (bd == "acrylic") BackdropRadios.SelectedIndex = 1;
            else BackdropRadios.SelectedIndex = 0;
            AutoCopyIpSwitch.IsOn = s?.AutoCopyIpOnStart ?? false;
            AutoDisconnectOnBanSwitch.IsOn = s?.AutoDisconnectOnBan ?? false;
            DebugSwitch.IsOn = s?.Debug ?? false;
            Socks5EnableSwitch.IsOn = s?.Socks5Enabled ?? false;
            Socks5HostBox.Text = s?.Socks5Address ?? string.Empty;
            Socks5PortBox.Value = s?.Socks5Port ?? 1080;
            Socks5UsernameBox.Text = s?.Socks5Username ?? string.Empty;
            Socks5PasswordBox.Password = s?.Socks5Password ?? string.Empty;
            Socks5HostBox.IsEnabled = Socks5EnableSwitch.IsOn;
            Socks5PortBox.IsEnabled = Socks5EnableSwitch.IsOn;
            Socks5UsernameBox.IsEnabled = Socks5EnableSwitch.IsOn;
            Socks5PasswordBox.IsEnabled = Socks5EnableSwitch.IsOn;
            _initing = false;
        }

        private void ThemeRadios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initing) return;
            var sel = ThemeRadios.SelectedIndex;
            var data = SettingManager.Instance.Get();
            if (sel == 1) data.ThemeMode = "light";
            else if (sel == 2) data.ThemeMode = "dark";
            else data.ThemeMode = "system";
            SettingManager.Instance.Update(data);
            MainWindow.ApplyThemeFromSettingsStatic();
        }

        private void BackdropRadios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initing) return;
            var sel = BackdropRadios.SelectedIndex;
            var data = SettingManager.Instance.Get();
            if (sel == 1) data.Backdrop = "acrylic";
            else data.Backdrop = "mica";
            SettingManager.Instance.Update(data);
            MainWindow.ApplyThemeFromSettingsStatic();
        }

        private void AutoCopyIpSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_initing) return;
            var data = SettingManager.Instance.Get();
            data.AutoCopyIpOnStart = AutoCopyIpSwitch.IsOn;
            SettingManager.Instance.Update(data);
        }

        private void AutoDisconnectOnBanSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_initing) return;
            var data = SettingManager.Instance.Get();
            data.AutoDisconnectOnBan = AutoDisconnectOnBanSwitch.IsOn;
            SettingManager.Instance.Update(data);
            AppState.AutoDisconnectOnBan = AutoDisconnectOnBanSwitch.IsOn;
        }

        private void DebugSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_initing) return;
            var data = SettingManager.Instance.Get();
            data.Debug = DebugSwitch.IsOn;
            SettingManager.Instance.Update(data);
            AppState.Debug = DebugSwitch.IsOn;
        }

        private void Socks5HostBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_initing) return;
            var data = SettingManager.Instance.Get();
            data.Socks5Address = Socks5HostBox.Text ?? string.Empty;
            SettingManager.Instance.Update(data);
        }

        private void Socks5PortBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (_initing) return;
            var v = (int)Math.Max(0, Math.Min(65535, sender.Value));
            var data = SettingManager.Instance.Get();
            data.Socks5Port = v;
            SettingManager.Instance.Update(data);
        }

        private void Socks5UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_initing) return;
            var data = SettingManager.Instance.Get();
            data.Socks5Username = Socks5UsernameBox.Text ?? string.Empty;
            SettingManager.Instance.Update(data);
        }

        private void Socks5PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_initing) return;
            var data = SettingManager.Instance.Get();
            data.Socks5Password = Socks5PasswordBox.Password ?? string.Empty;
            SettingManager.Instance.Update(data);
        }

        private void Socks5EnableSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_initing) return;
            var data = SettingManager.Instance.Get();
            data.Socks5Enabled = Socks5EnableSwitch.IsOn;
            SettingManager.Instance.Update(data);
            Socks5HostBox.IsEnabled = Socks5EnableSwitch.IsOn;
            Socks5PortBox.IsEnabled = Socks5EnableSwitch.IsOn;
            Socks5UsernameBox.IsEnabled = Socks5EnableSwitch.IsOn;
            Socks5PasswordBox.IsEnabled = Socks5EnableSwitch.IsOn;
        }
    }
}
