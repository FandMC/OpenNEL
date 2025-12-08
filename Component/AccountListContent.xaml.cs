using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using OpenNEL.Manager;
using OpenNEL_WinUI.Handlers.Login;
using Serilog;

namespace OpenNEL_WinUI
{
    public sealed partial class AccountListContent : UserControl
    {
        static bool _dialogOpen;
        public ObservableCollection<AccountModel> Accounts
        {
            get => (ObservableCollection<AccountModel>)GetValue(AccountsProperty);
            set => SetValue(AccountsProperty, value);
        }

        public static readonly DependencyProperty AccountsProperty = DependencyProperty.Register(
            nameof(Accounts), typeof(ObservableCollection<AccountModel>), typeof(AccountListContent), new PropertyMetadata(null));

        public AccountListContent()
        {
            this.InitializeComponent();
        }

        ElementTheme GetAppTheme()
        {
            var mode = UserManager.Instance != null ? OpenNEL.Manager.SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system" : "system";
            if (mode == "light") return ElementTheme.Light;
            if (mode == "dark") return ElementTheme.Dark;
            return ElementTheme.Default;
        }

        ContentDialog CreateDialog(object content, string title)
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

        void RefreshAccounts()
        {
            if (Accounts == null) return;
            Accounts.Clear();
            var users = UserManager.Instance.GetUsersNoDetails();
            foreach (var u in users.OrderBy(x => x.UserId))
            {
                Accounts.Add(new AccountModel
                {
                    EntityId = u.UserId,
                    Channel = u.Channel,
                    Status = u.Authorized ? "online" : "offline",
                    Alias = u.Alias
                });
            }
        }

        bool TryDetectSuccess(object result)
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

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AccountModel account)
            {
                account.IsLoading = true;
                try
                {
                    await System.Threading.Tasks.Task.Run(() =>
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
                                var dlg = CreateDialog(dialogContent, "输入验证码");
                                dialogContent.SetCaptcha(sidVal, urlVal);
                                if (_dialogOpen)
                                {
                                    return;
                                }
                                _dialogOpen = true;
                                dlg.Closed += (s,e) => { _dialogOpen = false; };
                                dlg.PrimaryButtonClick += async (s2, e2) =>
                                {
                                    e2.Cancel = true;
                                    dlg.IsPrimaryButtonEnabled = false;
                                    try
                                    {
                                        var sid2 = dialogContent.SessionId;
                                        var cap2 = dialogContent.CaptchaText;
                                        var r3 = await System.Threading.Tasks.Task.Run(() => new Login4399().Execute(accVal, pwdVal, sid2, cap2));
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
                    NotificationHost.ShowGlobal("账号删除成功", ToastLevel.Success);
                    RefreshAccounts();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "删除账号失败");
                }
            }
        }

        private async void AliasBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Tag is AccountModel account)
            {
                try
                {
                    UserManager.Instance.UpdateUserAlias(account.EntityId, account.Alias);
                    await UserManager.Instance.SaveUsersToDiskAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "保存描述失败");
                }
            }
        }
    }
}
