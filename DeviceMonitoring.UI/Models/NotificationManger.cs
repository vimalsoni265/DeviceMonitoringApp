using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            _snackbar?.MessageQueue?.Enqueue(message, null, null, null, false, true, TimeSpan.FromMilliseconds(durationMilliseconds));
        }
    }
}
