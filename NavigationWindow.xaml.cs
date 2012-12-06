//
//  NavigationWindow.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Windows.Interop;
using System.IO;
using win_client.AppDelegate;
using System.Windows.Forms;
using win_client.Model;
using CleanShutdown;
using CleanShutdown.Helpers;
using win_client.Common;
using win_client.Static;
using GalaSoft.MvvmLight.Messaging;
using CleanShutdown.Messaging;
using CloudApiPrivate.Model.Settings;
using CloudApiPublic.Support;
using CloudApiPublic.Model;

namespace win_client
{
    /// <summary>
    /// Interaction logic for NavigationWindow.xaml
    /// </summary>
    public partial class MyNavigationWindow : NavigationWindow, IDisposable
    {
        #region Private Instance Variables

        private bool disposed = false;
        private CLTrace _trace = CLTrace.Instance;
        private uint _taskbarCreatedWndMsg = 0;

        #endregion

        #region Public Variables
        public bool firstMinimize = false;                      // set this to indicate that PageInvisible will be the first 
        #endregion

        #region Life Cycle

        /// <summary>
        /// Default constructor
        /// </summary>
        public MyNavigationWindow()
        {
            try
            {
                this.NavigationService.Navigated += NavigationService_Navigated;
                this.NavigationService.Navigating += NavigationService_Navigating;
                this.Closing += MyNavigationWindow_Closing;

                _trace.writeToLog(9, "NavigationWindow: NavigationWindow constructor: Call InitializeComponent.");
                InitializeComponent();
                _trace.writeToLog(9, "NavigationWindow: NavigationWindow constructor: After InitializeComponent.");

                // Register messages
                Messenger.Default.Register<CleanShutdown.Messaging.NotificationMessageAction<bool>>(
                    this,
                    message =>
                    {
                        OnQueryNotificationMessageAction(message);
                    });
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(9, "NavigationWindow: NavigationWindow: ERROR. Exception: Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode);
                System.Windows.Forms.MessageBox.Show(String.Format("Unable to start the Cloud application (MyNavigationWindow).  Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode));
                global::System.Windows.Application.Current.Shutdown(0);
            }
            _trace.writeToLog(9, "NavigationWindow: NavigationWindow constructor: Exit.");
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// We received a query message
        /// </summary>
        private void OnQueryNotificationMessageAction(CleanShutdown.Messaging.NotificationMessageAction<bool> message)
        {
            if (message.Notification == Notifications.QueryFirstPageInvisible)
            {
                message.Execute(firstMinimize);     // return the current value.
                firstMinimize = false;              // only once
            }
        }

        /// <summary>
        /// This window is closing.
        /// </summary>
        void MyNavigationWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _trace.writeToLog(9, "NavigationWindow: MyNavigationWindow_Closing: Entry.");
            e.Cancel = ShutdownService.RequestShutdown();

            // Save the position of the window if we will be shutting down, and if the window
            // is visible.
            if (!e.Cancel && this.WindowStyle != System.Windows.WindowStyle.None && this.Visibility == System.Windows.Visibility.Visible)
            {
                _trace.writeToLog(1, "NavigationWindow: MyNavigationWindow_Closing: Set MainWindowPlacement. Coords: {0},{1},{2},{3}(LRWH). Title: {4}.", this.Left, this.Top, this.Width, this.Height, this.Title);
                Settings.Instance.MainWindowPlacement = this.GetPlacement();
            }
        }

        /// <summary>
        /// Event handler: Navigated.
        /// </summary>
        public static void NavigationService_Navigated(object sender, NavigationEventArgs e)
        {
            // Cause all pages to unregister their messages because we may be navigating away from them.
            CLAppMessages.Message_PageMustUnregisterWindowClosingMessage.Send("");

            // Now drive the target page's HandleNavigated() event.  It will reregister for messages as required.
            IOnNavigated castContent = e.Content as IOnNavigated;
            if (castContent != null)
            {
                castContent.HandleNavigated(sender, e);
            }
        }

        /// <summary>
        /// Event handler: Navigating.
        /// Ignore F5 refresh.
        /// </summary>
        public static void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Refresh)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Capture the SourceInitialized event.  This function is used to add a WndProc hook so we
        /// can watch for particular window messages.
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            _taskbarCreatedWndMsg = NativeMethods.RegisterWindowMessage(NativeMethods.TBM_CREATE_STRING);
            source.AddHook(WndProc);
        }

        /// <summary>
        /// Handle window messages.
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Look for a message to indicate that Explorer has restarted and we should restore our NotifyIcon.
            // Message 0xC09F is apparently sent when Explorer restarts.
            if (_taskbarCreatedWndMsg != 0 && msg == _taskbarCreatedWndMsg)
            {
                if (this.Visibility != global::System.Windows.Visibility.Visible)
                {
                    ResetNotifyIcon();
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Event handler: Tray icon left double-click.
        /// </summary>
        void TrayIcon_LeftDoubleClick(object sender, EventArgs e)
        {
            // If window needs opening...
            if (WindowState == System.Windows.WindowState.Minimized)
                WindowState = System.Windows.WindowState.Normal;

        }

        private void ResetNotifyIcon()
        {
            try 
	        {
                CLAppMessages.Message_PageInvisible_ResetNotifyIcon.Send("");       // have the view reset the NotifyIcon
	        }
	        catch (Exception ex)
	        {
                CLError error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(9, "NavigationWindow: ResetNotifyIcon: ERROR. Exception: Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode);
            }
        }

        #endregion

        #region Dispose

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        public void Dispose() 
        { 
            Dispose(true); 
            GC.SuppressFinalize(this); 
        } 
 
        // Leave out the finalizer altogether if this class doesn't own unmanaged
        // resources itself.
        //~MyNavigationWindow() 
        //{ 
        //    Dispose(false); 
        //} 
 
        // Dispose
        protected virtual void Dispose(bool disposing) 
        { 
            if (!this.disposed) 
            { 
                if (disposing) 
                { 
                    // Free managed resources, if any
                } 

                // Free native (unmanaged) resources, if any
            } 
            disposed = true; 
        }

        #endregion
    }
}
