using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DeviceMonitoring.UI.Converters
{
    public class TemperatureToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (double.TryParse(value?.ToString(), out double  temp))
            {
                if (temp < 40)
                    return FromHex("#FF4CAF50"); // Green for low temperatures
                else if (temp < 70)
                    return FromHex("#FFFFEB3B"); // Yellow for moderate temperatures
                else
                    return FromHex("#FFF44336"); // Red for high temperatures
            }

            return new SolidColorBrush(Colors.Black); // fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static SolidColorBrush FromHex(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
        }
    }
}
