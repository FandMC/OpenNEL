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
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OpenNEL_WinUI.Handlers.Game.NetServer;
using Windows.ApplicationModel.DataTransfer;
using OpenNEL_WinUI.Handlers.Game;

namespace OpenNEL_WinUI
{
    public sealed partial class GamesPage : Page
    {
        public static string PageTitle => "游戏";
        public ObservableCollection<GameSessionItem> Sessions { get; } = new ObservableCollection<GameSessionItem>();

        public GamesPage()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Loaded += GamesPage_Loaded;
        }

        private async void GamesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshSessions();
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

        private async Task RefreshSessions()
        {
            Sessions.Clear();
            object result;
            try
            {
                result = await RunOnStaAsync(() => new QueryGameSession().Execute());
            }
            catch
            {
                return;
            }
            var typeProp = result.GetType().GetProperty("type");
            var typeVal = typeProp != null ? typeProp.GetValue(result) as string : null;
            if (!string.Equals(typeVal, "query_game_session")) return;
            var itemsProp = result.GetType().GetProperty("items");
            var items = itemsProp?.GetValue(result) as System.Collections.IEnumerable;
            if (items == null) return;
            foreach (var it in items)
            {
                var id = it.GetType().GetProperty("Id")?.GetValue(it) as string ?? string.Empty;
                var serverName = it.GetType().GetProperty("ServerName")?.GetValue(it) as string ?? string.Empty;
                var characterName = it.GetType().GetProperty("CharacterName")?.GetValue(it) as string ?? string.Empty;
                var type = it.GetType().GetProperty("Type")?.GetValue(it) as string ?? string.Empty;
                var status = it.GetType().GetProperty("StatusText")?.GetValue(it) as string ?? string.Empty;
                var local = it.GetType().GetProperty("LocalAddress")?.GetValue(it) as string ?? string.Empty;
                var identifier = it.GetType().GetProperty("Guid")?.GetValue(it) as string ?? string.Empty;
                Sessions.Add(new GameSessionItem
                {
                    Id = id,
                    ServerName = serverName,
                    CharacterName = characterName,
                    Type = type,
                    StatusText = status,
                    LocalAddress = local,
                    Identifier = identifier
                });
            }
        }

        private void CopyIpButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var text = btn.Tag as string;
                if (string.IsNullOrWhiteSpace(text)) return;
                var pkg = new DataPackage();
                pkg.SetText(text);
                try
                {
                    Clipboard.SetContent(pkg);
                }
                catch { }
            }
        }

        private async void ShutdownButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var identifier = btn.Tag as string;
                if (string.IsNullOrWhiteSpace(identifier)) return;
                try
                {
                    await RunOnStaAsync(() => new ShutdownGame().Execute(new[] { identifier }));
                }
                catch { }
                NotificationHost.ShowGlobal("通道已成功关闭", ToastLevel.Success);
                await RefreshSessions();
            }
        }
    }

    public class GameSessionItem
    {
        public string Id { get; set; }
        public string ServerName { get; set; }
        public string CharacterName { get; set; }
        public string Type { get; set; }
        public string StatusText { get; set; }
        public string LocalAddress { get; set; }
        public string Identifier { get; set; }
        public string CharacterDisplay => (CharacterName ?? string.Empty) + " · " + (Type ?? string.Empty);
    }
}
