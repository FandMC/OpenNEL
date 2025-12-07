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

        private bool _isWaitingRestart;
        public bool IsWaitingRestart
        {
            get => _isWaitingRestart;
            set
            {
                if (_isWaitingRestart != value)
                {
                    _isWaitingRestart = value;
                    OnPropertyChanged();
                }
            }
        }

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
