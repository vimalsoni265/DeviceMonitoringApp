using DeviceMonitoring.Core.Devices;

namespace DeviceMonitoring.Services.NewFolder
{
    public class DeviceEntry : IDisposable
    {
        private bool _isMonitoring;
        private bool _disposed = false;

        public DeviceEntry(IDevice device, int interval, DateTime lastUpdated)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
            Interval = interval;
            LastUpdated = lastUpdated;
        }
        /// <summary>
        /// Gets or sets the device associated with the current operation.  
        /// </summary>
        public IDevice Device { get; set; }

        /// <summary>
        /// Gets or sets the time interval for the operation.
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the last update.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        private EventHandler<DeviceDataChangedEventArgs>? m_dataChangedHandler { get; set; }
        private EventHandler<DeviceMonitorStateChangedEventArgs>? m_stateChangedHandler { get; set; }

        public void SubscribeToStateChanged(EventHandler<DeviceMonitorStateChangedEventArgs> handler)
        {
            if (handler != null)
                m_stateChangedHandler += handler;
        }

        public void SubscribeToDataChanged(EventHandler<DeviceDataChangedEventArgs> handler)
        {
            if (handler != null)
                m_dataChangedHandler += handler;
        }

        public void UnsubscribeFromDataChanged(EventHandler<DeviceDataChangedEventArgs> handler)
        {
            if (handler != null)
                m_dataChangedHandler -= handler;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            // Unsubscribe from events
            if (m_dataChangedHandler != null)
            {
                Device.DeviceDataChanged -= m_dataChangedHandler;
                m_dataChangedHandler = null;
            }
            if (m_stateChangedHandler != null)
            {
                Device.DeviceStateChanged -= m_stateChangedHandler;
                m_stateChangedHandler = null;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
