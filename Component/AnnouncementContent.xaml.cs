using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Net.Http;
using System.Text.Json;
using Serilog;

namespace OpenNEL_WinUI
{
    public sealed partial class AnnouncementContent : UserControl
    {
        public AnnouncementContent()
        {
            this.InitializeComponent();
            this.Loaded += AnnouncementContent_Loaded;
        }

        private async void AnnouncementContent_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var http = new HttpClient();
                var text = await http.GetStringAsync("https://api.fandmc.cn/v1/announcement");
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
