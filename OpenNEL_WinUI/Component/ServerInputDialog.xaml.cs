using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OpenNEL_WinUI.Component;

public sealed partial class ServerInputDialog : ThemedContentDialog
{
    public ServerInputDialog()
    {
        this.InitializeComponent();
    }

    public string ServerId => ServerIdInput?.Text ?? string.Empty;
}