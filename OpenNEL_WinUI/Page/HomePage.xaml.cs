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
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using OpenNEL_WinUI.Handlers.Login;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.Entities.Web;
using System.Linq;
using Serilog;

namespace OpenNEL_WinUI
{
    public sealed partial class HomePage : Page
    {
        public static string PageTitle => "概括";
        public ObservableCollection<AccountModel> Accounts { get; } = new ObservableCollection<AccountModel>();

        public HomePage()
        {
            InitializeComponent();
            RefreshAccounts();
            UserManager.Instance.UsersReadFromDisk += () => DispatcherQueue.TryEnqueue(RefreshAccounts);
        }

        private ElementTheme GetAppTheme()
        {
            var mode = SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
            if (mode == "light") return ElementTheme.Light;
            if (mode == "dark") return ElementTheme.Dark;
            return ElementTheme.Default;
        }

        private ContentDialog CreateDialog(object content, string title)
        {
            var d = new ThemedContentDialog
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = title,
                Content = content,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };
            return d;
        }

        private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowAddAccountDialogAsync();
        }

        private async Task ShowAddAccountDialogAsync()
        {
            var dialogContent = new AddAccountContent();
            var dialog = CreateDialog(dialogContent, "添加账号");
            dialogContent.CaptchaInputRequested = async (url) =>
            {
                try { dialog.Hide(); } catch { }
                var content = new CaptchaContent();
                var sid = Guid.NewGuid().ToString("N");
                var dlg2 = CreateDialog(content, "输入验证码");
                content.SetCaptcha(sid, url);
                string cap = string.Empty;
                dlg2.PrimaryButtonClick += (s2, e2) =>
                {
                    e2.Cancel = true;
                    cap = content.CaptchaText;
                    try { dlg2.Hide(); } catch { }
                };
                await dlg2.ShowAsync();
                await dialog.ShowAsync();
                return cap ?? string.Empty;
            };
            dialogContent.AutoLoginSucceeded += () =>
            {
                try { dialog.Hide(); } catch { }
                RefreshAccounts();
            };
            dialogContent.CaptchaRequired += async (sid, url, acc, pwd) =>
            {
                try { dialog.Hide(); } catch { }
                await ShowCaptchaDialogFor4399Async(dialogContent, acc, pwd, sid, url, dialog);
            };
            dialog.PrimaryButtonClick += async (s, e) =>
            {
                e.Cancel = true;
                dialog.IsPrimaryButtonEnabled = false;
                try
                {
                    await ProcessAddAccountAsync(dialogContent, dialog);
                }
                finally
                {
                    dialog.IsPrimaryButtonEnabled = true;
                }
            };
            await dialog.ShowAsync();
        }

        private async Task ProcessAddAccountAsync(AddAccountContent dialogContent, ContentDialog dialog)
        {
            var type = dialogContent.SelectedType;
            try
            {
                if (type == "Cookie")
                {
                    var succ = await ProcessCookieAsync(dialogContent);
                    RefreshAccounts();
                    if (succ)
                    {
                        NotificationHost.ShowGlobal("账号添加成功", ToastLevel.Success);
                        dialog.Hide();
                    }
                }
                else if (type == "PC4399")
                {
                    var result = await ProcessPc4399Async(dialogContent, dialog);
                    RefreshAccounts();
                    if (result.succ && !result.parentHidden)
                    {
                        NotificationHost.ShowGlobal("账号添加成功", ToastLevel.Success);
                        dialog.Hide();
                    }
                }
                else if (type == "网易邮箱")
                {
                    var succ = await ProcessNeteaseAsync(dialogContent);
                    RefreshAccounts();
                    if (succ)
                    {
                        NotificationHost.ShowGlobal("账号添加成功", ToastLevel.Success);
                        dialog.Hide();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "添加账号失败");
            }
        }

        private async Task<bool> ProcessCookieAsync(AddAccountContent dialogContent)
        {
            var cookie = dialogContent.CookieText;
            var r = await Task.Run(() => new LoginCookie().Execute(cookie));
            var succ = dialogContent.TryDetectSuccess(r);
            return succ;
        }

        private async Task<bool> ProcessNeteaseAsync(AddAccountContent dialogContent)
        {
            var email = dialogContent.NeteaseMail;
            var password = dialogContent.NeteasePass;
            var r = await Task.Run(() => new LoginX19().Execute(email, password));
            var succ = dialogContent.TryDetectSuccess(r);
            return succ;
        }

        private async Task<(bool succ, bool parentHidden)> ProcessPc4399Async(AddAccountContent dialogContent, ContentDialog dialog)
        {
            var acc = dialogContent.Pc4399User;
            var pwd = dialogContent.Pc4399Pass;
            var sidExisting = dialogContent.Pc4399SessionId;
            if (!string.IsNullOrWhiteSpace(sidExisting))
            {
                NotificationHost.ShowGlobal("需要输入验证码", ToastLevel.Warning);
                try { dialog.Hide(); } catch { }
                var succ0 = await ShowCaptchaDialogFor4399Async(dialogContent, acc, pwd, sidExisting, dialogContent.Pc4399CaptchaUrl, dialog);
                return (succ0, true);
            }
            object r = await Task.Run(() => new Login4399().Execute(acc, pwd));
            var tProp = r.GetType().GetProperty("type");
            var tVal = tProp != null ? tProp.GetValue(r) as string : null;
            if (string.Equals(tVal, "login_error", StringComparison.OrdinalIgnoreCase) || string.Equals(tVal, "login_4399_error", StringComparison.OrdinalIgnoreCase))
            {
                var mProp = r.GetType().GetProperty("message");
                var mVal = mProp?.GetValue(r) as string ?? string.Empty;
                var lower = mVal.Trim().ToLowerInvariant();
                if (lower.Contains("captcha required") || lower.Contains("captcha") || lower.Contains("验证码"))
                {
                    var sidVal = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 8);
                    var urlVal = "https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + sidVal;
                    NotificationHost.ShowGlobal("需要输入验证码", ToastLevel.Warning);try { dialog.Hide(); } catch { }
                    var succE = await ShowCaptchaDialogFor4399Async(dialogContent, acc, pwd, sidVal, urlVal, dialog);
                    return (succE, true);
                }
            }
            var succ = dialogContent.TryDetectSuccess(r);
            return (succ, false);
        }

        private async Task<bool> ShowCaptchaDialogFor4399Async(AddAccountContent dialogContent, string acc, string pwd, string sid, string url, ContentDialog parentDialog)
        {
            var dialogContent2 = new CaptchaContent();
            var dlg2 = CreateDialog(dialogContent2, "输入验证码");
            dialogContent2.SetCaptcha(sid, url);
            bool success = false;
            dlg2.PrimaryButtonClick += async (s2, e2) =>
            {
                e2.Cancel = true;
                dlg2.IsPrimaryButtonEnabled = false;
                try
                {
                    var cap2 = dialogContent2.CaptchaText;
                    var r2 = await Task.Run(() => new Login4399().Execute(acc, pwd, sid, cap2));
                    var succ2 = dialogContent.TryDetectSuccess(r2);
                    if (succ2)
                    {
                        NotificationHost.ShowGlobal("账号添加成功", ToastLevel.Success);
                        success = true;
                        try { dlg2.Hide(); } catch { }
                        try { parentDialog.Hide(); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "验证码登录失败");
                }
                dlg2.IsPrimaryButtonEnabled = true;
            };
            await dlg2.ShowAsync();
            return success;
        }

        private void RefreshAccounts()
        {
            Accounts.Clear();
            foreach (var item in GetAccount.GetAccountList().OrderBy(x => x.EntityId))
            {
                Accounts.Add(new AccountModel
                {
                    EntityId = item.EntityId,
                    Channel = item.Channel,
                    Status = item.Status
                });
            }
        }

        
    }
}
