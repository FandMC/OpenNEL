using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace OpenNEL_WinUI
{
    public sealed partial class CaptchaContent : UserControl
    {
        private string _sessionId;

        public CaptchaContent()
        {
            this.InitializeComponent();
        }

        public string CaptchaText => CaptchaInput.Text;

        public string SessionId => _sessionId;

        public void SetCaptcha(string sessionId, string captchaUrl)
        {
            _sessionId = sessionId ?? string.Empty;
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
    }
}
