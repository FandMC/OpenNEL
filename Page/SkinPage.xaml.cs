using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.ComponentModel;
using OpenNEL.Manager;
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
            this.InitializeComponent();
            this.DataContext = this;
            this.Loaded += SkinPage_Loaded;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshSkinsAsync();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
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
                var tProp = r.GetType().GetProperty("type");
                var tVal = tProp != null ? tProp.GetValue(r) as string : null;
                Log.Information("皮肤刷新返回 type={Type}", tVal ?? string.Empty);
                Skins.Clear();
                if (string.Equals(tVal, "skins"))
                {
                    var itemsProp = r.GetType().GetProperty("items");
                    var items = itemsProp?.GetValue(r) as System.Collections.IEnumerable;
                    if (items != null)
                    {
                        int count = 0;
                        foreach (var it in items)
                        {
                            var idProp = it.GetType().GetProperty("entityId");
                            var nameProp = it.GetType().GetProperty("name");
                            var prevProp = it.GetType().GetProperty("previewUrl");
                            var id = idProp?.GetValue(it) as string ?? string.Empty;
                            var name = nameProp?.GetValue(it) as string ?? string.Empty;
                            var prev = prevProp?.GetValue(it) as string ?? string.Empty;
                            Skins.Add(new SkinItem { Name = name, PreviewUrl = prev, EntityId = id });
                            if (count < 5) Log.Information("皮肤项: {Name} {Id} {Preview}", name, id, prev);
                            count++;
                        }
                        Log.Information("皮肤项数量={Count}", count);
                        if (count == 0) NotificationHost.ShowGlobal("暂无皮肤数据", ToastLevel.Error);
                    }
                    else
                    {
                        NotificationHost.ShowGlobal("皮肤接口返回空", ToastLevel.Error);
                    }
                }
                else if (string.Equals(tVal, "skins_error"))
                {
                    var msgProp = r.GetType().GetProperty("message");
                    var msg = msgProp?.GetValue(r) as string ?? string.Empty;
                    Log.Error("皮肤刷新失败: {Message}", msg);
                    NotificationHost.ShowGlobal(string.IsNullOrWhiteSpace(msg) ? "刷新失败" : msg, ToastLevel.Error);
                }
                else if (string.Equals(tVal, "notlogin"))
                {
                    Log.Error("皮肤刷新失败: 未登录");
                    NotificationHost.ShowGlobal("未登录", ToastLevel.Error);
                    NotLogin = true;
                }
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
                var t = r.GetType();
                var type = t.GetProperty("type")?.GetValue(r) as string ?? string.Empty;
                if (string.Equals(type, "set_skin_result"))
                {
                    var succObj = t.GetProperty("success")?.GetValue(r);
                    var succ = succObj is bool b && b;
                    var msg = t.GetProperty("message")?.GetValue(r) as string ?? string.Empty;
                    if (succ)
                    {
                        NotificationHost.ShowGlobal("皮肤已应用", ToastLevel.Success);
                    }
                    else
                    {
                        var m = string.IsNullOrWhiteSpace(msg) ? "设置失败" : msg;
                        NotificationHost.ShowGlobal(m, ToastLevel.Error);
                    }
                }
                else if (string.Equals(type, "notlogin"))
                {
                    NotificationHost.ShowGlobal("未登录", ToastLevel.Error);
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

        
