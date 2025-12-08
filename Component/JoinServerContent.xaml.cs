using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System;
using Microsoft.UI.Xaml;
using OpenNEL.Manager;

namespace OpenNEL_WinUI
{
    public sealed partial class JoinServerContent : UserControl
    {
        public JoinServerContent()
        {
            this.InitializeComponent();
            try
            {
                var mode = SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
                ElementTheme t = ElementTheme.Default;
                if (mode == "light") t = ElementTheme.Light;
                else if (mode == "dark") t = ElementTheme.Dark;
                this.RequestedTheme = t;
            }
            catch { }
        }

        public class OptionItem
        {
            public string Label { get; set; }
            public string Value { get; set; }
        }

        public void SetAccounts(List<OptionItem> items)
        {
            AccountCombo.ItemsSource = items;
            if (AccountCombo.SelectedIndex < 0 && items != null && items.Count > 0)
                AccountCombo.SelectedIndex = 0;
        }

        public void SetRoles(List<OptionItem> items)
        {
            RoleCombo.ItemsSource = items;
            if (RoleCombo.SelectedIndex < 0 && items != null && items.Count > 0)
                RoleCombo.SelectedIndex = 0;
        }

        public string SelectedAccountId => AccountCombo.SelectedValue as string ?? string.Empty;
        public string SelectedRoleId => RoleCombo.SelectedValue as string ?? string.Empty;

        public event Action<string> AccountChanged;

        private void AccountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var id = SelectedAccountId;
            AccountChanged?.Invoke(id);
        }
    }
}
