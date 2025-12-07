using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.ApplicationModel;

namespace OpenNEL_WinUI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "OpenNEL.ico");
            EnsureFileFromResource(iconPath, "OpenNEL_WinUI.Assets.OpenNEL.ico");
            if (File.Exists(iconPath))
            {
                appWindow.SetIcon(iconPath);
            }
            appWindow.Title = "Open NEL";
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            AddNavItem(Symbol.Home, "HomePage");
            AddNavItem(Symbol.World, "NetworkServerPage");
            AddNavItem(Symbol.AllApps, "PluginsPage");
            AddNavItem(Symbol.Play, "GamesPage");
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

            DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal("启动成功", ToastLevel.Success));
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

        static void EnsureFileFromResource(string path, string resourceName)
        {
            try
            {
                if (File.Exists(path)) return;
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var asm = typeof(MainWindow).Assembly;
                using var s = asm.GetManifestResourceStream(resourceName);
                if (s == null) return;
                using var fs = File.Create(path);
                s.CopyTo(fs);
            }
            catch { }
        }
    }
}
