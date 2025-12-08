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
