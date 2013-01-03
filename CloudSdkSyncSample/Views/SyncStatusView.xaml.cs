using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Windows.Data;
using System.Windows.Threading;
using CloudApiPublic.Support;
using System.Diagnostics;
using CloudApiPublic.Model;
using System.Collections.Specialized;
using CloudApiPublic.EventMessageReceiver;

namespace CloudSdkSyncSample.Views
{
    public partial class SyncStatusView : Window
    {
        private DispatcherTimer _timer;
        private bool _isVisible = false;
        private bool _isShuttingDown = false;
        private static CLTrace _trace = CLTrace.Instance;
        private EventMessageReceiver _vm = null;

        public SyncStatusView()
        {
            try
            {
                _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus constructor: Entry. Call InitializeComponent.");
                InitializeComponent();
                _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus constructor: Back from InitializeComponent.");

                Loaded += WindowSyncStatus_Loaded;
                Unloaded += WindowSyncStatus_Unloaded;
                Closing += WindowSyncStatus_Closing;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus: ERROR. Exception: Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode);
                System.Windows.Forms.MessageBox.Show(String.Format("Unable to start the Cloud application (WindowSyncStatus).  Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode));
                global::System.Windows.Application.Current.Shutdown(0);
            }

            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus constructor: Exit.");
        }

        void WindowSyncStatus_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the ViewModel
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Loaded: Entry.");
            _vm = (EventMessageReceiver)this.DataContext;

            // Register for messages
            MessageSender.Instance.NotifySyncStatusWindowShouldClose += OnNotifySyncStatusWindowShouldClose;

            // Focus to the button.
            this.cmdDone.Focus();

        }

        /// <summary>
        /// This window should close.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnNotifySyncStatusWindowShouldClose(object sender, Support.NotificationEventArgs e)
        {
            this.Close();
        }

        void WindowSyncStatus_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unregister for messages
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Unloaded: Entry.");
            MessageSender.Instance.NotifySyncStatusWindowShouldClose -= OnNotifySyncStatusWindowShouldClose;

            // Unload the event receiver
            if (_vm != null)
            {
                _vm.Dispose();
                _vm = null;
            }
        }

        void WindowSyncStatus_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Closing: Entry.");
        }
    }
}
