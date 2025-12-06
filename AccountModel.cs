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

        // Credentials
        public string Cookie { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
