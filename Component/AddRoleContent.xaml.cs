using Microsoft.UI.Xaml.Controls;
using System;

namespace OpenNEL_WinUI
{
    public sealed partial class AddRoleContent : UserControl
    {
        private readonly Random _random = new Random();

        public AddRoleContent()
        {
            this.InitializeComponent();
        }

        public string RoleName => RoleNameInput.Text;

        private void RandomBtn_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var len = _random.Next(6, 9);
            var start = 0x4E00;
            var end = 0x9FA5;
            var s = new char[len];
            for (var i = 0; i < len; i++)
            {
                s[i] = (char)_random.Next(start, end + 1);
            }
            RoleNameInput.Text = new string(s);
        }
    }
}
