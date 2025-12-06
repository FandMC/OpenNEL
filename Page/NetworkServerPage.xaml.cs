using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using OpenNEL_WinUI.Handlers.Game;
using System.ComponentModel;
using OpenNEL.Manager;
using OpenNEL.Entities.Web.NetGame;

namespace OpenNEL_WinUI
{
    public sealed partial class NetworkServerPage : Page, INotifyPropertyChanged
    {
        public static string PageTitle => "网络服务器";
        public ObservableCollection<ServerItem> Servers { get; } = new ObservableCollection<ServerItem>();
        private bool _notLogin;
        public bool NotLogin { get => _notLogin; private set { _notLogin = value; OnPropertyChanged(nameof(NotLogin)); } }
        private System.Threading.CancellationTokenSource _cts;

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
            try
            {
                r = await RunOnStaAsync(() =>
                {
                    if (token.IsCancellationRequested) return new { type = "servers", items = System.Array.Empty<object>() };
                    if (string.IsNullOrWhiteSpace(keyword))
                    {
                        return new ListServers().Execute();
                    }
                    return new SearchServers().Execute(keyword);
                });
            }
            catch (System.Exception)
            {
                NotLogin = false;
                Servers.Clear();
                return;
            }
            var tProp = r.GetType().GetProperty("type");
            var tVal = tProp != null ? tProp.GetValue(r) as string : null;
            if (string.Equals(tVal, "notlogin"))
            {
                NotLogin = true;
                Servers.Clear();
                return;
            }
            NotLogin = false;
            Servers.Clear();
            var itemsProp = r.GetType().GetProperty("items");
            var items = itemsProp?.GetValue(r) as System.Collections.IEnumerable;
            if (items != null)
            {
                foreach (var item in items)
                {
                    var idProp = item.GetType().GetProperty("entityId");
                    var nameProp = item.GetType().GetProperty("name");
                    var id = idProp?.GetValue(item) as string ?? string.Empty;
                    var name = nameProp?.GetValue(item) as string ?? string.Empty;
                    Servers.Add(new ServerItem { EntityId = id, Name = name });
                }
            }
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (sender as TextBox)?.Text ?? string.Empty;
            await RefreshServers(q);
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
                        .Where(a => !IsOfflineChannel(a.Channel))
                        .Select(a => new JoinServerContent.OptionItem { Label = a.UserId + " (" + a.Channel + ")", Value = a.UserId })
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

                        var result = await dlg.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            var accId = joinContent.SelectedAccountId;
                            var roleId = joinContent.SelectedRoleId;
                            if (string.IsNullOrWhiteSpace(accId) || string.IsNullOrWhiteSpace(roleId))
                            {
                                continue;
                            }
                        var rSel = await RunOnStaAsync(() => new SelectAccount().Execute(accId));
                        var req = new EntityJoinGame { ServerId = s.EntityId, ServerName = s.Name, Role = roleId, GameId = s.EntityId };
                        var rStart = await Task.Run(async () => await new JoinGame().Execute(req));
                        var tv = rStart.GetType().GetProperty("type")?.GetValue(rStart) as string;
                        if (string.Equals(tv, "channels_updated")) { NotificationHost.ShowGlobal("启动成功", ToastLevel.Success); break; }
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
                            var addRes = await dlg2.ShowAsync();
                            if (addRes == ContentDialogResult.Primary)
                            {
                                var name = addRoleContent.RoleName;
                                if (!string.IsNullOrWhiteSpace(name))
                                {
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

        private static Task<object> RunOnStaAsync(System.Func<object> func)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<object>();
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

        private static bool IsOfflineChannel(string channel)
        {
            var s = (channel ?? string.Empty).Trim().ToLowerInvariant();
            return s.Contains("离线") || s.Contains("offline");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ServerItem
    {
        public string EntityId { get; set; }
        public string Name { get; set; }
    }
}
