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
using System.ComponentModel;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenNEL_WinUI.Utils;
using OpenNEL_WinUI.Handlers.Plugin;
using OpenNEL_WinUI.Manager;
using OpenNEL.Core.Utils;
using System.Text.Json;

namespace OpenNEL_WinUI
{
    public sealed partial class MainWindow : Window
    {
        static MainWindow? _instance;
        AppWindow? _appWindow;
        string _currentBackdrop = "";
        string _currentCaptchaId = "";
        public static Microsoft.UI.Dispatching.DispatcherQueue? UIQueue => _instance?.DispatcherQueue;
        public MainWindow()
        {
            InitializeComponent();
            _instance = this;
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            _appWindow.Title = "Open NEL";
            AppTitleTextBlock.Text = _appWindow.Title;
            ApplyThemeFromSettings();
        }

        private async void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            if (AuthManager.Instance.IsLoggedIn)
            {
                var result = await AuthManager.Instance.VerifyAsync();
                if (result.Success)
                {
                    CrcSalt.TokenProvider = () => AuthManager.Instance.Token ?? "";
                    CrcSalt.InvalidateCache();
                    _ = CrcSalt.Compute();
                    NotificationHost.ShowGlobal("已自动登录，欢迎回来", ToastLevel.Success);
                }
                else
                {
                    LoginOverlay.Visibility = Visibility.Visible;
                    NavView.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                LoginOverlay.Visibility = Visibility.Visible;
                NavView.Visibility = Visibility.Collapsed;
            }

            AddNavItem(Symbol.Home, "HomePage");
            AddNavItem(Symbol.World, "NetworkServerPage");
            AddNavItem(Symbol.Remote, "RentalServerPage");
            AddNavItem(Symbol.AllApps, "PluginsPage");
            AddNavItem(Symbol.Play, "GamesPage");
            AddNavItem(Symbol.AllApps, "SkinPage");
            AddNavItem(Symbol.Setting, "ToolsPage");
            AddNavItem(Symbol.ContactInfo, "AboutPage");

            foreach (NavigationViewItemBase item in NavView.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag.ToString() == "HomePage")
                {
                    NavView.SelectedItem = navItem;
                    ContentFrame.Navigate(typeof(HomePage));
                    break;
                }
            }

            await ShowFirstRunInstallDialogAsync();
        }

        private void AddNavItem(Symbol icon, string pageName)
        {
            string fullPageName = "OpenNEL_WinUI." + pageName;
            Type pageType = Type.GetType(fullPageName);
            if (pageType != null)
            {
                var prop = pageType.GetProperty("PageTitle", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                string title = prop?.GetValue(null) as string ?? pageType.Name;

                NavView.MenuItems.Add(new NavigationViewItem
                {
                    Icon = new SymbolIcon(icon),
                    Content = title,
                    Tag = pageName
                });
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else
            {
                var selectedItem = (NavigationViewItem)args.SelectedItem;
                if (selectedItem != null)
                {
                    string pageName = "OpenNEL_WinUI." + selectedItem.Tag.ToString();
                    Type pageType = Type.GetType(pageName);
                    ContentFrame.Navigate(pageType);
                }
            }
        }

        private void NavView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Left)
            {
                NavView.OpenPaneLength = e.NewSize.Width * 0.2; 
            }
        }

        

        void ApplyThemeFromSettings()
        {
            try
            {
                var mode = OpenNEL_WinUI.Manager.SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
                ElementTheme t = ElementTheme.Default;
                if (mode == "light") t = ElementTheme.Light;
                else if (mode == "dark") t = ElementTheme.Dark;
                RootGrid.RequestedTheme = t;
                NavView.RequestedTheme = t;
                ContentFrame.RequestedTheme = t;
                var actual = t == ElementTheme.Default ? RootGrid.ActualTheme : t;
                UpdateTitleBarColors(actual);

                var bd = OpenNEL_WinUI.Manager.SettingManager.Instance.Get().Backdrop?.Trim().ToLowerInvariant() ?? "mica";
                if (bd != _currentBackdrop)
                {
                    if (bd == "acrylic")
                    {
                        SystemBackdrop = new DesktopAcrylicBackdrop();
                        RootGrid.Background = null;
                    }
                    else
                    {
                        SystemBackdrop = new MicaBackdrop();
                        RootGrid.Background = null;
                    }
                    _currentBackdrop = bd;
                }
            }
            catch { }
        }


        public static void ApplyThemeFromSettingsStatic()
        {
            _instance?.ApplyThemeFromSettings();
        }

        void UpdateTitleBarColors(ElementTheme theme)
        {
            try
            {
                var tb = _appWindow?.TitleBar;
                if (tb == null) return;
                var fg = ColorUtil.ForegroundForTheme(theme);
                var bg = ColorUtil.Transparent;
                tb.ForegroundColor = fg;
                tb.InactiveForegroundColor = fg;
                tb.ButtonForegroundColor = fg;
                tb.ButtonInactiveForegroundColor = fg;
                tb.BackgroundColor = bg;
                tb.InactiveBackgroundColor = bg;
                tb.ButtonHoverForegroundColor = fg;
                tb.ButtonPressedForegroundColor = fg;
                tb.ButtonBackgroundColor = ColorUtil.Transparent;
                tb.ButtonInactiveBackgroundColor = ColorUtil.Transparent;
                tb.ButtonHoverBackgroundColor = ColorUtil.HoverBackgroundForTheme(theme);
                tb.ButtonPressedBackgroundColor = ColorUtil.PressedBackgroundForTheme(theme);
            }
            catch { }
        }

        async System.Threading.Tasks.Task ShowFirstRunInstallDialogAsync()
        {
            try
            {
                var detection = PluginHandler.DetectDefaultProtocolsInstalled();
                var hasBase = detection.hasBase1200;
                var hasHp = detection.hasHeypixel;
                if (hasHp && hasBase)
                {
                    try
                    {
                        var data = OpenNEL_WinUI.Manager.SettingManager.Instance.Get();
                        OpenNEL_WinUI.Manager.SettingManager.Instance.Update(data);
                    }
                    catch { }
                    return;
                }
                if (hasHp && !hasBase)
                {
                    var d2 = new ThemedContentDialog
                    {
                        XamlRoot = RootGrid.XamlRoot,
                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = "提示",
                        Content = new TextBlock { Text = "检测到安装了布吉岛协议但是未安装前置，是否安装" },
                        PrimaryButtonText = "确定",
                        CloseButtonText = "取消",
                        DefaultButton = ContentDialogButton.Primary
                    };
                    d2.PrimaryButtonClick += async (s, e) =>
                    {
                        e.Cancel = true;
                        d2.IsPrimaryButtonEnabled = false;
                        try
                        {
                            _ = PluginHandler.InstallBase1200Async();
                        }
                        catch { }
                        d2.IsPrimaryButtonEnabled = true;
                        try { d2.Hide(); } catch { }
                        try
                        {
                            var data = OpenNEL_WinUI.Manager.SettingManager.Instance.Get();
                            OpenNEL_WinUI.Manager.SettingManager.Instance.Update(data);
                        }
                        catch { }
                    };
                    d2.Closed += (s, e) =>
                    {
                        try
                        {
                            var data = OpenNEL_WinUI.Manager.SettingManager.Instance.Get();
                            OpenNEL_WinUI.Manager.SettingManager.Instance.Update(data);
                        }
                        catch { }
                    };
                    await d2.ShowAsync();
                    return;
                }
                if (!hasHp && !hasBase && !System.IO.File.Exists("setting.json"))
                {
                    var d = new ThemedContentDialog
                    {
                        XamlRoot = RootGrid.XamlRoot,
                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = "提示",
                        Content = new TextBlock { Text = "检测到您第一次用这个软件，是否要安装布吉岛协议？" },
                        PrimaryButtonText = "确定",
                        CloseButtonText = "取消",
                        DefaultButton = ContentDialogButton.Primary
                    };
                    d.PrimaryButtonClick += async (s, e) =>
                    {
                        e.Cancel = true;
                        d.IsPrimaryButtonEnabled = false;
                        try
                        {
                            _ = PluginHandler.InstallDefaultProtocolsAsync();
                        }
                        catch { }
                        d.IsPrimaryButtonEnabled = true;
                        try { d.Hide(); } catch { }
                        try
                        {
                            var data = OpenNEL_WinUI.Manager.SettingManager.Instance.Get();
                            OpenNEL_WinUI.Manager.SettingManager.Instance.Update(data);
                        }
                        catch { }
                    };
                    d.Closed += (s, e) =>
                    {
                        try
                        {
                            var data = OpenNEL_WinUI.Manager.SettingManager.Instance.Get();
                            OpenNEL_WinUI.Manager.SettingManager.Instance.Update(data);
                        }
                        catch { }
                    };
                    await d.ShowAsync();
                }
            }
            catch { }
        }

        private void AuthNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (LoginPanel == null || RegisterPanel == null) return;
            if (args.SelectedItem is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();
                if (tag == "Login")
                {
                    LoginPanel.Visibility = Visibility.Visible;
                    LoginPanel.Opacity = 1;
                    RegisterPanel.Visibility = Visibility.Collapsed;
                    RegisterPanel.Opacity = 0;
                }
                else if (tag == "Register")
                {
                    LoginPanel.Visibility = Visibility.Collapsed;
                    LoginPanel.Opacity = 0;
                    RegisterPanel.Visibility = Visibility.Visible;
                    RegisterPanel.Opacity = 1;
                    _ = LoadCaptchaAsync();
                }
            }
        }

        private async System.Threading.Tasks.Task LoadCaptchaAsync()
        {
            try
            {
                var result = await AuthManager.Instance.GetCaptchaAsync();
                if (result.Success && !string.IsNullOrEmpty(result.ImageBase64))
                {
                    _currentCaptchaId = result.CaptchaId;
                    var bytes = Convert.FromBase64String(result.ImageBase64);
                    using var stream = new MemoryStream(bytes);
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                    CaptchaImage.Source = bitmap;
                }
            }
            catch { }
        }

        private void RefreshCaptcha_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadCaptchaAsync();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginError.Visibility = Visibility.Collapsed;
            LoginButton.IsEnabled = false;
            try
            {
                var username = LoginUsername.Text?.Trim() ?? "";
                var password = LoginPassword.Password ?? "";
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    LoginError.Text = "用户名和密码不能为空";
                    LoginError.Visibility = Visibility.Visible;
                    return;
                }
                var result = await AuthManager.Instance.LoginAsync(username, password);
                if (result.Success)
                {
                    CrcSalt.TokenProvider = () => AuthManager.Instance.Token ?? "";
                    CrcSalt.InvalidateCache();
                    _ = CrcSalt.Compute();
                    LoginOverlay.Visibility = Visibility.Collapsed;
                    NavView.Visibility = Visibility.Visible;
                }
                else
                {
                    LoginError.Text = result.Message ?? "登录失败";
                    LoginError.Visibility = Visibility.Visible;
                }
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterError.Visibility = Visibility.Collapsed;
            RegisterButton.IsEnabled = false;
            try
            {
                var username = RegisterUsername.Text?.Trim() ?? "";
                var password = RegisterPassword.Password ?? "";
                var captchaText = RegisterCaptchaText.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    RegisterError.Text = "用户名和密码不能为空";
                    RegisterError.Visibility = Visibility.Visible;
                    return;
                }
                if (string.IsNullOrEmpty(captchaText))
                {
                    RegisterError.Text = "请输入验证码";
                    RegisterError.Visibility = Visibility.Visible;
                    return;
                }
                var result = await AuthManager.Instance.RegisterAsync(username, password, _currentCaptchaId, captchaText);
                if (result.Success)
                {
                    CrcSalt.TokenProvider = () => AuthManager.Instance.Token ?? "";
                    CrcSalt.InvalidateCache();
                    _ = CrcSalt.Compute();
                    LoginOverlay.Visibility = Visibility.Collapsed;
                    NavView.Visibility = Visibility.Visible;
                }
                else
                {
                    RegisterError.Text = result.Message ?? "注册失败";
                    RegisterError.Visibility = Visibility.Visible;
                    _ = LoadCaptchaAsync();
                }
            }
            finally
            {
                RegisterButton.IsEnabled = true;
            }
        }

        
    }
}
