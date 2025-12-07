using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

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
            
            // 如果只有一个账号，则自动选择
            if (items.Count == 1)
            {
                AccountCombo.SelectedIndex = 0;
            }
        }

        public void SetRoles(List<OptionItem> items)
        {
            RoleCombo.ItemsSource = items;
            
            // 如果只有一个角色，则自动选择
            if (items.Count == 1)
            {
                RoleCombo.SelectedIndex = 0;
            }
        }

        public string SelectedAccountId => AccountCombo.SelectedValue as string ?? string.Empty;
        public string SelectedRoleId => RoleCombo.SelectedValue as string ?? string.Empty;
    }
}