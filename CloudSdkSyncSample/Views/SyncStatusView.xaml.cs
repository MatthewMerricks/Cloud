using CloudApiPublic.Support;
using CloudApiPublic.Model;
using CloudApiPublic.EventMessageReceiver;
using CloudSdkSyncSample.ViewModels;
using System;
using System.Windows;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Media.Animation;

namespace CloudSdkSyncSample.Views
{
    public partial class SyncStatusView : Window
    {
        #region Private Fields

        private static CLTrace _trace = CLTrace.Instance;
        private EventMessageReceiver _vm = null;
        private SyncStatusViewModel _vmOurs = null;
        
        #endregion

        #region Public Properties

        public bool AllowClose { get; set; }

        #endregion

        #region Constructors

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
                _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus: ERROR. Exception: Msg: <{0}>. Code: {1}.", error.errorDescription, ((int)error.code).ToString());
                System.Windows.Forms.MessageBox.Show(String.Format("Unable to start the Cloud application (WindowSyncStatus).  Msg: <{0}>. Code: {1}.", error.errorDescription, ((int)error.code).ToString()));
                global::System.Windows.Application.Current.Shutdown(0);
            }

            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus constructor: Exit.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The sync status has changed.  Pass this event along to our ViewModel.
        /// </summary>
        /// <param name="userState">This is the instance of the SyncBox (CLSync) whose status has changed.</param>
        public void OnSyncStatusUpdated(object userState)
        {
            if (_vmOurs != null)
            {
                _vmOurs.OnSyncStatusUpdated(userState);
            }
        }

        #endregion

        #region Event Handlers

        private void WindowSyncStatus_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the EventMessageReceiver ViewModel which will be the DataContext
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Loaded: Entry.");
            _vm = (EventMessageReceiver)this.DataContext;

            // Get our own ViewModel for commands
            _vmOurs = this.Resources["SyncStatusViewModel"] as SyncStatusViewModel;

            if (_vmOurs == null)
            {
                MessageBox.Show("SyncStatusViewModel is the wrong type", "Error", MessageBoxButton.OK);
            }
            else
            {
                _vmOurs.NotifySyncStatusWindowShouldClose += OnNotifySyncStatusWindowShouldClose;
                _vmOurs.PropertyChanged += _vmOurs_PropertyChanged;
            }

            // Focus to the button.
            this.cmdDone.Focus();
        }

        /// <summary>
        /// The sync status changed.  If it is now syncing, start the animation.  Otherwise, stop the animation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _vmOurs_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SyncStatus")
            {
                if (_vmOurs.SyncStatus == Static.SyncStates.Syncing)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<SyncStatusView>((view) =>
                    {
                        Storyboard storyboard = view.TryFindResource("SyncingIconAnimation") as Storyboard;
                        if (storyboard != null)
                        {
                            storyboard.Begin();
                        }
                    }), this);
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<SyncStatusView>((view) =>
                    {
                        Storyboard storyboard = view.TryFindResource("SyncingIconAnimation") as Storyboard;
                        if (storyboard != null)
                        {
                            storyboard.Stop();
                        }
                    }), this);
                }
            }
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

            if (_vmOurs != null)
            {
                _vmOurs.NotifySyncStatusWindowShouldClose -= OnNotifySyncStatusWindowShouldClose;
                _vmOurs.PropertyChanged -= _vmOurs_PropertyChanged;
                _vmOurs = null;
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

        #endregion

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