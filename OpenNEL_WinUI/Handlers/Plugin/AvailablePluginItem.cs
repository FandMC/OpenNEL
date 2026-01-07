/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public class AvailablePluginItem : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string LogoUrl { get; set; }
        public string ShortDescription { get; set; }
        public string Publisher { get; set; }
        public string DownloadUrl { get; set; }
        public string Depends { get; set; }

        private bool _isInstalled;
        public bool IsInstalled
        {
            get => _isInstalled;
            set
            {
                if (_isInstalled != value)
                {
                    _isInstalled = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
