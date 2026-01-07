/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenNEL_WinUI.Manager;
using Windows.UI;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Serilog;

namespace OpenNEL_WinUI
{
    public sealed partial class HomePage : Page
    {
        public static string PageTitle => "概括";

        private static readonly Dictionary<char, Color> McColors = new()
        {
            { '0', Color.FromArgb(255, 0, 0, 0) },
            { '1', Color.FromArgb(255, 0, 0, 170) },
            { '2', Color.FromArgb(255, 0, 170, 0) },
            { '3', Color.FromArgb(255, 0, 170, 170) },
            { '4', Color.FromArgb(255, 170, 0, 0) },
            { '5', Color.FromArgb(255, 170, 0, 170) },
            { '6', Color.FromArgb(255, 255, 170, 0) }, 
            { '7', Color.FromArgb(255, 170, 170, 170) },
            { '8', Color.FromArgb(255, 85, 85, 85) }, 
            { '9', Color.FromArgb(255, 85, 85, 255) }, 
            { 'a', Color.FromArgb(255, 85, 255, 85) }, 
            { 'b', Color.FromArgb(255, 85, 255, 255) },
            { 'c', Color.FromArgb(255, 255, 85, 85) },
            { 'd', Color.FromArgb(255, 255, 85, 255) }, 
            { 'e', Color.FromArgb(255, 255, 255, 85) }, 
            { 'f', Color.FromArgb(255, 255, 255, 255) },
        };

        public HomePage()
        {
            InitializeComponent();
            Loaded += HomePage_Loaded;
        }

        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!AuthManager.Instance.IsLoggedIn)
            {
                UsernameText.Text = "未登录";
                UserIdText.Text = "请先登录 OpenNEL 账号";
                return;
            }

            try
            {
                var result = await AuthManager.Instance.GetUserAsync();
                if (result.Success)
                {
                    UsernameText.Text = result.Username;
                    UserIdText.Text = $"ID: {result.Id}";
                    CreatedAtText.Text = FormatDateTime(result.CreatedAt);
                    LastLoginText.Text = FormatDateTime(result.LastLogin);

                    if (!string.IsNullOrEmpty(result.Rank))
                    {
                        RankBorder.Visibility = Visibility.Visible;
                        ApplyMinecraftText(RankText, result.Rank);
                    }

                    if (!string.IsNullOrEmpty(result.Avatar))
                    {
                        LoadAvatarFromBase64(result.Avatar);
                    }
                }
                else
                {
                    UsernameText.Text = "加载失败";
                    UserIdText.Text = result.Message ?? "未知错误";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "加载用户信息失败");
                UsernameText.Text = "加载失败";
                UserIdText.Text = ex.Message;
            }
        }

        private static string FormatDateTime(DateTime? dt)
        {
            if (dt == null) return "从未";
            return dt.Value.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
        }

        private static void ApplyMinecraftText(TextBlock textBlock, string text)
        {
            textBlock.Inlines.Clear();
            if (string.IsNullOrEmpty(text))
            {
                textBlock.Inlines.Add(new Run { Text = "普通用户" });
                return;
            }

            var currentColor = Color.FromArgb(255, 170, 170, 170); // 默认灰色
            var isBold = false;
            var isItalic = false;
            var currentText = string.Empty;

            void PushText()
            {
                if (string.IsNullOrEmpty(currentText)) return;
                var run = new Run
                {
                    Text = currentText,
                    Foreground = new SolidColorBrush(currentColor),
                    FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = isItalic ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal
                };
                textBlock.Inlines.Add(run);
                currentText = string.Empty;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '§' && i + 1 < text.Length)
                {
                    var code = char.ToLower(text[i + 1]);
                    PushText();

                    if (McColors.TryGetValue(code, out var color))
                    {
                        currentColor = color;
                    }
                    else if (code == 'l')
                    {
                        isBold = true;
                    }
                    else if (code == 'o')
                    {
                        isItalic = true;
                    }
                    else if (code == 'r')
                    {
                        currentColor = Color.FromArgb(255, 170, 170, 170);
                        isBold = false;
                        isItalic = false;
                    }
                    i++;
                }
                else
                {
                    currentText += text[i];
                }
            }
            PushText();

            if (textBlock.Inlines.Count == 0)
            {
                textBlock.Inlines.Add(new Run { Text = "普通用户" });
            }
        }

        private async void WebKeyButton_Click(object sender, RoutedEventArgs e)
        {
            WebKeyButton.IsEnabled = false;
            try
            {
                var result = await AuthManager.Instance.GenerateWebKeyAsync();
                if (result.Success)
                {
                    await ShowWebKeyDialogAsync(result.Key);
                }
                else
                {
                    NotificationHost.ShowGlobal(result.Message ?? "生成失败", ToastLevel.Error);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "生成网页密钥失败");
                NotificationHost.ShowGlobal("生成失败", ToastLevel.Error);
            }
            finally
            {
                WebKeyButton.IsEnabled = true;
            }
        }

        private async void AvatarButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");
            picker.FileTypeFilter.Add(".bmp");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            AvatarButton.IsEnabled = false;
            try
            {
                using var stream = await file.OpenStreamForReadAsync();
                var bytes = new byte[stream.Length];
                await stream.ReadAsync(bytes, 0, bytes.Length);
                var base64 = Convert.ToBase64String(bytes);

                var result = await AuthManager.Instance.UpdateAvatarAsync(base64);
                if (result.Success)
                {
                    LoadAvatarFromBase64(base64);
                    NotificationHost.ShowGlobal("头像已更新", ToastLevel.Success);
                }
                else
                {
                    NotificationHost.ShowGlobal(result.Message ?? "更新失败", ToastLevel.Error);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新头像失败");
                NotificationHost.ShowGlobal("更新头像失败", ToastLevel.Error);
            }
            finally
            {
                AvatarButton.IsEnabled = true;
            }
        }

        private void LoadAvatarFromBase64(string base64)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64);
                using var ms = new MemoryStream(bytes);
                var bitmap = new BitmapImage
                {
                    DecodePixelWidth = 112,
                    DecodePixelHeight = 112
                };
                bitmap.SetSource(ms.AsRandomAccessStream());
                AvatarImage.Source = bitmap;
                AvatarImageBorder.Visibility = Visibility.Visible;
                AvatarBorder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "加载头像失败");
            }
        }

        private async System.Threading.Tasks.Task ShowWebKeyDialogAsync(string key)
        {
            var keyBox = new TextBox
            {
                Text = key,
                IsReadOnly = true,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var copyButton = new Button
            {
                Content = "复制密钥",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 0)
            };
            copyButton.Click += (s, e) =>
            {
                var dp = new DataPackage();
                dp.SetText(key);
                Clipboard.SetContent(dp);
                NotificationHost.ShowGlobal("密钥已复制", ToastLevel.Success);
            };

            var content = new StackPanel
            {
                Children =
                {
                    keyBox,
                    copyButton,
                    new TextBlock
                    {
                        Text = "该密钥 5 分钟后失效，仅可使用一次",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Colors.Gray),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 8, 0, 0)
                    }
                }
            };

            var dialog = new ThemedContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "网页登录密钥",
                Content = content,
                CloseButtonText = "关闭"
            };

            await dialog.ShowAsync();
        }
    }
}
