//
//  WindowSyncStatus.xaml.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

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
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Data;
using win_client.Common;
using win_client.ViewModels;
using Dialog.Abstractions.Wpf.Intefaces;
using Xceed.Wpf.Toolkit;
using CleanShutdown.Messaging;
using System.Windows.Threading;
using CloudApiPrivate.Model.Settings;
using CleanShutdown.Helpers;
using Cloud.Support;
using CloudApiPrivate.Common;
using System.Diagnostics;
using Cloud.Model;
using System.Collections.Specialized;
using Cloud.EventMessageReceiver;

namespace win_client.Views
{
    public partial class WindowSyncStatus : Window, IModalWindow
    {
        private DispatcherTimer _timer;
        private bool _isVisible = false;
        private bool _isShuttingDown = false;
        private static CLTrace _trace = CLTrace.Instance;
        private EventMessageReceiver _vm = null;

        public WindowSyncStatus()
        {
            try
            {
                _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus constructor: Entry. Call InitializeComponent.");
                InitializeComponent();
                _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus constructor: Back from InitializeComponent.");

                // Register for messages
                CLAppMessages.Message_WindowSyncStatus_ShouldClose.Register(this, OnMessage_WindowSyncStatus_ShouldClose);

                Loaded += WindowSyncStatus_Loaded;
                Unloaded += WindowSyncStatus_Unloaded;
                Closing += WindowSyncStatus_Closing;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus: ERROR. Exception: Msg: <{0}>. Code: {1}.", error.errorDescription, ((int)error.code).ToString());
                System.Windows.Forms.MessageBox.Show(String.Format("Unable to start the Cloud application (WindowSyncStatus).  Msg: <{0}>. Code: {1}.", error.errorDescription, ((int)error.code).ToString()));
                global::System.Windows.Application.Current.Shutdown(0);
            }

            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus constructor: Exit.");
        }

        private void OnMessage_WindowSyncStatus_ShouldClose(string obj)
        {
            this.Close();
        }

        void WindowSyncStatus_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the ViewModel
            _vm = (EventMessageReceiver)this.DataContext;

            // Register for messages
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Loaded: Entry.");
            this.cmdDone.Focus();

        }

        void WindowSyncStatus_Unloaded(object sender, RoutedEventArgs e)
        {
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Unloaded: Entry.");
        }

        void WindowSyncStatus_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Closing: Entry.");
            Messenger.Default.Unregister(this);
        }


    }
}
