/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Codexus.Development.SDK.Entities;
using Codexus.Game.Launcher.Entities;
using OpenNEL_WinUI.Entities.Web.NetGame;
using OpenNEL_WinUI.Handlers.Game.NetServer;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.Utils;
using Serilog;
using Windows.ApplicationModel.DataTransfer;
using static OpenNEL_WinUI.Utils.StaTaskRunner;

namespace OpenNEL_WinUI;

public sealed partial class NetworkServerPage : Page, INotifyPropertyChanged
{
    public static string PageTitle => "网络服务器";

    readonly SemaphoreSlim _imageLimiter = new(6);
    CancellationTokenSource? _cts;
    int _page = 1;
    int _refreshId;
    bool _hasMore;
    bool _notLogin;

    public ObservableCollection<ServerItem> Servers { get; } = new();
    public bool NotLogin { get => _notLogin; private set { _notLogin = value; OnPropertyChanged(nameof(NotLogin)); } }

    public NetworkServerPage()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += async (_, _) => await RefreshAsync();
    }

    // ========== 数据加载 ==========
    async Task RefreshAsync(string? keyword = null)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        var myId = Interlocked.Increment(ref _refreshId);

        var result = await RunOnStaAsync(() =>
        {
            if (ct.IsCancellationRequested) return new ListServersResult();
            var offset = Math.Max(0, (_page - 1) * 20);
            return string.IsNullOrWhiteSpace(keyword)
                ? new ListServers().Execute(offset, 20)
                : new SearchServers().Execute(keyword, offset, 20);
        });
        if (myId != _refreshId) return;

        NotLogin = result.NotLogin;
        Servers.Clear();
        _hasMore = result.HasMore;

        if (!result.Success) { UpdatePaging(); return; }

        if (_page == 1 && DateTime.Now <= new DateTime(2026, 1, 5))
            Servers.Add(new ServerItem { EntityId = "77114517833647104", Name = "花雨庭(2026.1.5 R.I.P)" });

        foreach (var item in result.Items)
        {
            if (ct.IsCancellationRequested) break;
            Servers.Add(item);
            _ = LoadImageAsync(item, myId, ct);
        }
        UpdatePaging();
    }

    async Task LoadImageAsync(ServerItem item, int myId, CancellationToken ct)
    {
        await _imageLimiter.WaitAsync(ct);
        try
        {
            if (myId != _refreshId || ct.IsCancellationRequested) return;
            var d = await RunOnStaAsync(() => new GetServersDetail().Execute(item.EntityId));
            if (d.Success && d.Images.Count > 0 && myId == _refreshId)
                DispatcherQueue.TryEnqueue(() => item.ImageUrl = d.Images[0]);
        }
        catch (Exception ex) { Log.Debug(ex, "加载图片失败"); }
        finally { _imageLimiter.Release(); }
    }

    // ========== UI 事件 ==========
    void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _page = 1;
        Servers.Clear();
        UpdatePaging();
        _ = RefreshAsync((sender as TextBox)?.Text);
    }

    async void SpecifyServerButton_Click(object sender, RoutedEventArgs e)
    {
        var id = await DialogService.ShowInputAsync(XamlRoot, "指定服务器", "请输入服务器号");
        if (string.IsNullOrWhiteSpace(id)) { if (id != null) NotificationHost.ShowGlobal("请输入服务器号", ToastLevel.Error); return; }
        await JoinServerAsync(id, id);
    }

    async void JoinServerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ServerItem s }) await JoinServerAsync(s.EntityId, s.Name);
    }

    void PrevPageButton_Click(object sender, RoutedEventArgs e) { if (_page > 1) { _page--; _ = RefreshAsync(SearchBox?.Text); } }
    void NextPageButton_Click(object sender, RoutedEventArgs e) { if (_hasMore) { _page++; _ = RefreshAsync(SearchBox?.Text); } }

    void ServersGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ServersGrid.ItemsPanelRoot is ItemsWrapGrid panel && e.NewSize.Width > 0)
            panel.ItemWidth = Math.Max(240, (e.NewSize.Width - 24) / 4);
    }

    // ========== 加入服务器 ==========
    async Task JoinServerAsync(string serverId, string serverName)
    {
        var openResult = await RunOnStaAsync(() => new OpenServer().Execute(serverId));
        if (!openResult.Success) { await DialogService.ShowErrorAsync(XamlRoot, openResult.Message ?? "打开失败"); return; }

        var accounts = UserManager.Instance.GetAuthorizedAccounts();
        var roles = openResult.Items;

        while (true)
        {
            var content = new JoinServerContent();
            content.SetAccounts(accounts.Select(a => new JoinServerContent.OptionItem { Label = a.Label, Value = a.Id }).ToList());
            content.SetRoles(roles.Select(r => new JoinServerContent.OptionItem { Label = r.Name, Value = r.Id }).ToList());
            content.AccountChanged += async id =>
            {
                await RunOnStaAsync(() => new SelectAccount().Execute(id));
                var r = await RunOnStaAsync(() => new OpenServer().ExecuteForAccount(id, serverId));
                if (r.Success) { roles = r.Items; DispatcherQueue.TryEnqueue(() => content.SetRoles(roles.Select(x => new JoinServerContent.OptionItem { Label = x.Name, Value = x.Id }).ToList())); }
            };

            var dlg = DialogService.Create(XamlRoot, "加入服务器", content, "启动", "白端", "关闭");
            content.ParentDialog = dlg;
            var result = await dlg.ShowAsync();
            var accId = content.SelectedAccountId;
            var roleId = content.SelectedRoleId;

            if (result == ContentDialogResult.Primary)
            {
                if (string.IsNullOrWhiteSpace(accId) || string.IsNullOrWhiteSpace(roleId)) continue;
                await LaunchGameAsync(accId, serverId, serverName, roleId);
                break;
            }
            if (result == ContentDialogResult.Secondary)
            {
                if (string.IsNullOrWhiteSpace(accId) || string.IsNullOrWhiteSpace(roleId)) continue;
                LaunchWhiteGame(accId, serverId, serverName, roleId);
                break;
            }
            if (result == ContentDialogResult.None && content.AddRoleRequested)
            {
                var newRoles = await AddRoleAsync(serverId);
                if (newRoles != null) roles = newRoles;
                content.ResetAddRoleRequested();
                continue;
            }
            break;
        }
    }

    async Task LaunchGameAsync(string accId, string serverId, string serverName, string roleId)
    {
        NotificationHost.ShowGlobal("正在准备游戏资源...", ToastLevel.Success);
        Log.Information("启动游戏: Server={Server}, Role={Role}", serverId, roleId);
        var r = await Task.Run(async () => await new JoinGame().Execute(accId, serverId, serverName, roleId));
        if (r.Success)
        {
            NotificationHost.ShowGlobal("启动成功", ToastLevel.Success);
            var copyText = SettingManager.Instance.GetCopyIpText(r.Ip, r.Port);
            if (copyText != null)
            {
                var dp = new DataPackage(); dp.SetText(copyText); Clipboard.SetContent(dp); Clipboard.Flush();
                NotificationHost.ShowGlobal("地址已复制", ToastLevel.Success);
            }
        }
    }

    void LaunchWhiteGame(string accId, string serverId, string serverName, string roleId)
    {
        NotificationHost.ShowGlobal("正在准备白端资源...", ToastLevel.Success);
        var progress = new Progress<EntityProgressUpdate>(u =>
        {
            Log.Information("白端: {Msg} ({P}%)", u.Message, u.Percent);
            DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal($"白端: {u.Message} ({u.Percent}%)", ToastLevel.Normal));
        });
        _ = Task.Run(async () =>
        {
            try
            {
                await RunOnStaAsync(() => new SelectAccount().Execute(accId));
                var r = await new LaunchWhiteGame(progress).Execute(accId, serverId, serverName, roleId);
                DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal(r.Success ? "白端启动成功" : $"白端失败: {r.Message}", r.Success ? ToastLevel.Success : ToastLevel.Error));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "白端启动失败");
                DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal($"白端失败: {ex.Message}", ToastLevel.Error));
            }
        });
    }

    async Task<List<RoleItem>?> AddRoleAsync(string serverId)
    {
        var addContent = new AddRoleContent();
        var dlg = DialogService.Create(XamlRoot, "添加角色", addContent, "添加", null, "关闭");
        if (await dlg.ShowAsync() != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(addContent.RoleName)) return null;
        var roleName = addContent.RoleName;
        var r = await RunOnStaAsync(() => new CreateRoleNamed().Execute(serverId, roleName));
        return r.Success ? r.Items : null;
    }

    void UpdatePaging()
    {
        if (PageInfoText != null) PageInfoText.Text = $"第 {_page} 页";
        if (PrevPageButton != null) PrevPageButton.IsEnabled = _page > 1;
        if (NextPageButton != null) NextPageButton.IsEnabled = _hasMore;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
