using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using System.Linq;
using OpenNEL_WinUI.Handlers.Login;
using OpenNEL.Manager;
using Microsoft.UI.Xaml.Media.Imaging;

namespace OpenNEL_WinUI
{
    public sealed partial class AddAccountContent : UserControl
    {
        public event Action AutoLoginSucceeded;
        public event Action ConfirmRequested;
        public event Action CancelRequested;
        private string _pc4399SessionId;
        public AddAccountContent()
        {
            this.InitializeComponent();
            var mode = SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
            ElementTheme t = ElementTheme.Default;
            if (mode == "light") t = ElementTheme.Light;
            else if (mode == "dark") t = ElementTheme.Dark;
            this.RequestedTheme = t;
        }

        public string SelectedType => (AccountTypePivot.SelectedItem as PivotItem)?.Header?.ToString();

        public string CookieText => CookieInput.Text;
        
        public string Pc4399User => Pc4399Username.Text;
        public string Pc4399Pass => Pc4399Password.Password;
        private string _pc4399CaptchaUrl;
        public string Pc4399SessionId => _pc4399SessionId;
        public string Pc4399CaptchaUrl => _pc4399CaptchaUrl;
        public string Pc4399Captcha => Pc4399CaptchaInput?.Text ?? string.Empty;
        
        public string NeteaseMail => NeteaseEmail.Text;
        public string NeteasePass => NeteasePassword.Password;

        private async void GetFreeAccount_Click(object sender, RoutedEventArgs e)
        {
            FreeAccountMsg.Text = string.Empty;
            FreeAccountMsg.Visibility = Visibility.Visible;
            try
            {
                DispatcherQueue.TryEnqueue(() => NotificationHost.ShowGlobal("尝试获取中", ToastLevel.Success));
                var r = await new GetFreeAccount().Execute();
                if (r != null && r.Length >= 2)
                {
                    var payload = r[1];
                    var tProp = payload.GetType().GetProperty("type");
                    var tVal = tProp != null ? tProp.GetValue(payload) as string : null;
                    if (tVal == "get_free_account_result")
                    {
                        var sProp = payload.GetType().GetProperty("success");
                        var sVal = sProp != null && (bool)(sProp.GetValue(payload) ?? false);
                        if (sVal)
                        {
                            var ckProp = payload.GetType().GetProperty("cookie");
                            var ckErrProp = payload.GetType().GetProperty("cookieError");
                            var ckVal = ckProp?.GetValue(payload) as string ?? string.Empty;
                            var ckErr = ckErrProp?.GetValue(payload) as string ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(ckVal) && string.IsNullOrWhiteSpace(ckErr))
                            {
                                try
                                {
                                    var r2 = await Task.Run(() => new CookieLogin().Execute(ckVal));
                                    var tp = r2?.GetType().GetProperty("type");
                                    var tv = tp != null ? tp.GetValue(r2) as string : null;
                                    if (!string.Equals(tv, "login_error"))
                                    {
                                        NotificationHost.ShowGlobal("账号添加成功", ToastLevel.Success);
                                        FreeAccountMsg.Text = "已使用Cookie自动登录";
                                        AutoLoginSucceeded?.Invoke();
                                        return;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    FreeAccountMsg.Text = ex.Message;
                                }
                            }

                            var uProp = payload.GetType().GetProperty("username");
                            var pProp = payload.GetType().GetProperty("password");
                            var uVal = uProp?.GetValue(payload) as string ?? string.Empty;
                            var pVal = pProp?.GetValue(payload) as string ?? string.Empty;
                            Pc4399Username.Text = uVal;
                            Pc4399Password.Password = pVal;
                            FreeAccountMsg.Text = "获取成功！已自动填充。";
                        }
                        else
                        {
                            var mProp = payload.GetType().GetProperty("message");
                            var mVal = mProp?.GetValue(payload) as string ?? string.Empty;
                            FreeAccountMsg.Text = mVal.Length == 0 ? "获取失败" : mVal;
                        }
                    }
                    else if (tVal == "get_free_account_requires_captcha")
                    {
                        var sidProp = payload.GetType().GetProperty("captchaId");
                        var urlProp = payload.GetType().GetProperty("captchaImageUrl");
                        var accProp = payload.GetType().GetProperty("username");
                        var pwdProp = payload.GetType().GetProperty("password");
                        var sidVal = sidProp?.GetValue(payload) as string ?? string.Empty;
                        var urlVal = urlProp?.GetValue(payload) as string ?? string.Empty;
                        var accVal = accProp?.GetValue(payload) as string ?? string.Empty;
                        var pwdVal = pwdProp?.GetValue(payload) as string ?? string.Empty;
                        SetCaptchaFor4399(sidVal, urlVal, accVal, pwdVal);
                        FreeAccountMsg.Text = "需要验证码";
                    }
                }
            }
            catch (Exception ex)
            {
                FreeAccountMsg.Text = ex.Message;
            }
        }

        public void SetCaptchaFor4399(string sessionId, string captchaUrl, string account, string password)
        {
            _pc4399SessionId = sessionId ?? string.Empty;
            _pc4399CaptchaUrl = captchaUrl ?? string.Empty;
            Pc4399Username.Text = account ?? string.Empty;
            Pc4399Password.Password = password ?? string.Empty;
            Pc4399CaptchaPanel.Visibility = string.IsNullOrWhiteSpace(_pc4399CaptchaUrl) ? Visibility.Collapsed : Visibility.Visible;
            try
            {
                if (!string.IsNullOrWhiteSpace(_pc4399CaptchaUrl))
                {
                    Pc4399CaptchaImage.Source = new BitmapImage(new Uri(_pc4399CaptchaUrl));
                }
            }
            catch { }
        }

        public bool TryDetectSuccess(object result)
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

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try { ConfirmRequested?.Invoke(); } catch { }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try { CancelRequested?.Invoke(); } catch { }
        }
    }
}
