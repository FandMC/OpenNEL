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
using System.Linq;
using System.Threading.Tasks;
using OpenNEL_WinUI.Handlers.Game;
using System.ComponentModel;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.Entities.Web.NetGame;
using Windows.ApplicationModel.DataTransfer;
using OpenNEL.SDK.Entities;

namespace OpenNEL_WinUI
{
    public sealed partial class NetworkServerPage : Page, INotifyPropertyChanged
    {
        public static string PageTitle => "网络服务器";
        public ObservableCollection<ServerItem> Servers { get; } = new ObservableCollection<ServerItem>();
        private bool _notLogin;
        public bool NotLogin { get => _notLogin; private set { _notLogin = value; OnPropertyChanged(nameof(NotLogin)); } }
        private System.Threading.CancellationTokenSource _cts;
        private int _page = 1;
        private const int PageSize = 20;
        private bool _hasMore;
        private int _refreshId;

        public NetworkServerPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.Loaded += NetworkServerPage_Loaded;
        }

        private async void NetworkServerPage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshServers(string.Empty);
        }

        private async Task RefreshServers(string keyword)
        {
            var cts = _cts;
            cts?.Cancel();
            _cts = new System.Threading.CancellationTokenSource();
            var token = _cts.Token;
            object r;
            var my = System.Threading.Interlocked.Increment(ref _refreshId);
            try
            {
                r = await RunOnStaAsync(() =>
                {
                    if (token.IsCancellationRequested) return new { type = "servers", items = System.Array.Empty<object>(), hasMore = false };
                    var offset = Math.Max(0, (_page - 1) * PageSize);
                    if (string.IsNullOrWhiteSpace(keyword))
                    {
                        return new ListServers().Execute(offset, PageSize);
                    }
                    return new SearchServers().Execute(keyword, offset, PageSize);
                });
            }
            catch (System.Exception)
            {
                NotLogin = false;
                Servers.Clear();
                UpdatePageView();
                return;
            }
            if (my != _refreshId) return;
            var tProp = r.GetType().GetProperty("type");
            var tVal = tProp != null ? tProp.GetValue(r) as string : null;
            if (string.Equals(tVal, "notlogin"))
            {
                NotLogin = true;
                Servers.Clear();
                _page = 1;
                _hasMore = false;
                UpdatePageView();
                return;
            }
            NotLogin = false;
            Servers.Clear();
            var itemsProp = r.GetType().GetProperty("items");
            var items = itemsProp?.GetValue(r) as System.Collections.IEnumerable;
            var hmProp = r.GetType().GetProperty("hasMore");
            _hasMore = hmProp != null && (bool)(hmProp.GetValue(r) ?? false);
            if (items != null)
            {
                var list = new System.Collections.Generic.List<ServerItem>();
                foreach (var item in items)
                {
                    if (my != _refreshId || token.IsCancellationRequested) break;
                    var idProp = item.GetType().GetProperty("entityId");
                    var nameProp = item.GetType().GetProperty("name");
                    var id = idProp?.GetValue(item) as string ?? string.Empty;
                    var name = nameProp?.GetValue(item) as string ?? string.Empty;
                    var si = new ServerItem { EntityId = id, Name = name, ImageUrl = string.Empty };
                    list.Add(si);
                }
                var limiter = new System.Threading.SemaphoreSlim(6);
                foreach (var si in list)
                {
                    if (my != _refreshId || token.IsCancellationRequested) break;
                    Servers.Add(si);
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        await limiter.WaitAsync();
                        try
                        {
                            if (my != _refreshId || token.IsCancellationRequested) return;
                            object d = await RunOnStaAsync(() => new GetServersDetail().Execute(si.EntityId));
                            if (my != _refreshId || token.IsCancellationRequested) return;
                            var tp = d.GetType().GetProperty("type");
                            var tv = tp != null ? tp.GetValue(d) as string : null;
                            if (string.Equals(tv, "server_detail"))
                            {
                                var ip = d.GetType().GetProperty("images");
                                var il = ip != null ? ip.GetValue(d) as System.Collections.IEnumerable : null;
                                if (il != null)
                                {
                                    foreach (var it in il)
                                    {
                                        if (my != _refreshId || token.IsCancellationRequested) return;
                                        var s = it != null ? it.ToString() : string.Empty;
                                        s = (s ?? string.Empty).Replace("`", string.Empty).Trim();
                                        if (!string.IsNullOrWhiteSpace(s))
                                        {
                                            var url = s;
                                            DispatcherQueue.TryEnqueue(() =>
                                            {
                                                if (my != _refreshId || token.IsCancellationRequested) return;
                                                si.ImageUrl = url;
                                            });
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                        finally { try { limiter.Release(); } catch { } }
                    });
                }
            }
            UpdatePageView();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (sender as TextBox)?.Text ?? string.Empty;
            _page = 1;
            Servers.Clear();
            UpdatePageView();
            _ = RefreshServers(q);
        }

        private async void JoinServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ServerItem s)
            {
                try
                {
                    object r = await RunOnStaAsync(() => new OpenServer().Execute(s.EntityId));
                    var tProp = r.GetType().GetProperty("type");
                    var tVal = tProp != null ? tProp.GetValue(r) as string : null;
                    if (!string.Equals(tVal, "server_roles")) return;

                    var accounts = UserManager.Instance.GetUsersNoDetails();
                    var acctItems = accounts
                        .Where(a => a.Authorized)
                        .Select(a => {
                            var label = string.IsNullOrWhiteSpace(a.Alias) ? a.UserId : a.Alias;
                            return new JoinServerContent.OptionItem { Label = label + " (" + a.Channel + ")", Value = a.UserId };
                        })
                        .ToList();

                    var itemsProp = r.GetType().GetProperty("items");
                    var items = itemsProp?.GetValue(r) as System.Collections.IEnumerable;
                    var roleItems = new System.Collections.Generic.List<JoinServerContent.OptionItem>();
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            var idProp = item.GetType().GetProperty("id");
                            var nameProp = item.GetType().GetProperty("name");
                            var id = idProp?.GetValue(item) as string ?? string.Empty;
                            var name = nameProp?.GetValue(item) as string ?? string.Empty;
                            roleItems.Add(new JoinServerContent.OptionItem { Label = name, Value = id });
                        }
                    }

                    while (true)
                    {
                        var joinContent = new JoinServerContent();
                        joinContent.SetAccounts(acctItems);
                        joinContent.SetRoles(roleItems);
                        joinContent.AccountChanged += async (accountId) =>
                        {
                            try
                            {
                                await RunOnStaAsync(() => new SelectAccount().Execute(accountId));
                                object rAcc = await RunOnStaAsync(() => new OpenServer().ExecuteForAccount(accountId, s.EntityId));
                                var tP = rAcc.GetType().GetProperty("type");
                                var tV = tP != null ? tP.GetValue(rAcc) as string : null;
                                if (string.Equals(tV, "server_roles"))
                                {
                                    var ip2 = rAcc.GetType().GetProperty("items");
                                    var il2 = ip2?.GetValue(rAcc) as System.Collections.IEnumerable;
                                    var ri2 = new System.Collections.Generic.List<JoinServerContent.OptionItem>();
                                    if (il2 != null)
                                    {
                                        foreach (var it2 in il2)
                                        {
                                            var idP2 = it2.GetType().GetProperty("id");
                                            var nameP2 = it2.GetType().GetProperty("name");
                                            var id2 = idP2?.GetValue(it2) as string ?? string.Empty;
                                            var name2 = nameP2?.GetValue(it2) as string ?? string.Empty;
                                            ri2.Add(new JoinServerContent.OptionItem { Label = name2, Value = id2 });
                                        }
                                    }
                                    roleItems = ri2;
                                    joinContent.SetRoles(roleItems);
                                }
                            }
                            catch { }
                        };
                        var dlg = new ContentDialog
                        {
                            XamlRoot = this.XamlRoot,
                            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                            Title = "加入服务器",
                            Content = joinContent,
                            PrimaryButtonText = "启动",
                            SecondaryButtonText = "添加角色",
                            CloseButtonText = "关闭",
                            DefaultButton = ContentDialogButton.Primary
                        };
                        try
                        {
                            var mode = SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
                            ElementTheme t = ElementTheme.Default;
                            if (mode == "light") t = ElementTheme.Light; else if (mode == "dark") t = ElementTheme.Dark;
                            dlg.RequestedTheme = t;
                        }
                        catch { }

                        var result = await dlg.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            var accId = joinContent.SelectedAccountId;
                            var roleId = joinContent.SelectedRoleId;
                            if (string.IsNullOrWhiteSpace(accId) || string.IsNullOrWhiteSpace(roleId))
                            {
                                continue;
                            }
                            NotificationHost.ShowGlobal("正在准备游戏资源，请稍后", ToastLevel.Success);
                            var rSel = await RunOnStaAsync(() => new SelectAccount().Execute(accId));
                            var req = new EntityJoinGame { ServerId = s.EntityId, ServerName = s.Name, Role = roleId, GameId = s.EntityId };
                            var set = SettingManager.Instance.Get();
                            var socks = new EntitySocks5();
                            var enabled = set?.Socks5Enabled ?? false;
                            if (!enabled || string.IsNullOrWhiteSpace(set?.Socks5Address))
                            {
                                socks.Address = string.Empty;
                                socks.Port = 0;
                                socks.Username = string.Empty;
                                socks.Password = string.Empty;
                            }
                            else
                            {
                                socks.Address = set!.Socks5Address;
                                socks.Port = set.Socks5Port;
                                socks.Username = set.Socks5Username;
                                socks.Password = set.Socks5Password;
                            }
                            Serilog.Log.Information("准备传递 SOCKS5: Enabled={Enabled}, Address={Addr}, Port={Port}, User={User}", enabled, socks.Address, socks.Port, socks.Username);
                            req.Socks5 = socks;
                            var rStart = await Task.Run(async () => await new JoinGame().Execute(req));
                            var tv = rStart.GetType().GetProperty("type")?.GetValue(rStart) as string;
                            if (string.Equals(tv, "channels_updated"))
                            {
                                NotificationHost.ShowGlobal("启动成功", ToastLevel.Success);
                                var autoCopy = SettingManager.Instance.Get().AutoCopyIpOnStart;
                                if (autoCopy)
                                {
                                    var ipProp = rStart.GetType().GetProperty("ip");
                                    var portProp = rStart.GetType().GetProperty("port");
                                    var ipVal = ipProp != null ? ipProp.GetValue(rStart) as string : null;
                                    var portObj = portProp != null ? portProp.GetValue(rStart) : null;
                                    var portStr = portObj != null ? portObj.ToString() : string.Empty;
                                    if (!string.IsNullOrWhiteSpace(ipVal))
                                    {
                                        var text = !string.IsNullOrWhiteSpace(portStr) ? (ipVal + ":" + portStr) : ipVal;
                                        var dp = new DataPackage();
                                        dp.SetText(text);
                                        Clipboard.SetContent(dp);
                                        Clipboard.Flush();
                                        NotificationHost.ShowGlobal("地址已复制到剪切板", ToastLevel.Success);
                                    }
                                }
                            }
                            break;
                        }
                        else if (result == ContentDialogResult.Secondary)
                        {
                            var addRoleContent = new AddRoleContent();
                        var dlg2 = new ContentDialog
                        {
                            XamlRoot = this.XamlRoot,
                            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                            Title = "添加角色",
                            Content = addRoleContent,
                            PrimaryButtonText = "添加",
                            CloseButtonText = "关闭",
                            DefaultButton = ContentDialogButton.Primary
                        };
                        try
                        {
                            var mode2 = SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
                            ElementTheme t2 = ElementTheme.Default;
                            if (mode2 == "light") t2 = ElementTheme.Light; else if (mode2 == "dark") t2 = ElementTheme.Dark;
                            dlg2.RequestedTheme = t2;
                        }
                        catch { }
                            var addRes = await dlg2.ShowAsync();
                            if (addRes == ContentDialogResult.Primary)
                            {
                                var name = addRoleContent.RoleName;
                                if (!string.IsNullOrWhiteSpace(name))
                                {
                                    var accId2 = joinContent.SelectedAccountId;
                                    if (!string.IsNullOrWhiteSpace(accId2))
                                    {
                                        await RunOnStaAsync(() => new SelectAccount().Execute(accId2));
                                    }
                                    var r2 = await RunOnStaAsync(() => new CreateRoleNamed().Execute(s.EntityId, name));
                                    var t2 = r2.GetType().GetProperty("type")?.GetValue(r2) as string;
                                    if (string.Equals(t2, "server_roles"))
                                    {
                                        var ip = r2.GetType().GetProperty("items");
                                        var il = ip?.GetValue(r2) as System.Collections.IEnumerable;
                                        var ri = new System.Collections.Generic.List<JoinServerContent.OptionItem>();
                                        if (il != null)
                                        {
                                            foreach (var it in il)
                                            {
                                                var idProp2 = it.GetType().GetProperty("id");
                                                var nameProp2 = it.GetType().GetProperty("name");
                                                var id2 = idProp2?.GetValue(it) as string ?? string.Empty;
                                                var name2 = nameProp2?.GetValue(it) as string ?? string.Empty;
                                                ri.Add(new JoinServerContent.OptionItem { Label = name2, Value = id2 });
                                            }
                                        }
                                        roleItems = ri;
                                        NotificationHost.ShowGlobal("角色创建成功", ToastLevel.Success);
                                    }
                                }
                            }
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    try
                    {
                        Serilog.Log.Error(ex, "打开服务器失败");
                        var dlg = new ContentDialog
                        {
                            XamlRoot = this.XamlRoot,
                            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                            Title = "错误",
                            Content = new TextBlock { Text = ex.Message },
                            CloseButtonText = "关闭"
                        };
                        try
                        {
                            var mode3 = SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
                            ElementTheme t3 = ElementTheme.Default;
                            if (mode3 == "light") t3 = ElementTheme.Light; else if (mode3 == "dark") t3 = ElementTheme.Dark;
                            dlg.RequestedTheme = t3;
                        }
                        catch { }
                        await dlg.ShowAsync();
                    }
                    catch { }
                }
            }
        }

        private void ServersGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var panel = ServersGrid.ItemsPanelRoot as ItemsWrapGrid;
            if (panel == null) return;
            var width = e.NewSize.Width;
            if (width <= 0) return;
            var itemWidth = Math.Max(240, (width - 24) / 4);
            panel.ItemWidth = itemWidth;
        }

        private void UpdatePageView()
        {
            try
            {
                if (PageInfoText != null) PageInfoText.Text = "第 " + _page + " 页";
                if (PrevPageButton != null) PrevPageButton.IsEnabled = _page > 1;
                if (NextPageButton != null) NextPageButton.IsEnabled = _hasMore;
            }
            catch { }
        }

        private void PrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_page <= 1) return;
            _page--;
            var q = (SearchBox?.Text ?? string.Empty);
            Servers.Clear();
            UpdatePageView();
            _ = RefreshServers(q);
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_hasMore) return;
            _page++;
            var q = (SearchBox?.Text ?? string.Empty);
            Servers.Clear();
            UpdatePageView();
            _ = RefreshServers(q);
        }

        private static Task<object> RunOnStaAsync(Func<object> func)
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    var r = func();
                    tcs.TrySetResult(r);
                }
                catch (System.Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            thread.IsBackground = true;
            try { thread.SetApartmentState(System.Threading.ApartmentState.STA); } catch { }
            thread.Start();
            return tcs.Task;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ServerItem : INotifyPropertyChanged
    {
        string _entityId;
        string _name;
        string _imageUrl;
        public string EntityId { get => _entityId; set { _entityId = value; OnPropertyChanged(nameof(EntityId)); } }
        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }
        public string ImageUrl { get => _imageUrl; set { _imageUrl = value; OnPropertyChanged(nameof(ImageUrl)); } }
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string name) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
    }
}
