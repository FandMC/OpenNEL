using Microsoft.UI.Xaml.Controls;
using OpenNEL_WinUI.Manager;

namespace OpenNEL_WinUI;

public class ThemedContentDialog : ContentDialog
{
    public ThemedContentDialog()
    {
        RequestedTheme = SettingManager.Instance.GetAppTheme();
    }
}
