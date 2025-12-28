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
using OpenNEL_WinUI.Handlers.Game.NetServer;
using System.ComponentModel;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.Entities.Web.NetGame;
using Windows.ApplicationModel.DataTransfer;
using OpenNEL.SDK.Entities;
using OpenNEL.GameLauncher.Entities;
using OpenNEL_WinUI.type;
using OpenNEL_WinUI.Utils;
using Serilog;
using static OpenNEL_WinUI.Utils.StaTaskRunner;

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
            InitializeComponent();
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
            var my = System.Threading.Interlocked.Increment(ref _refreshId);

            ListServersResult r;
            try
            {
                r = await RunOnStaAsync(() =>
                {
                    if (token.IsCancellationRequested) return new ListServersResult();
                    var offset = Math.Max(0, (_page - 1) * PageSize);
                    if (string.IsNullOrWhiteSpace(keyword))
                        return new ListServers().Execute(offset, PageSize);
                    return new SearchServers().Execute(keyword, offset, PageSize);
                });
            }
            catch
            {
                NotLogin = false;
                Servers.Clear();
                UpdatePageView();
                return;
            }

            if (my != _refreshId) return;
            if (r.NotLogin)
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
            _hasMore = r.HasMore;

            var limiter = new System.Threading.SemaphoreSlim(6);
            foreach (var item in r.Items)
            {
                if (my != _refreshId || token.IsCancellationRequested) break;
                Servers.Add(item);
                _ = Task.Run(async () =>
                {
                    await limiter.WaitAsync();
                    try
                    {
                        if (my != _refreshId || token.IsCancellationRequested) return;
                        var d = await RunOnStaAsync(() => new GetServersDetail().Execute(item.EntityId));
                        if (my != _refreshId || token.IsCancellationRequested) return;
                        if (d.Success && d.Images.Count > 0)
                        {
                            var url = d.Images[0];
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                if (my != _refreshId || token.IsCancellationRequested) return;
                                item.ImageUrl = url;
                            });
                        }
                    }
                    catch { }
                    finally { try { limiter.Release(); } catch { } }
                });
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
                    var r = await RunOnStaAsync(() => new OpenServer().Execute(s.EntityId));
                    if (!r.Success) return;

                    var accounts = UserManager.Instance.GetUsersNoDetails();
                    var acctItems = accounts
                        .Where(a => a.Authorized)
                        .Select(a => new JoinServerContent.OptionItem
                        {
                            Label = (string.IsNullOrWhiteSpace(a.Alias) ? a.UserId : a.Alias) + " (" + a.Channel + ")",
                            Value = a.UserId
                        })
                        .ToList();

                    var roleItems = r.Items.Select(x => new JoinServerContent.OptionItem { Label = x.Name, Value = x.Id }).ToList();

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
                                var rAcc = await RunOnStaAsync(() => new OpenServer().ExecuteForAccount(accountId, s.EntityId));
                                if (rAcc.Success)
                                {
                                    roleItems = rAcc.Items.Select(x => new JoinServerContent.OptionItem { Label = x.Name, Value = x.Id }).ToList();
                                    joinContent.SetRoles(roleItems);
                                }
                            }
                            catch { }
                        };

                        var dlg = new ThemedContentDialog
                        {
                            XamlRoot = this.XamlRoot,
                            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                            Title = "加入服务器",
                            Content = joinContent,
                            PrimaryButtonText = "启动",
                            SecondaryButtonText = "白端",
                            CloseButtonText = "关闭",
                            DefaultButton = ContentDialogButton.Primary
                        };
                        joinContent.ParentDialog = dlg;

                        var result = await dlg.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            var accId = joinContent.SelectedAccountId;
                            var roleId = joinContent.SelectedRoleId;
                            if (string.IsNullOrWhiteSpace(accId) || string.IsNullOrWhiteSpace(roleId)) continue;

                            NotificationHost.ShowGlobal("正在准备游戏资源，请稍后", ToastLevel.Success);
                            await RunOnStaAsync(() => new SelectAccount().Execute(accId));

                            var req = new EntityJoinGame { ServerId = s.EntityId, ServerName = s.Name, Role = roleId, GameId = s.EntityId };
                            var set = SettingManager.Instance.Get();
                            var enabled = set?.Socks5Enabled ?? false;
                            req.Socks5 = (!enabled || string.IsNullOrWhiteSpace(set?.Socks5Address))
                                ? new EntitySocks5 { Address = string.Empty, Port = 0, Username = string.Empty, Password = string.Empty }
                                : new EntitySocks5 { Address = set!.Socks5Address, Port = set.Socks5Port, Username = set.Socks5Username, Password = set.Socks5Password };

                            Log.Information("准备传递 SOCKS5: Enabled={Enabled}, Address={Addr}, Port={Port}, User={User}", enabled, req.Socks5.Address, req.Socks5.Port, req.Socks5.Username);
                            var rStart = await Task.Run(async () => await new JoinGame().Execute(req));

                            if (rStart.Success)
                            {
                                NotificationHost.ShowGlobal("启动成功", ToastLevel.Success);
                                if (SettingManager.Instance.Get().AutoCopyIpOnStart && !string.IsNullOrWhiteSpace(rStart.Ip))
                                {
                                    var text = rStart.Port > 0 ? $"{rStart.Ip}:{rStart.Port}" : rStart.Ip;
                                    var dp = new DataPackage();
                                    dp.SetText(text);
                                    Clipboard.SetContent(dp);
                                    Clipboard.Flush();
                                    NotificationHost.ShowGlobal("地址已复制到剪切板", ToastLevel.Success);
                                }
                            }
                            break;
                        }
                        else if (result == ContentDialogResult.Secondary)
                        {
                            var accId = joinContent.SelectedAccountId;
                            var roleId = joinContent.SelectedRoleId;
                            if (string.IsNullOrWhiteSpace(accId) || string.IsNullOrWhiteSpace(roleId)) continue;

                            NotificationHost.ShowGlobal("正在准备白端资源，请稍后", ToastLevel.Success);
                            var progress = new Progress<EntityProgressUpdate>(update =>
                            {
                                Log.Information("白端启动进度: {Message} ({Percent}%)", update.Message, update.Percent);
                                DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal($"白端启动: {update.Message} ({update.Percent}%)", ToastLevel.Normal));
                            });
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await RunOnStaAsync(() => new SelectAccount().Execute(accId));
                                    var rLaunch = await new LaunchWhiteGame(progress).Execute(accId, s.EntityId, s.Name, roleId);
                                    DispatcherQueue.TryEnqueue(() =>
                                    {
                                        if (rLaunch.Success)
                                            NotificationHost.ShowGlobal("白端启动成功", ToastLevel.Success);
                                        else
                                            NotificationHost.ShowGlobal("白端启动失败: " + (rLaunch.Message ?? "启动失败"), ToastLevel.Error);
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "白端启动失败");
                                    DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal("白端启动失败: " + ex.Message, ToastLevel.Error));
                                }
                            });
                            break;
                        }
                        else if (result == ContentDialogResult.None && joinContent.AddRoleRequested)
                        {
                            var addRoleContent = new AddRoleContent();
                            var dlg2 = new ThemedContentDialog
                            {
                                XamlRoot = this.XamlRoot,
                                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                                Title = "添加角色",
                                Content = addRoleContent,
                                PrimaryButtonText = "添加",
                                CloseButtonText = "关闭",
                                DefaultButton = ContentDialogButton.Primary
                            };
                            var addRes = await dlg2.ShowAsync();
                            if (addRes == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(addRoleContent.RoleName))
                            {
                                var roleName = addRoleContent.RoleName;
                                var accId2 = joinContent.SelectedAccountId;
                                if (!string.IsNullOrWhiteSpace(accId2))
                                    await RunOnStaAsync(() => new SelectAccount().Execute(accId2));
                                var r2 = await RunOnStaAsync(() => new CreateRoleNamed().Execute(s.EntityId, roleName));
                                if (r2.Success)
                                {
                                    roleItems = r2.Items.Select(x => new JoinServerContent.OptionItem { Label = x.Name, Value = x.Id }).ToList();
                                    NotificationHost.ShowGlobal("角色创建成功", ToastLevel.Success);
                                }
                            }
                            joinContent.ResetAddRoleRequested();
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "打开服务器失败");
                    try
                    {
                        var dlg = new ThemedContentDialog
                        {
                            XamlRoot = this.XamlRoot,
                            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                            Title = "错误",
                            Content = new TextBlock { Text = ex.Message },
                            CloseButtonText = "关闭"
                        };
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
