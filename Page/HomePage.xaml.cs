using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using OpenNEL_WinUI.Handlers.Login;
using OpenNEL.Manager;
using OpenNEL.Entities.Web;
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
            this.InitializeComponent();
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
            var d = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = title,
                Content = content,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };
            d.RequestedTheme = GetAppTheme();
            return d;
        }

        void ShowInfo(string msg, InfoBarSeverity sev)
        {
            InfoBar.Message = msg;
            InfoBar.Severity = sev;
            InfoBar.IsOpen = true;
        }

        private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowAddAccountDialogAsync();
        }

        private async Task ShowAddAccountDialogAsync()
        {
            var dialogContent = new AddAccountContent();
            var dialog = CreateDialog(dialogContent, "添加账号");
            dialogContent.AutoLoginSucceeded += () =>
            {
                try { dialog.Hide(); } catch { }
                RefreshAccounts();
            };
            dialog.PrimaryButtonText = string.Empty;
            dialog.CloseButtonText = string.Empty;
            dialogContent.CancelRequested += () =>
            {
                try { dialog.Hide(); } catch { }
            };
            dialogContent.ConfirmRequested += async () =>
            {
                await ProcessAddAccountAsync(dialogContent, dialog);
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
                        ShowInfo("账号添加成功", InfoBarSeverity.Success);
                        dialog.Hide();
                    }
                }
                else if (type == "PC4399")
                {
                    var result = await ProcessPc4399Async(dialogContent, dialog);
                    RefreshAccounts();
                    if (result.succ && !result.parentHidden)
                    {
                        ShowInfo("账号添加成功", InfoBarSeverity.Success);
                        dialog.Hide();
                    }
                }
                else if (type == "网易邮箱")
                {
                    var succ = await ProcessX19Async(dialogContent);
                    RefreshAccounts();
                    if (succ)
                    {
                        ShowInfo("账号添加成功", InfoBarSeverity.Success);
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
            var r = await Task.Run(() => new CookieLogin().Execute(cookie));
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
                DispatcherQueue.TryEnqueue(() => ShowInfo("需要输入验证码", InfoBarSeverity.Warning));
                var succ2 = await ShowCaptchaDialogFor4399Async(dialogContent, acc, pwd, sidExisting, dialogContent.Pc4399CaptchaUrl, dialog);
                return (succ2, succ2);
            }
            object r = await Task.Run(() => new Login4399().Execute(acc, pwd));
            var tProp = r.GetType().GetProperty("type");
            var tVal = tProp != null ? tProp.GetValue(r) as string : null;
            if (tVal == "captcha_required")
            {
                var accProp = r.GetType().GetProperty("account");
                var pwdProp = r.GetType().GetProperty("password");
                var sidProp = r.GetType().GetProperty("sessionId");
                var urlProp = r.GetType().GetProperty("captchaUrl");
                var accVal = accProp?.GetValue(r) as string ?? string.Empty;
                var pwdVal = pwdProp?.GetValue(r) as string ?? string.Empty;
                var sidVal = sidProp?.GetValue(r) as string ?? string.Empty;
                var urlVal = urlProp?.GetValue(r) as string ?? string.Empty;
                DispatcherQueue.TryEnqueue(() => ShowInfo("需要输入验证码", InfoBarSeverity.Warning));
                var succ2 = await ShowCaptchaDialogFor4399Async(dialogContent, accVal, pwdVal, sidVal, urlVal, dialog);
                return (succ2, succ2);
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
                        ShowInfo("账号添加成功", InfoBarSeverity.Success);
                        success = true;
                        dlg2.Hide();
                        parentDialog.Hide();
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

        private async Task<bool> ProcessX19Async(AddAccountContent dialogContent)
        {
            var email = dialogContent.NeteaseMail;
            var pwd = dialogContent.NeteasePass;
            var r = await Task.Run(() => new LoginX19().Execute(email, pwd));
            var succ = dialogContent.TryDetectSuccess(r);
            return succ;
        }

        

        private void RefreshAccounts()
        {
            Accounts.Clear();
            var users = UserManager.Instance.GetUsersNoDetails();
            foreach (var u in users.OrderBy(x => x.UserId))
            {
                Accounts.Add(new AccountModel
                {
                    EntityId = u.UserId,
                    Channel = u.Channel,
                    Status = u.Authorized ? "online" : "offline"
                });
            }
        }

        
    }
}
