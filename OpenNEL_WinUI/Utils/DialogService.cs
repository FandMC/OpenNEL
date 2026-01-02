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
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace OpenNEL_WinUI.Utils;

public static class DialogService
{
    public static ThemedContentDialog Create(XamlRoot root, string title, object content,
        string? primaryText = null, string? secondaryText = null, string? closeText = "关闭")
    {
        return new ThemedContentDialog
        {
            XamlRoot = root,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = title,
            Content = content,
            PrimaryButtonText = primaryText,
            SecondaryButtonText = secondaryText,
            CloseButtonText = closeText,
            DefaultButton = primaryText != null ? ContentDialogButton.Primary : ContentDialogButton.Close
        };
    }

    public static async Task<string?> ShowInputAsync(XamlRoot root, string title, string placeholder)
    {
        var input = new TextBox { PlaceholderText = placeholder, Width = 300 };
        var dlg = Create(root, title, input, "确定", null, "取消");
        var result = await dlg.ShowAsync();
        return result == ContentDialogResult.Primary ? input.Text?.Trim() : null;
    }

    public static async Task ShowErrorAsync(XamlRoot root, string message)
    {
        try
        {
            var dlg = Create(root, "错误", new TextBlock { Text = message });
            await dlg.ShowAsync();
        }
        catch (System.Exception ex) { Log.Debug(ex, "显示错误对话框失败"); }
    }

    public static async Task<bool> ShowConfirmAsync(XamlRoot root, string title, string message)
    {
        var dlg = Create(root, title, new TextBlock { Text = message }, "确定", null, "取消");
        var result = await dlg.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
