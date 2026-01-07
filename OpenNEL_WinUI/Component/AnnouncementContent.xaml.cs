/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Net.Http;
using System.Text.Json;
using OpenNEL_WinUI.type;
using Serilog;

namespace OpenNEL_WinUI
{
    public sealed partial class AnnouncementContent : UserControl
    {
        public AnnouncementContent()
        {
            InitializeComponent();
            this.Loaded += AnnouncementContent_Loaded;
        }

        private async void AnnouncementContent_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var http = new HttpClient();
                var text = await http.GetStringAsync(AppInfo.ApiBaseURL + "/v1/announcement");
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                var content = root.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
                if (!string.IsNullOrWhiteSpace(content))
                {
                    ContentText.Text = content;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取公告失败");
            }
        }
    }
}
