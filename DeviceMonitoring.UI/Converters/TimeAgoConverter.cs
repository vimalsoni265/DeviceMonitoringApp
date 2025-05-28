using System.Globalization;
using System.Windows.Data;

namespace DeviceMonitoring.UI.Converters
{
    public class TimeAgoConverter : IMultiValueConverter
    {
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeAgoConverter"/> class using the default TimeProvider.
        /// </summary>
        public TimeAgoConverter() : this(TimeProvider.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeAgoConverter"/> class with a specific TimeProvider.
        /// </summary>
        /// <param name="timeProvider">The time provider to use for current time calculations.</param>
        public TimeAgoConverter(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values[0] == null)
                return string.Empty;

            if (values[0] is DateTime lastUpdated)
            {
                // Use the injected TimeProvider instead of DateTime.Now
                var timeSpan = _timeProvider.Now - lastUpdated;

                if (timeSpan.TotalSeconds < 60)
                    return $"{timeSpan.Seconds} seconds ago";
                if (timeSpan.TotalMinutes < 60)
                    return $"{timeSpan.Minutes} minutes ago";
                if (timeSpan.TotalHours < 24)
                    return $"{timeSpan.Hours} hours ago";

                return $"{timeSpan.Days} days ago";
            }

            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
