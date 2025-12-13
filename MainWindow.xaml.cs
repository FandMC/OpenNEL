using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using OpenNEL.Utils;

namespace OpenNEL_WinUI
{
    public sealed partial class MainWindow : Window
    {
        static MainWindow? _instance;
        AppWindow? _appWindow;
        string _currentBackdrop = "";
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

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            AddNavItem(Symbol.Home, "HomePage");
            AddNavItem(Symbol.World, "NetworkServerPage");
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

            //DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal("启动成功", ToastLevel.Success));
            //DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal("启动成功", ToastLevel.Success));
            //DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal("启动成功", ToastLevel.Success));
            //DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal("启动成功", ToastLevel.Success));
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
                var mode = OpenNEL.Manager.SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
                ElementTheme t = ElementTheme.Default;
                if (mode == "light") t = ElementTheme.Light;
                else if (mode == "dark") t = ElementTheme.Dark;
                RootGrid.RequestedTheme = t;
                NavView.RequestedTheme = t;
                ContentFrame.RequestedTheme = t;
                var actual = t == ElementTheme.Default ? RootGrid.ActualTheme : t;
                UpdateTitleBarColors(actual);

                var bd = OpenNEL.Manager.SettingManager.Instance.Get().Backdrop?.Trim().ToLowerInvariant() ?? "mica";
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
    }
}
