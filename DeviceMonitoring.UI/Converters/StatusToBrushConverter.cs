using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DeviceMonitoring.UI.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public Brush MonitoringBrush { get; set; } = Brushes.Green;
        public Brush StoppedBrush { get; set; } = Brushes.Red;
        public Brush DefaultBrush { get; set; } = Brushes.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;
            return status switch
            {
                "Monitoring" => MonitoringBrush,
                "Stopped" => StoppedBrush,
                _ => DefaultBrush
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
