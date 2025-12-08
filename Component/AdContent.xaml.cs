using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using Serilog;

namespace OpenNEL_WinUI
{
    public sealed partial class AdContent : UserControl
    {
        public AdContent()
        {
            this.InitializeComponent();
        }

        private void OpenOfficialSiteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://freecookie.studio/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "打开网站失败");
            }
        }
    }
}
