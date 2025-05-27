using System.ComponentModel;
using System.Windows.Threading;

namespace DeviceMonitoring.UI.Converters
{
    /// <summary>
    /// Helper class to support TimeAgo functionality in the UI.
    /// It is UI Responsiveness, MVVM Friendly and suitable for 10K+ Devices.  
    /// </summary>
    public class TimeProvider : INotifyPropertyChanged
    {
        private static readonly TimeProvider _instance = new();
        public static TimeProvider Instance => _instance;

        private DispatcherTimer _timer;

        private TimeProvider()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            _timer.Tick += (s, e) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Now)));
            _timer.Start();
        }

        public DateTime Now => DateTime.Now;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
