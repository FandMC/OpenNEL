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
    public class PluginViewModel : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Status { get; set; }
        public string LatestVersion { get; set; }
        public string DownloadUrl { get; set; }

        private bool _needUpdate;
        public bool NeedUpdate
        {
            get => _needUpdate;
            set
            {
                if (_needUpdate != value)
                {
                    _needUpdate = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
