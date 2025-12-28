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
using System;
using System.Diagnostics;
using System.Net;
using Windows.ApplicationModel.DataTransfer;
using System.Collections.ObjectModel;
    using System.IO;
using OpenNEL_WinUI.Utils;
using System.Collections.Generic;

namespace OpenNEL_WinUI
{
    public sealed partial class ToolsPage : Page
    {
        public static string PageTitle => "工具";
        ObservableCollection<string> _logLines = new ObservableCollection<string>();
        readonly Queue<string> _pending = new Queue<string>();
        readonly object _lockObj = new object();
        DispatcherTimer _flushTimer;
        public ToolsPage()
        {
            InitializeComponent();
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                string ipv4 = string.Empty;
                string ipv6 = string.Empty;
                foreach (var a in host.AddressList)
                {
                    if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(a))
                    {
                        ipv4 = a.ToString();
                        break;
                    }
                }
                foreach (var a in host.AddressList)
                {
                    if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && !System.Net.IPAddress.IsLoopback(a))
                    {
                        var s = a.ToString();
                        var lower = s.ToLowerInvariant();
                        if (lower.StartsWith("fe80") || lower.StartsWith("fc") || lower.StartsWith("fd")) continue;
                        if (a.IsIPv6LinkLocal || a.IsIPv6Multicast || a.IsIPv6SiteLocal) continue;
                        ipv6 = s;
                        break;
                    }
                }
                Ipv4Text.Text = ipv4;
                Ipv6Text.Text = string.IsNullOrWhiteSpace(ipv6) ? "无" : ipv6;
                LogList.ItemsSource = _logLines;
                UiLog.Logged += UiLog_Logged;
                this.Unloaded += ToolsPage_Unloaded;
                try
                {
                    var snap = UiLog.GetSnapshot();
                    if (snap != null)
                    {
                        int max = 300;
                        int total = snap.Count;
                        int start = total - max;
                        if (start < 0) start = 0;
                        for (int i = start; i < total; i++)
                        {
                            var line = snap[i];
                            if (!string.IsNullOrEmpty(line)) _logLines.Add(line);
                        }
                        if (_logLines.Count > 0 && LogList != null)
                        {
                            try { LogList.UpdateLayout(); } catch { }
                            try { LogList.ScrollIntoView(_logLines[_logLines.Count - 1]); } catch { }
                        }
                    }
                }
                catch { }
                _flushTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
                _flushTimer.Tick += (s, e) => FlushPending();
                _flushTimer.Start();
            }
            catch { }
        }

        private void OpenSite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "https://fandmc.cn/", UseShellExecute = true });
            }
            catch { }
        }

        private void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var baseDir = System.IO.Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
                var dir = System.IO.Path.Combine(baseDir, "logs");
                Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
            }
            catch { }
        }

        private void CopyIpv4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dp = new DataPackage();
                dp.SetText(Ipv4Text.Text ?? string.Empty);
                Clipboard.SetContent(dp);
                NotificationHost.ShowGlobal("已复制", ToastLevel.Success);
            }
            catch { }
        }

        private void CopyIpv6_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dp = new DataPackage();
                dp.SetText(Ipv6Text.Text ?? string.Empty);
                Clipboard.SetContent(dp);
                NotificationHost.ShowGlobal("已复制", ToastLevel.Success);
            }
            catch { }
        }

        void UiLog_Logged(string line)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                lock (_lockObj)
                {
                    _pending.Enqueue(line);
                    if (_pending.Count > 5000)
                    {
                        while (_pending.Count > 2000) _pending.Dequeue();
                    }
                }
            }
            catch { }
        }

        void ToolsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            try { UiLog.Logged -= UiLog_Logged; } catch { }
            try { _flushTimer?.Stop(); } catch { }
        }

        void FlushPending()
        {
            try
            {
                List<string> batch = null;
                lock (_lockObj)
                {
                    if (_pending.Count == 0) return;
                    batch = new List<string>(_pending.Count);
                    while (_pending.Count > 0) batch.Add(_pending.Dequeue());
                }
                foreach (var line in batch)
                {
                    _logLines.Add(line);
                    if (_logLines.Count > 2000) _logLines.RemoveAt(0);
                }
                if (LogList != null)
                {
                    try { LogList.UpdateLayout(); } catch { }
                    if (_logLines.Count > 0)
                    {
                        try { LogList.ScrollIntoView(_logLines[_logLines.Count - 1]); } catch { }
                    }
                }
            }
            catch { }
        }
    }
}
