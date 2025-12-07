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
        }

        private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            var dialogContent = new AddAccountContent();
            
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "添加账号",
                Content = dialogContent,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };
            dialog.PrimaryButtonClick += async (s, e2) =>
            {
                e2.Cancel = true;
                dialog.IsPrimaryButtonEnabled = false;
                string type = dialogContent.SelectedType;
                try
                {
                    if (type == "Cookie")
                    {
                        var cookie = dialogContent.CookieText;
                        var r = await Task.Run(() => new CookieLogin().Execute(cookie));
                        var succ = TryDetectSuccess(r);
                        RefreshAccounts();
                        if (succ)
                        {
                            NotificationHost.ShowGlobal("账号添加成功", ToastLevel.Success);
                            dialog.Hide();
                        }
                    }
                    else if (type == "PC4399")
                    {
                        var acc = dialogContent.Pc4399User;
                        var pwd = dialogContent.Pc4399Pass;
                        var cap = dialogContent.Pc4399Captcha;
                        var sid = dialogContent.Pc4399SessionId;
                        object r = await Task.Run(() =>
                        {
                            if (!string.IsNullOrWhiteSpace(cap) && !string.IsNullOrWhiteSpace(sid))
                            {
                                return new Login4399().Execute(acc, pwd, sid, cap);
                            }
                            else
                            {
                                return new Login4399().Execute(acc, pwd);
                            }
                        });
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
                            dialogContent.SetCaptchaFor4399(sidVal, urlVal, accVal, pwdVal);
                            DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal("需要输入验证码", ToastLevel.Warning));
                            dialog.IsPrimaryButtonEnabled = true;
                            return;
                        }
                        var succ = TryDetectSuccess(r);
                        RefreshAccounts();
                        if (succ)
                        {
                            NotificationHost.ShowGlobal("账号添加成功", ToastLevel.Success);
                            dialog.Hide();
                        }
                        else dialog.IsPrimaryButtonEnabled = true;
                    }
                    else if (type == "网易邮箱")
                    {
                        var email = dialogContent.NeteaseMail;
                        var pwd = dialogContent.NeteasePass;
                        var r = await Task.Run(() => new LoginX19().Execute(email, pwd));
                        var succ = TryDetectSuccess(r);
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
                dialog.IsPrimaryButtonEnabled = true;
            };

            await dialog.ShowAsync();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AccountModel account)
            {
                account.IsLoading = true;
                try
                {
                    await Task.Run(() =>
                    {
                        var r = new ActivateAccount().Execute(account.EntityId);
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
                            DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal("需要输入验证码", ToastLevel.Warning));
                            DispatcherQueue.TryEnqueue(async () =>
                            {
                                var dialogContent = new CaptchaContent();
                                var dlg = new ContentDialog
                                {
                                    XamlRoot = this.XamlRoot,
                                    Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                                    Title = "输入验证码",
                                    Content = dialogContent,
                                    PrimaryButtonText = "确定",
                                    CloseButtonText = "取消",
                                    DefaultButton = ContentDialogButton.Primary
                                };
                                dialogContent.SetCaptcha(sidVal, urlVal);
                                dlg.PrimaryButtonClick += async (s2, e2) =>
                                {
                                    e2.Cancel = true;
                                    dlg.IsPrimaryButtonEnabled = false;
                                    try
                                    {
                                        var sid2 = dialogContent.SessionId;
                                        var cap2 = dialogContent.CaptchaText;
                                        var r3 = await Task.Run(() => new Login4399().Execute(accVal, pwdVal, sid2, cap2));
                                        var succ = TryDetectSuccess(r3);
                                        RefreshAccounts();
                                        if (succ) dlg.Hide();
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex, "验证码登录失败");
                                    }
                                    dlg.IsPrimaryButtonEnabled = true;
                                };
                                await dlg.ShowAsync();
                            });
                        }
                        else
                        {
                            DispatcherQueue.TryEnqueue(RefreshAccounts);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "登录失败");
                }
                account.IsLoading = false;
            }
        }

        private void DeleteAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AccountModel account)
            {
                try
                {
                    var r = new DeleteAccount().Execute(account.EntityId);
                    RefreshAccounts();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "删除账号失败");
                }
            }
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

        private bool TryDetectSuccess(object result)
        {
            if (result == null) return false;
            var tProp = result.GetType().GetProperty("type");
            if (tProp != null)
            {
                var tVal = tProp.GetValue(result) as string;
                if (string.Equals(tVal, "login_error", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(tVal, "login_4399_error", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(tVal, "captcha_required", StringComparison.OrdinalIgnoreCase)) return false;
            }
            if (result is System.Collections.IEnumerable en)
            {
                foreach (var item in en)
                {
                    var p = item?.GetType().GetProperty("type");
                    var v = p != null ? p.GetValue(item) as string : null;
                    if (string.Equals(v, "Success_login", StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            var users = UserManager.Instance.GetUsersNoDetails();
            if (users.Any(u => u.Authorized)) return true;
            return false;
        }
    }
}
