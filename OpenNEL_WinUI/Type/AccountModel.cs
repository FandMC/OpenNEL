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

namespace OpenNEL_WinUI
{
    public class AccountModel : INotifyPropertyChanged
    {
        private string _entityId = "未分配";
        private string _channel;
        private string _status = "offline";
        private bool _isLoading;
        private string _alias = string.Empty;

        public string EntityId
        {
            get => _entityId;
            set { _entityId = value; OnPropertyChanged(); }
        }

        public string Channel
        {
            get => _channel;
            set { _channel = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string Alias
        {
            get => _alias;
            set { _alias = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
