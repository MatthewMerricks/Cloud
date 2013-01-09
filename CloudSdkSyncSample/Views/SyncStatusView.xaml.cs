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
using CloudSdkSyncSample.ViewModels;

namespace CloudSdkSyncSample.Views
{
    public partial class SyncStatusView : Window
    {
        private static CLTrace _trace = CLTrace.Instance;
        private EventMessageReceiver _vm = null;
        private SyncStatusViewModel _vmCommand = null;

        public bool AllowClose { get; set; }

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

        private void WindowSyncStatus_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the EventMessageReceiver ViewModel which will be the DataContext
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Loaded: Entry.");
            _vm = (EventMessageReceiver)this.DataContext;

            // Get our own ViewModel for commands
            _vmCommand = Application.Current.Resources["SyncStatusViewModel"] as SyncStatusViewModel;

            if (_vmCommand == null)
            {
                MessageBox.Show("SyncStatusViewModel is the wrong type", "Error", MessageBoxButton.OK);
            }
            else
            {
                _vmCommand.NotifySyncStatusWindowShouldClose += OnNotifySyncStatusWindowShouldClose;
            }

            // Focus to the button.
            this.cmdDone.Focus();
        }

        private void WindowSyncStatus_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unload the event receiver
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Unloaded: Entry.");
            if (_vm != null)
            {
                _vm.Dispose();
                _vm = null;
            }

            if (_vmCommand != null)
            {
                _vmCommand.NotifySyncStatusWindowShouldClose -= OnNotifySyncStatusWindowShouldClose;
                _vmCommand = null;
            }
        }

        private void WindowSyncStatus_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Closing: Entry.");
            if (!AllowClose)
            {
                // Just hide the window on the UI thread
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    this.Width = 0;
                    this.Height = 0;
                    this.MinWidth = 0;
                    this.MinHeight = 0;
                    this.Left = Int32.MaxValue;
                    this.Top = Int32.MaxValue;
                    this.ShowInTaskbar = false;
                    this.ShowActivated = false;
                    this.Visibility = Visibility.Hidden;
                    this.WindowStyle = WindowStyle.None;
                    this.Show();
                }));

                // Prevent closing
                e.Cancel = true;
            }
        }

        /// <summary>
        /// The user clicked the Done button.  Hide this window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNotifySyncStatusWindowShouldClose(object sender, Support.NotificationEventArgs e)
        {
            this.Close();
        }
    }
}