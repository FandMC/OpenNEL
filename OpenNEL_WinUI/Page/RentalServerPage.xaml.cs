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
using OpenNEL_WinUI.Handlers.Game.RentalServer;
using System.ComponentModel;
using OpenNEL_WinUI.Manager;
using Windows.ApplicationModel.DataTransfer;
using OpenNEL.SDK.Entities;
using OpenNEL_WinUI.Entities.Web.RentalGame;
using Serilog;
using static OpenNEL_WinUI.Utils.StaTaskRunner;

namespace OpenNEL_WinUI
{
    public sealed partial class RentalServerPage : Page, INotifyPropertyChanged
    {
        public static string PageTitle => "租赁服";
        public ObservableCollection<RentalServerItem> Servers { get; } = new ObservableCollection<RentalServerItem>();
        private bool _notLogin;
        public bool NotLogin { get => _notLogin; private set { _notLogin = value; OnPropertyChanged(nameof(NotLogin)); } }
        private System.Threading.CancellationTokenSource? _cts;
        private int _page = 1;
        private const int PageSize = 20;
        private bool _hasMore;
        private int _refreshId;

        public RentalServerPage()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Loaded += RentalServerPage_Loaded;
        }

        private async void RentalServerPage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshServers();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _page = 1;
            await RefreshServers();
        }

        private async Task RefreshServers()
        {
            Log.Debug("[RentalServer] RefreshServers: page={Page}", _page);
            var cts = _cts;
            cts?.Cancel();
            _cts = new System.Threading.CancellationTokenSource();
            var token = _cts.Token;
            var my = System.Threading.Interlocked.Increment(ref _refreshId);

            ListRentalServersResult r;
            try
            {
                r = await RunOnStaAsync(() =>
                {
                    if (token.IsCancellationRequested) return new ListRentalServersResult();
                    var offset = Math.Max(0, (_page - 1) * PageSize);
                    return new ListRentalServers().Execute(offset, PageSize);
                });
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, "[RentalServer] RefreshServers 异常");
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

            foreach (var item in r.Items)
            {
                if (my != _refreshId || token.IsCancellationRequested) break;
                Servers.Add(item);
            }
            UpdatePageView();
        }

        private async void JoinServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RentalServerItem s)
            {
                try
                {
                    var r = await RunOnStaAsync(() => new OpenRentalServer().Execute(s.EntityId));
                    if (!r.Success) return;

                    var accounts = UserManager.Instance.GetUsersNoDetails();
                    var acctItems = accounts
                        .Where(a => a.Authorized)
                        .Select(a => new JoinRentalServerContent.OptionItem
                        {
                            Label = (string.IsNullOrWhiteSpace(a.Alias) ? a.UserId : a.Alias) + " (" + a.Channel + ")",
                            Value = a.UserId
                        })
                        .ToList();

                    var roleItems = r.Items.Select(x => new JoinRentalServerContent.OptionItem { Label = x.Name, Value = x.Id }).ToList();

                    while (true)
                    {
                        var joinContent = new JoinRentalServerContent();
                        joinContent.SetAccounts(acctItems);
                        joinContent.SetRoles(roleItems);
                        joinContent.SetPasswordRequired(s.HasPassword);
                        joinContent.AccountChanged += async (accountId) =>
                        {
                            try
                            {
                                await RunOnStaAsync(() => new Handlers.Game.NetServer.SelectAccount().Execute(accountId));
                                var rAcc = await RunOnStaAsync(() => new OpenRentalServer().ExecuteForAccount(accountId, s.EntityId));
                                if (rAcc.Success)
                                {
                                    roleItems = rAcc.Items.Select(x => new JoinRentalServerContent.OptionItem { Label = x.Name, Value = x.Id }).ToList();
                                    joinContent.SetRoles(roleItems);
                                }
                            }
                            catch { }
                        };

                        var dlg = new ThemedContentDialog
                        {
                            XamlRoot = this.XamlRoot,
                            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                            Title = "加入租赁服",
                            Content = joinContent,
                            PrimaryButtonText = "启动",
                            CloseButtonText = "关闭",
                            DefaultButton = ContentDialogButton.Primary
                        };
                        joinContent.ParentDialog = dlg;

                        var result = await dlg.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            var accId = joinContent.SelectedAccountId;
                            var roleId = joinContent.SelectedRoleId;
                            var password = joinContent.Password;
                            if (string.IsNullOrWhiteSpace(accId) || string.IsNullOrWhiteSpace(roleId)) continue;
                            if (s.HasPassword && string.IsNullOrWhiteSpace(password))
                            {
                                NotificationHost.ShowGlobal("请输入服务器密码", ToastLevel.Error);
                                continue;
                            }

                            NotificationHost.ShowGlobal("正在准备游戏资源，请稍后", ToastLevel.Success);
                            await RunOnStaAsync(() => new Handlers.Game.NetServer.SelectAccount().Execute(accId));

                            var req = new EntityJoinRentalGame
                            {
                                ServerId = s.EntityId,
                                ServerName = s.Name,
                                Role = roleId,
                                GameId = s.EntityId,
                                Password = password,
                                McVersion = s.McVersion
                            };
                            var set = SettingManager.Instance.Get();
                            var enabled = set?.Socks5Enabled ?? false;
                            req.Socks5 = (!enabled || string.IsNullOrWhiteSpace(set?.Socks5Address))
                                ? new EntitySocks5 { Address = string.Empty, Port = 0, Username = string.Empty, Password = string.Empty }
                                : new EntitySocks5 { Address = set!.Socks5Address, Port = set.Socks5Port, Username = set.Socks5Username, Password = set.Socks5Password };

                            var rStart = await Task.Run(async () => await new JoinRentalGame().Execute(req));
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
                            else
                            {
                                NotificationHost.ShowGlobal(rStart.Message ?? "启动失败", ToastLevel.Error);
                            }
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
                                    await RunOnStaAsync(() => new Handlers.Game.NetServer.SelectAccount().Execute(accId2));
                                var r2 = await RunOnStaAsync(() => new CreateRentalRole().Execute(s.EntityId, roleName));
                                if (r2.Success)
                                {
                                    roleItems = r2.Items.Select(x => new JoinRentalServerContent.OptionItem { Label = x.Name, Value = x.Id }).ToList();
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
                catch (System.Exception ex)
                {
                    Log.Error(ex, "加入租赁服失败");
                    NotificationHost.ShowGlobal("加入失败: " + ex.Message, ToastLevel.Error);
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
            Servers.Clear();
            UpdatePageView();
            _ = RefreshServers();
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_hasMore) return;
            _page++;
            Servers.Clear();
            UpdatePageView();
            _ = RefreshServers();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
