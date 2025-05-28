using MaterialDesignThemes.Wpf;
using System.Windows.Threading;

namespace DeviceMonitoring.UI.Models
{
    /// <summary>
    /// Defines a service for displaying notifications to the user.
    /// </summary>
    public interface INotificationManger
    {
        void ShowMessage(string message, int durationMilliseconds = 2000);
    }

    public class NotificationManger : INotificationManger
    {
        private Snackbar _snackbar;

        public void SetSnackbar(Snackbar snackbar)
        {
            _snackbar = snackbar;
        }

        public void ShowMessage(string message, int durationMilliseconds = 2000)
        {
            // If not on the UI thread, invoke the method on the UI thread
            if (!Dispatcher.CurrentDispatcher.CheckAccess())
            {
                Dispatcher.CurrentDispatcher.Invoke(() => ShowMessage(message, durationMilliseconds));
                return;
            }

            _snackbar?.MessageQueue?.Enqueue(message, null, null, null, false, true, TimeSpan.FromMilliseconds(durationMilliseconds));
        }
    }
}
