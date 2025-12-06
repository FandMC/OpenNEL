using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace OpenNEL_WinUI
{
    public sealed partial class JoinServerContent : UserControl
    {
        public JoinServerContent()
        {
            this.InitializeComponent();
        }

        public class OptionItem
        {
            public string Label { get; set; }
            public string Value { get; set; }
        }

        public void SetAccounts(List<OptionItem> items)
        {
            AccountCombo.ItemsSource = items;
        }

        public void SetRoles(List<OptionItem> items)
        {
            RoleCombo.ItemsSource = items;
        }

        public string SelectedAccountId => AccountCombo.SelectedValue as string ?? string.Empty;
        public string SelectedRoleId => RoleCombo.SelectedValue as string ?? string.Empty;
    }
}
