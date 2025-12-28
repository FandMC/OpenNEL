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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using Serilog;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Xaml.Shapes;

namespace OpenNEL_WinUI
{
    public sealed partial class AdContent : UserControl
    {
        class AdItem
        {
            public string Text { get; set; }
            public string Url { get; set; }
            public string ButtonText { get; set; }
        }
        List<AdItem> _ads = new List<AdItem>();
        int _index;
        DispatcherTimer _timer;
        public AdContent()
        {
            InitializeComponent();
            InitializeAds();
            AdScroll.SizeChanged += AdScroll_SizeChanged;
            if (_ads.Count > 1)
            {
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                _timer.Tick += (s, e) => { Next(); };
                _timer.Start();
            }
        }

    private void OpenOfficialSiteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string url = "https://freecookie.studio/";
            if (sender is Button b && b.Tag is string t && !string.IsNullOrWhiteSpace(t)) url = t;
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开网站失败");
        }
    }

    void InitializeAds()
    {
        _ads.Clear();
        _ads.Add(new AdItem { Text = "最好的客户端: Southside | 官方群1011337297", Url = "https://client.freecookie.studio/", ButtonText = "官方网站" });
        _ads.Add(new AdItem { Text = "Atri Injection 新一代安全热注入 Hyp/布吉岛全模式适用", Url = "https://shop.atri.cards/", ButtonText = "官方网站" });
        AdStack.Children.Clear();
        foreach (var ad in _ads)
        {
            var sp = new StackPanel { Spacing = 8 };
            var tb = new TextBlock { Text = ad.Text, TextWrapping = TextWrapping.Wrap };
            var btn = new Button { Content = ad.ButtonText, HorizontalAlignment = HorizontalAlignment.Left, Tag = ad.Url };
            btn.Click += OpenOfficialSiteButton_Click;
            sp.Children.Add(tb);
            sp.Children.Add(btn);
            AdStack.Children.Add(sp);
        }
        var w = AdScroll.ActualWidth;
        var reserved = 96;
        var contentWidth = w > reserved ? (w - reserved) : w;
        foreach (var c in AdStack.Children)
        {
            if (c is FrameworkElement fe) fe.Width = contentWidth;
        }
        UpdateDots();
    }

    void AdScroll_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var w = AdScroll.ActualWidth;
        var reserved = 96;
        var contentWidth = w > reserved ? (w - reserved) : w;
        foreach (var c in AdStack.Children)
        {
            if (c is FrameworkElement fe) fe.Width = contentWidth;
        }
        UpdateView();
        UpdateDots();
    }

    void UpdateView()
    {
        var w = AdScroll.ActualWidth;
        var x = _index * w;
        AdScroll.ChangeView(x, null, null);
        UpdateDots();
    }

    void UpdateDots()
    {
        DotPanel.Children.Clear();
        for (int i = 0; i < _ads.Count; i++)
        {
            var el = new Ellipse { Width = 8, Height = 8, Margin = new Thickness(3) };
            el.Fill = new SolidColorBrush(i == _index ? Colors.DodgerBlue : Colors.Gray);
            el.Tag = i;
            el.Tapped += Dot_Tapped;
            DotPanel.Children.Add(el);
        }
        PrevButton.Visibility = _ads.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Visibility = _ads.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        DotPanel.Visibility = _ads.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    void Dot_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is int idx)
        {
            _index = idx;
            UpdateView();
        }
    }

    void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        if (_ads.Count == 0) return;
        _index = (_index - 1 + _ads.Count) % _ads.Count;
        UpdateView();
        ResetTimer();
    }

        void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ads.Count == 0) return;
            _index = (_index + 1) % _ads.Count;
            UpdateView();
            ResetTimer();
        }

        void Next()
        {
            if (_ads.Count == 0) return;
            _index = (_index + 1) % _ads.Count;
            UpdateView();
            ResetTimer();
        }

        void ResetTimer()
        {
            if (_timer == null)
            {
                if (_ads.Count > 1)
                {
                    _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                    _timer.Tick += (s, e) => { Next(); };
                    _timer.Start();
                }
                return;
            }
            try
            {
                _timer.Stop();
                _timer.Start();
            }
            catch { }
        }
    }
}
