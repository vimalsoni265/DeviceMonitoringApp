using DeviceMonitoring.Core.Devices;

namespace DeviceMonitoring.UI.Models
{
    /// <summary>
    /// Represents a model for a device, providing access to the device instance and tracking its last updated
    /// timestamp.
    /// </summary>
    /// <remarks>This class is designed to encapsulate a device and its associated state, allowing for data
    /// binding and change notification.</remarks>
    /// <param name="device"></param>
    public class DeviceModel : ViewModelBase
    {
        #region Private Members

        private DateTime _lastUpdated;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the device associated with the current context.
        /// </summary>
        public IDevice Device { get; private set; }

        /// <summary>
        /// Gets the current value of the device.
        /// </summary>
        public string CurrentValue => Device.CurrentValue;

        /// <summary>
        /// Gets the current state of the device monitor as a string.
        /// </summary>
        public string DeviceMonitorState => Device.DeviceMonitorState.ToString();

        /// <summary>
        /// Gets or sets the date and time when the object was last updated.
        /// </summary>
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set
            {
                if (_lastUpdated != value)
                    SetProperty(ref _lastUpdated, value);
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="device"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public DeviceModel(IDevice device)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
            LastUpdated = DateTime.Now;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// RaisePropertyChanged:DeviceMonitorState, when device's monitor state updates.
        /// </summary>
        public void TriggerStateChanged()
        {
            RaisePropertyChanged(nameof(Device.DeviceMonitorState));
            LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// RaisePropertyChanged:CurrentValue, when device's monitor state updates.
        /// </summary>
        public void TriggerDataChanged()
        {
            RaisePropertyChanged(nameof(Device.CurrentValue));
            LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Returns a string representation of the <see cref="IDevice"/>.
        /// </summary>
        public override string ToString()
        {
            return $"{Device.Id},{Device.Name},{Device.CurrentValue},{Device.DeviceMonitorState}, {LastUpdated}";
        }

        #endregion
    }
}
