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
using CloudApiPublic.Support;
using CloudApiPrivate.Common;
using System.Diagnostics;
using CloudApiPublic.Model;

namespace win_client.Views
{
    public partial class WindowSyncStatus : Window, IModalWindow
    {
        private DispatcherTimer _timer;
        private bool _isVisible = false;
        private bool _isShuttingDown = false;
        private static CLTrace _trace = CLTrace.Instance;
        private WindowSyncStatusViewModel _vm = null;

        public WindowSyncStatus()
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
                error.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus: ERROR. Exception: Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode);
                System.Windows.Forms.MessageBox.Show(String.Format("Unable to start the Cloud application (WindowSyncStatus).  Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode));
                global::System.Windows.Application.Current.Shutdown(0);
            }

            _vm = (WindowSyncStatusViewModel)this.DataContext;
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus constructor: Exit.");
        }

        void WindowSyncStatus_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Closing: Entry.");
            //TODO: Add code.
        }

        void WindowSyncStatus_Unloaded(object sender, RoutedEventArgs e)
        {
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Unloaded: Entry.");
            //TODO: Add code.
        }

        void WindowSyncStatus_Loaded(object sender, RoutedEventArgs e)
        {
            // Register for messages
            _trace.writeToLog(9, "WindowSyncStatus: WindowSyncStatus_Loaded: Entry.");

            // Give focus to the right button.
            //TODO: The caller's should establish the focus position in a parameter.
            //this.cmdOk.Focus();

        }

    }
}
