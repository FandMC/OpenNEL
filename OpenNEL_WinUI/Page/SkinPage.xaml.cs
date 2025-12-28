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
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.ComponentModel;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.Handlers.Skin;
using System.Threading.Tasks;
using Serilog;

namespace OpenNEL_WinUI
{
    public sealed partial class SkinPage : Page, INotifyPropertyChanged
    {
        public static string PageTitle => "皮肤";
        public ObservableCollection<SkinItem> Skins { get; } = new ObservableCollection<SkinItem>();
        bool _notLogin;
        public bool NotLogin { get => _notLogin; private set { _notLogin = value; OnPropertyChanged(nameof(NotLogin)); } }
        public SkinPage()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Loaded += SkinPage_Loaded;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshSkinsAsync();
        }

        async void SkinPage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshSkinsAsync();
        }

        async Task RefreshSkinsAsync()
        {
            var last = UserManager.Instance.GetLastAvailableUser();
            if (last == null)
            {
                NotLogin = true;
                Skins.Clear();
                return;
            }
            NotLogin = false;
            try
            {
                var r = await Task.Run(() => new GetFreeSkin().Execute(0, 20));
                Skins.Clear();
                if (r.NotLogin)
                {
                    Log.Error("皮肤刷新失败: 未登录");
                    NotificationHost.ShowGlobal("未登录", ToastLevel.Error);
                    NotLogin = true;
                    return;
                }
                if (!r.Success)
                {
                    Log.Error("皮肤刷新失败: {Message}", r.Message);
                    NotificationHost.ShowGlobal(string.IsNullOrWhiteSpace(r.Message) ? "刷新失败" : r.Message, ToastLevel.Error);
                    return;
                }
                Log.Information("皮肤刷新返回数量={Count}", r.Items.Count);
                foreach (var it in r.Items)
                {
                    Skins.Add(new SkinItem { Name = it.Name, PreviewUrl = it.PreviewUrl, EntityId = it.EntityId });
                }
                if (r.Items.Count == 0) NotificationHost.ShowGlobal("暂无皮肤数据", ToastLevel.Error);
            }
            catch { }
        }

        private void SkinsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var panel = SkinsGrid.ItemsPanelRoot as ItemsWrapGrid;
            if (panel == null) return;
            var width = e.NewSize.Width;
            if (width <= 0) return;
            var itemWidth = Math.Max(240, (width - 24) / 4);
            panel.ItemWidth = itemWidth;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private async void ApplySkinButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                var id = btn?.Tag as string ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id)) return;
                btn.IsEnabled = false;
                var r = await Task.Run(() => new SetSkin().Execute(id));
                if (r.NotLogin)
                {
                    NotificationHost.ShowGlobal("未登录", ToastLevel.Error);
                    return;
                }
                if (r.Success)
                {
                    NotificationHost.ShowGlobal("皮肤已应用", ToastLevel.Success);
                }
                else
                {
                    NotificationHost.ShowGlobal(string.IsNullOrWhiteSpace(r.Message) ? "设置失败" : r.Message, ToastLevel.Error);
                }
            }
            catch { }
            finally
            {
                try { (sender as Button)!.IsEnabled = true; } catch { }
            }
        }
    }
    
    public class SkinItem
    {
        public string Name { get; set; }
        public string PreviewUrl { get; set; }
        public string EntityId { get; set; }
        
    }
}

        
