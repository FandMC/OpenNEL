using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using OpenNEL_WinUI.Handlers.Login;
using Windows.ApplicationModel.DataTransfer;

namespace OpenNEL_WinUI
{
    public sealed partial class AddAccountContent : UserControl
    {
        private string _pc4399SessionId;
        public AddAccountContent()
        {
            this.InitializeComponent();
        }

        public string SelectedType => (AccountTypePivot.SelectedItem as PivotItem)?.Header?.ToString();

        public string CookieText => CookieInput.Text;

        public string Pc4399User => Pc4399Username.Text;
        public string Pc4399Pass => Pc4399Password.Password;
        public string Pc4399Captcha => CaptchaInput.Text;
        public string Pc4399SessionId => _pc4399SessionId;

        public string NeteaseMail => NeteaseEmail.Text;
        public string NeteasePass => NeteasePassword.Password;

        private async void GetFreeAccount_Click(object sender, RoutedEventArgs e)
        {
            FreeAccountMsg.Text = string.Empty;
            FreeAccountMsg.Visibility = Visibility.Visible;
            try
            {
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
            Pc4399Username.Text = account ?? string.Empty;
            Pc4399Password.Password = password ?? string.Empty;
            CaptchaPanel.Visibility = Visibility.Visible;
            CaptchaInput.Text = string.Empty;
            try
            {
                if (!string.IsNullOrWhiteSpace(captchaUrl))
                {
                    CaptchaImage.Source = new BitmapImage(new Uri(captchaUrl));
                }
            }
            catch
            {
            }
        }

        private async void PasteAndLoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 从剪贴板获取文本内容
                var clipboardContent = Clipboard.GetContent();
                var clipboardText = await clipboardContent.GetTextAsync();

                // 将获取到的文本设置到Cookie输入框中
                CookieInput.Text = clipboardText;
            }
            catch (Exception ex)
            {

            }
        }
    }
}
