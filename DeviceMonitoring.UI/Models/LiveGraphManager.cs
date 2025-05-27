using DeviceMonitoring.Core.Devices;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;

namespace DeviceMonitoring.UI.Models
{
    public class LiveGraphManager : ViewModelBase
    {
        #region Private Members

        private ObservableCollection<DateTimePoint> _values = [];
        private DateTimeAxis _customAxis;
        private string _chartTitle;
        private bool _isVisible;
        private IDevice _selectedDevice;

        #endregion

        #region Properties

        /// <summary>
        /// We use the ObservableCollection class to let the chart know 
        /// when a new item is added or removed from the chart. 
        /// </summary>
        public ObservableCollection<ISeries> Series { get; private set; }

        /// <summary>
        /// Gets or sets the collection of X-axis configurations for the chart.
        /// </summary>
        public Axis[] XAxes { get; private set; }

        /// <summary>
        /// The ObservablePoints property is an ObservableCollection of Values.
        /// </summary>
        public ObservableCollection<DateTimePoint> Values => _values;

        /// <summary>
        /// Gets or sets the title of the chart.
        /// </summary>
        public string ChartTitle
        {
            get => _chartTitle;
            set => SetProperty(ref _chartTitle, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the graph is visible.
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        /// <summary>
        /// Gets or sets the currently selected device.
        /// </summary>
        public IDevice SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the current instance.
        /// </summary>
        public object Sync { get; } = new object();

        #endregion

        #region Constructor
        public LiveGraphManager()
        {
            Series = [];
        }
        #endregion

        /// <summary>
        /// Configures the graph for real-time data visualization.
        /// </summary>
        /// <remarks>This method sets up the graph with a line series for displaying real-time data and
        /// custom X-axis settings. The graph's series and axes are updated to reflect the configuration.</remarks>
        public void ConfigureForDevice(IDevice device)
        {
            _selectedDevice = device;
            _chartTitle = $"Live Device Data: {device.Name}";
            _values.Clear();

            var lineSeries = GraphHelper.GetLineSeriesForRealTimeGraph();
            lineSeries.Values = _values;
            Series = [lineSeries];
            _customAxis = GraphHelper.GetCustomXAxes();
            XAxes = [_customAxis];

            RaisePropertyChanged(nameof(Series));
            RaisePropertyChanged(nameof(XAxes));
            RaisePropertyChanged(nameof(ChartTitle));
        }

        /// <summary>
        /// Adds a new data point to the series, using the current timestamp and the specified value.
        /// </summary>
        public void AddDataPoint(object newValue)
        {
            if (!double.TryParse(newValue.ToString(), out double parsedValue))
                return;

            lock (Sync)
            {
                _values.Add(new DateTimePoint(DateTime.Now, parsedValue));
                if (_values.Count > 50)
                    _values.RemoveAt(0);

                _customAxis.CustomSeparators = GraphHelper.GetSeparators();
            }

            RaisePropertyChanged(nameof(Series));
        }

        /// <summary>
        /// Clears all data from the collection, including series and axes, and raises property change notifications.
        /// </summary>
        public void Clear()
        {
            lock (Sync)
            {
                _values.Clear();
                Series = [];
                XAxes = [];
            }
            RaisePropertyChanged(nameof(Series));
            RaisePropertyChanged(nameof(XAxes));
        }
    }
}
