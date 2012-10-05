//
//  PageInvisible.xaml.cs
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
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Data;
using win_client.Common;
using System.Globalization;
using win_client.ViewModels;
using win_client.AppDelegate;
using System.Windows.Forms;
using Hardcodet.Wpf.TaskbarNotification;
using CloudApiPrivate.Static;
using CloudApiPublic.Model;
using win_client.Model;
using CleanShutdown.Messaging;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Win32;
using CloudApiPublic.Support;
using System.Resources;
using CloudApiPrivate.Model.Settings;
using win_client.Static;
using System.Windows.Interop;

namespace win_client.Views
{
    public partial class PageInvisible : Page, IOnNavigated
    {
        #region Private Instance Variables

        private System.Windows.Forms.ContextMenu _cm =new System.Windows.Forms.ContextMenu();
        private CLTrace _trace = CLTrace.Instance;

        protected delegate void OnAnimateFromSystemTrayCompleteDelegate(Uri nextPage);
        protected delegate void OnAnimateToSystemTrayCompleteDelegate(CLBalloonTooltipNotification tooltip);

        #endregion

        #region Life Cycle

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageInvisible()
        {
            try
            {
                _trace.writeToLog(9, "PageInvisible: PageInvisible: Entry. InitializeComponent.");
                InitializeComponent();
                _trace.writeToLog(9, "PageInvisible: PageInvisible: Back from InitializeComponent.");

                // Register event handlers
                Loaded += new RoutedEventHandler(OnLoadedCallback);
                Unloaded += new RoutedEventHandler(OnUnloadedCallback);

                // Pass the view's grid to the view model for the dialogs to use.
                PageInvisibleViewModel vm = (PageInvisibleViewModel)DataContext;
                vm.ViewGridContainer = LayoutRoot;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(9, "PageInvisible: PageInvisible: ERROR. Exception: Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode);
                System.Windows.Forms.MessageBox.Show(String.Format("Unable to start the Cloud application.  Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode));
                global::System.Windows.Application.Current.Shutdown(0);
            }
        }

        /// <summary>
        /// Loaded event handler
        /// </summary>
        void OnLoadedCallback(object sender, RoutedEventArgs e)
        {
            // Register messages
            _trace.writeToLog(9, "PageInvisible: OnLoadedCallback: Entry.");
            CLAppMessages.PageInvisible_NavigationRequest.Register(this,
                (uri) =>
                {
                    //TODO: RKS Once, this.NavigationService was null.  I think it was because I double-clicked multiple times on the
                    // system tray icon.  PagePreferences was already loaded, and maybe the 2nd double-click tried to navigate a 2nd time???
                    if (this.NavigationService != null)
                    {
                        _trace.writeToLog(9, "PageInvisible: OnLoadedCallback: Navigate to {0}.", uri.ToString());
                        this.NavigationService.Navigate(uri, UriKind.Relative);
                    }
                });

            CLAppMessages.Message_BalloonTooltipSystemTrayNotification.Register(this, (tooltipInfo) => { OnCLBalloonTooltipNotificationMessage(tooltipInfo); });
            CLAppMessages.Message_GrowlSystemTrayNotification.Register(this, (growlInfo) => { OnMessage_GrowlSystemTrayNotificationMessage(growlInfo); });
            CLAppMessages.PageInvisible_TriggerOutOfSystemTrayAnimation.Register(this, (uri) => { OnPageInvisible_TriggerOutOfSystemTrayAnimation(uri); });

            // Set the containing window to be invisible
            CLAppDelegate.HideMainWindow(Window.GetWindow(this));

            // Set the system tray tooltip.
            tb.TrayToolTip = null;                      // use the standard Windows tooltip (fancy WPF tooltips are available)

            // Start the core services.
            var dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(100), () => { CLAppDelegate.Instance.startCloudAppServicesAndUI(); });
        }

        /// <summary>
        /// Unloaded event handler.
        /// </summary>
        void OnUnloadedCallback(object sender, RoutedEventArgs e)
        {
            _trace.writeToLog(9, "PageInvisible: OnUnloadedCallback: Entry.");
            Messenger.Default.Unregister(this);
        }

        #endregion

        /// <summary>
        /// System tray icon was right-clicked.
        /// </summary>
        void MenuItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            System.Windows.MessageBox.Show(e.ClickedItem.Text);
        }

        /// <summary>
        /// Show the system tray balloon tooltip notification.  This is the one that pulls out the
        /// icon to the system tray and points to it from the balloon.
        /// This is a callback driven by a message receipt.
        /// </summary>
        void OnCLBalloonTooltipNotificationMessage(CLBalloonTooltipNotification tooltipInfo)
        {
            // Show this tooltip in the system tray.  It will automatically fade after several seconds.
            _trace.writeToLog(9, "PageInvisible: OnCLBalloonTooltipNotificationMessage: Entry.");
            if (tooltipInfo.CustomIcon != null)
            {
                _trace.writeToLog(9, "PageInvisible: OnCLBalloonTooltipNotificationMessage: Show custom icon.");
                tb.ShowBalloonTip(tooltipInfo.Title, tooltipInfo.Text, tooltipInfo.CustomIcon);
            }
            else
            {
                _trace.writeToLog(9, "PageInvisible: OnCLBalloonTooltipNotificationMessage: Show normal icon.");
                tb.ShowBalloonTip(tooltipInfo.Title, tooltipInfo.Text, tooltipInfo.IconType);
            }
        }

        /// <summary>
        /// Show the slide-up WPF notification.
        /// This is a callback driven by a message receipt.
        /// </summary>
        void OnMessage_GrowlSystemTrayNotificationMessage(CLGrowlNotification growlInfo)
        {
            // Show this growl over the system tray.  It will automatically fade after several seconds.
            _trace.writeToLog(9, "PageInvisible: OnMessage_GrowlSystemTrayNotificationMessage: Entry.");
            tb.ShowCustomBalloon(growlInfo.WpfControl, growlInfo.Animation, growlInfo.TimeoutMilliseconds);
        }

        /// <summary>
        /// We should animate the PagePreferences window out of the system tray and open the window when the animation is complete.
        /// </summary>
        private void OnPageInvisible_TriggerOutOfSystemTrayAnimation(Uri nextPage)
        {
            _trace.writeToLog(9, "PageInvisible: OnPageInvisible_TriggerOutOfSystemTrayAnimation: Entry.");
            AnimateMainWindowFromSystemTray(nextPage);
        }

        /// <summary>
        /// Navigated event handler.  Another page navigated to this PageInvisible.
        /// We will go back to the system tray.
        /// </summary>
        CLError IOnNavigated.HandleNavigated(object sender, NavigationEventArgs e)
        {
            try
            {
                // Register to receive the ConfirmShutdown message
                _trace.writeToLog(9, "PageInvisible: HandleNavigated: Entry.");
                Messenger.Default.Register<CleanShutdown.Messaging.NotificationMessageAction<bool>>(
                    this,
                    message =>
                    {
                        _trace.writeToLog(9, "PageInvisible: HandleNavigated: Call OnConfirmShutdownMessage.");
                        OnConfirmShutdownMessage(message);
                    });

                // Animate the window to the system tray and put up a welcome tooltip pointing to the icon in the system tray.
                AnimateMainWindowToSystemTray();
            }
            catch (Exception ex)
            {
                _trace.writeToLog(9, "PageInvisible: HandleNavigated: ERROR: Exception.  Msg: <{0}>.", ex.Message);
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Animate the NavigationWindow to the system tray.  Put up a welcome tooltip.
        /// </summary>
        private void AnimateMainWindowToSystemTray()
        {
            // Get the window that contains this page
            _trace.writeToLog(9, "PageInvisible: AnimateMainWindowToSystemTray: Entry.");
            Window pageWindow = Window.GetWindow(this);
            if (pageWindow != null)
            {
                // Save the current window placement.
                _trace.writeToLog(9, "PageInvisible: AnimateMainWindowToSystemTray: Got pageWindow.");
                if (pageWindow.WindowStyle != WindowStyle.None && pageWindow.Visibility == System.Windows.Visibility.Visible)
                {
                    _trace.writeToLog(9, "PageInvisible: AnimateMainWindowToSystemTray: Set placement from pageWindow.");
                    _trace.writeToLog(1, "PageInvisible: AnimateMainWindowToSystemTray: Set MainWindowPlacement. Coords: {0},{1},{2},{3}(LRWH). Title: {4}.", pageWindow.Left, pageWindow.Top, pageWindow.Width, pageWindow.Height, pageWindow.Title);
                    Settings.Instance.MainWindowPlacement = pageWindow.GetPlacement();
                }

                // get the screen dimensions
                WindowInteropHelper myWindow = new WindowInteropHelper(pageWindow);
                myWindow.EnsureHandle();
                Screen currentScreen = Screen.FromHandle(myWindow.Handle);
                System.Drawing.Rectangle screenRect = currentScreen.Bounds;

                // And the starting rectangle
                System.Drawing.Rectangle pageRect = new System.Drawing.Rectangle((int)pageWindow.Left, (int)pageWindow.Top, (int)pageWindow.Width, (int)pageWindow.Height);

                // Hide the window.
                _trace.writeToLog(9, "PageInvisible: AnimateMainWindowToSystemTray: Hide the main window.");
                CLAppDelegate.HideMainWindow(Window.GetWindow(this));

                // Start animating on a separate thread
                bool startAnimation = Settings.Instance.ShouldAnimateToSystemTray;
                Settings.Instance.ShouldAnimateToSystemTray = false;  // only one time
                Task.Factory.StartNew(() =>
                {
                    // Perform the animation or not
                    if (startAnimation)
                    {
                        _trace.writeToLog(9, "PageInvisible: AnimateMainWindowToSystemTray: Start animation to tray.");
                        AnimateWindow(ToTray: true, screenRect: screenRect, pageRect: pageRect);
                    }

                    // Put up a welcome balloon tooltip.
                    _trace.writeToLog(9, "PageInvisible: AnimateMainWindowToSystemTray: Put up the welcom balloon.");
                    Dispatcher dispatcher = CLAppDelegate.Instance.MainDispatcher;
                    OnAnimateToSystemTrayCompleteDelegate del = OnAnimateToSystemTrayComplete;
                    CLBalloonTooltipNotification tooltipInfo = new CLBalloonTooltipNotification("Welcome to the Cloud!", "Check here for Cloud options.", BalloonIcon.Error, null);
                    dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), del, tooltipInfo);
                });
            }
        }

        /// <summary>
        /// The animation is complete from the system tray.
        /// Navigate to the next page.
        /// </summary>
        protected void OnAnimateToSystemTrayComplete(CLBalloonTooltipNotification tooltip)
        {
            OnCLBalloonTooltipNotificationMessage(tooltip);
        }

        /// <summary>
        /// Animate the NavigationWindow from the system tray.
        /// </summary>
        private void AnimateMainWindowFromSystemTray(Uri nextPage)
        {
            // Get the window that contains this page
            Window pageWindow = Window.GetWindow(this);
            if (pageWindow != null)
            {
                // Get the target rectangle.  The current window is way off-screen, so we can't use
                // its coordinates.  Get the coordinates from Settings.
                WINDOWPLACEMENT windowPlacement = new WINDOWPLACEMENT();
                System.Drawing.Rectangle pageRect = new System.Drawing.Rectangle(200, 200, 640, 480);  // default location
                bool rc = WindowPlacement.ExtractWindowPlacementInfo(Settings.Instance.MainWindowPlacement, ref windowPlacement);
                if (rc)
                {
                    pageRect.X = windowPlacement.normalPosition.left;
                    pageRect.Y = windowPlacement.normalPosition.top;
                    pageRect.Width = windowPlacement.normalPosition.right - windowPlacement.normalPosition.left;
                    pageRect.Height = windowPlacement.normalPosition.bottom - windowPlacement.normalPosition.top;
                }

                // Fix up the window's coordinates so the proper screen will be found below.
                pageWindow.Left = pageRect.X;
                pageWindow.Top = pageRect.Y;
                pageWindow.Width = pageRect.Width;
                pageWindow.Height = pageRect.Height;

                // get the screen dimensions
                WindowInteropHelper myWindow = new WindowInteropHelper(pageWindow);
                myWindow.EnsureHandle();
                Screen currentScreen = Screen.FromHandle(myWindow.Handle);
                System.Drawing.Rectangle screenRect = currentScreen.Bounds;

                // Start animating on a separate thread
                Task.Factory.StartNew(() =>
                {
                    // Perform the animation
                    //TODO: Add the animation back in?
                    //AnimateWindow(ToTray: false, screenRect: screenRect, pageRect: pageRect);

                    // Navigate to the next page on the main thread
                    Dispatcher dispatcher = CLAppDelegate.Instance.MainDispatcher;
                    OnAnimateFromSystemTrayCompleteDelegate del = OnAnimateFromSystemTrayComplete;
                    dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(20), del, nextPage);
                });
            }
        }

        /// <summary>
        /// The animation is complete from the system tray.
        /// Navigate to the next page.
        /// </summary>
        protected void OnAnimateFromSystemTrayComplete(Uri nextPage)
        {
            // The animation from the system tray is complete.  Navigate to the proper page.
            CLAppMessages.PageInvisible_NavigationRequest.Send(nextPage);
        }


        /// <summary>
        /// NavigationWindow sends this to all pages prior to driving the HandleNavigated event above.
        /// Upon receipt, the page must unregister the WindowClosingMessage.
        /// </summary>
        private void OnMessage_PageMustUnregisterWindowClosingMessage(string obj)
        {
            Messenger.Default.Unregister<CleanShutdown.Messaging.NotificationMessageAction<bool>>(this, message => { });
        }

        /// <summary>
        /// The user clicked the 'X' on the NavigationWindow.  That sent a ConfirmShutdown message.
        /// If we will handle the shutdown ourselves, inform the ShutdownService that it should abort
        /// the automatic Window.Close (set true to message.Execute.
        /// </summary>
        private void OnConfirmShutdownMessage(CleanShutdown.Messaging.NotificationMessageAction<bool> message)
        {
            try
            {
                if (message.Notification == Notifications.ConfirmShutdown)
                {
                    // Ask the ViewModel if we should allow the window to close.
                    // This should not block.
                    PageInvisibleViewModel vm = (PageInvisibleViewModel)DataContext;
                    if (vm.WindowCloseRequested.CanExecute(null))
                    {
                        vm.WindowCloseRequested.Execute(null);
                    }

                    // Get the answer and set the real event Cancel flag appropriately.
                    message.Execute(!vm.WindowCloseOk);      // true == abort shutdown
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "PageInvisible: OnConfirmShutdownMessage: ERROR: Exception.  Msg: <{0}>.", ex.Message);
                try
                {
                    _trace.writeToLog(1, "PageInvisible: OnConfirmShutdownMessage: Allow the shutdown.");
                    message.Execute(false);         // false == allow shutdown
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Animate this window to or from the system tray.
        /// </summary>
        private void AnimateWindow(bool ToTray, System.Drawing.Rectangle screenRect, System.Drawing.Rectangle pageRect)
        {
            try
            {
                // We should not animate if we are already installed and just restarting the Cloud app.
                _trace.writeToLog(9, "PageInvisible: AnimateWindow: Entry.");
                bool isFirstTimePageInvisibleAtInit = false;
                Messenger.Default.Send(new CleanShutdown.Messaging.NotificationMessageAction<bool>(
                              Notifications.QueryFirstPageInvisible,
                                  firstTime => isFirstTimePageInvisibleAtInit = firstTime));
                if (isFirstTimePageInvisibleAtInit)
                {
                    _trace.writeToLog(9, "PageInvisible: AnimateWindow: Not animating to system tray on normal restart.");
                    return;                         
                }

                // A user can enable/disable window animation by setting the the "MinAnimate" key under 
                // HKeyCurrentUser\Control Panel\Desktop. This value need to be read inorder to set our Animation Falg.
                _trace.writeToLog(9, "PageInvisible: AnimateWindow: Get the MinAnimate registry key.");
                RegistryKey animationKey = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop\\WindowMetrics", true);
                object animKeyValue = animationKey.GetValue("MinAnimate");

                if (animKeyValue != null && (System.Convert.ToInt32(animKeyValue.ToString()) == 0))
                {
                    _trace.writeToLog(1, "PageInvisible: AnimateWindow: User has disabled window animation.");
                    return;
                }

                // figure out where the taskbar is (and consequently the tray)
                _trace.writeToLog(9, "PageInvisible: AnimateWindow: Allocate the structures.");
                System.Drawing.Point destPoint = default(System.Drawing.Point);
                NativeMethods.APPBARDATA BarData = default(NativeMethods.APPBARDATA);
                BarData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(BarData);
                NativeMethods.SHAppBarMessage((int)NativeMethods.ABMsg.ABM_GETTASKBARPOS, ref BarData);
                switch (BarData.uEdge)
                {
                    case NativeMethods.ABEdge.ABE_BOTTOM:
                    case NativeMethods.ABEdge.ABE_RIGHT:
                        // Tray is to the Bottom Right
                        destPoint = new System.Drawing.Point(BarData.rc.right - 100, BarData.rc.bottom);
                        break;
                    case NativeMethods.ABEdge.ABE_LEFT:
                        // Tray is to the Bottom Left
                        destPoint = new System.Drawing.Point(100, BarData.rc.bottom);
                        break;
                    case NativeMethods.ABEdge.ABE_TOP:
                        // Tray is to the Top Right
                        destPoint = new System.Drawing.Point(BarData.rc.right, 100);
                        break;
                }
                // setup our loop based on the direction
                double a = 0;
                double b = 0;
                double s = 0;
                if (ToTray)
                {
                    a = 0;
                    b = 1;
                    s = 0.05;
                }
                else
                {
                    a = 1;
                    b = 0;
                    s = -0.05;
                }
                // "animate" the window
                System.Drawing.Point curPoint = default(System.Drawing.Point);
                System.Drawing.Size curSize = default(System.Drawing.Size);
                System.Drawing.Point startPoint = new System.Drawing.Point((int)pageRect.Left, (int)pageRect.Top);

                double dWidth = destPoint.X - startPoint.X;
                double dHeight = destPoint.Y - startPoint.Y;
                double startWidth = pageRect.Width;
                double startHeight = pageRect.Height;
                double i = 0;
                _trace.writeToLog(9, "PageInvisible: AnimateWindow: Start animating the window.");
                for (i = a; ToTray ? i <= b : i >= b; i += s)
                {
                    curPoint = new System.Drawing.Point(startPoint.X + (int)(i * dWidth), startPoint.Y + (int)(i * dHeight));
                    curSize = new System.Drawing.Size((int)((1 - i) * startWidth), (int)((1 - i) * startHeight));
                    ControlPaint.DrawReversibleFrame(new System.Drawing.Rectangle(curPoint, curSize), System.Drawing.Color.Black, FrameStyle.Thick);
                    System.Threading.Thread.Sleep(1);
                    ControlPaint.DrawReversibleFrame(new System.Drawing.Rectangle(curPoint, curSize), System.Drawing.Color.Black, FrameStyle.Thick);
                }

            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "PageInvisible: AnimateWindow: ERROR: Exception.  Message: {0}, Code: {1}.", error.errorDescription, error.errorCode);
            }
        }
    }
}
