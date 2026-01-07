/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using Microsoft.UI.Xaml.Controls;
using OpenNEL_WinUI.type;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using System;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System.Reflection;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;

namespace OpenNEL_WinUI
{
    public sealed partial class AboutPage : Page
    {
        public static string PageTitle => "关于";
        
        public string AppVersion => AppInfo.AppVersion;
        public string GithubUrl => AppInfo.GithubUrL;
        public string QQGroup => AppInfo.QQGroup;

        public List<Contributor> Contributors { get; } = new List<Contributor>
        {
            new Contributor { Name = "FandMC", Role = "Owner", ColorBrush = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)) },
            new Contributor { Name = "OpenNEL Team", Role = "Developer", ColorBrush = new SolidColorBrush(Color.FromArgb(255, 40, 167, 69)) },
        };

        public AboutPage()
        {
            InitializeComponent();
            try
            {
                var asm = typeof(AboutPage).Assembly;
                using var s = asm.GetManifestResourceStream("OpenNEL_WinUI.Assets.OpenNEL.png");
                if (s != null)
                {
                    var ms = new InMemoryRandomAccessStream();
                    var dw = new DataWriter(ms);
                    var buf = new byte[s.Length];
                    s.Read(buf, 0, buf.Length);
                    dw.WriteBytes(buf);
                    dw.StoreAsync().GetAwaiter().GetResult();
                    ms.Seek(0);
                    var bmp = new BitmapImage();
                    bmp.SetSource(ms);
                    LogoImage.Source = bmp;
                }
            }
            catch { }
        }

        private async void License_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://www.gnu.org/licenses/gpl-3.0-standalone.html"));
        }

        private async void Github_Click(object sender, RoutedEventArgs e)
        {
             await Windows.System.Launcher.LaunchUriAsync(new Uri(AppInfo.GithubUrL));
        }

        private async void QQGroup_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://qm.qq.com/q/JOFAVmoScY"));
        }
    }

    public class Contributor
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public SolidColorBrush ColorBrush { get; set; }
    }
}
