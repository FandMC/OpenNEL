/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Microsoft.UI.Xaml;
using OpenNEL_WinUI.Manager;

namespace OpenNEL_WinUI
{
    public sealed partial class CaptchaContent : UserControl
    {
        private string _sessionId;

        public CaptchaContent()
        {
            InitializeComponent();
            try
            {
                var mode = SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
                ElementTheme t = ElementTheme.Default;
                if (mode == "light") t = ElementTheme.Light;
                else if (mode == "dark") t = ElementTheme.Dark;
                this.RequestedTheme = t;
            }
            catch { }
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
