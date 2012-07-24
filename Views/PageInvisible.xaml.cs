﻿//
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

namespace win_client.Views
{
    public partial class PageInvisible : Page, IOnNavigated
    {
        private System.Windows.Forms.ContextMenu _cm =new System.Windows.Forms.ContextMenu();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PageInvisible()
        {
            InitializeComponent();

            // Register event handlers
            Loaded += new RoutedEventHandler(OnLoadedCallback);
            Unloaded += new RoutedEventHandler(OnUnloadedCallback);

            // Register messages
            CLAppMessages.PageInvisible_NavigationRequest.Register(this,
                (uri) =>
                {
                    this.NavigationService.Navigate(uri, UriKind.Relative);
                });

            CLAppMessages.Message_BalloonTooltipSystemTrayNotification.Register(this, (tooltipInfo) => { OnCLBalloonTooltipNotificationMessage(tooltipInfo); });
            CLAppMessages.Message_GrowlSystemTrayNotification.Register(this, (growlInfo) => { OnMessage_GrowlSystemTrayNotificationMessage(growlInfo); });
        }

        /// <summary>
        /// Loaded event handler
        /// </summary>
        void OnLoadedCallback(object sender, RoutedEventArgs e)
        {
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
            Messenger.Default.Unregister(this);
        }

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
            if(tooltipInfo.CustomIcon != null)
            {
                tb.ShowBalloonTip(tooltipInfo.Title, tooltipInfo.Text, tooltipInfo.CustomIcon);
            }
            else
            {
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
            tb.ShowCustomBalloon(growlInfo.WpfControl, growlInfo.Animation, growlInfo.TimeoutMilliseconds);
        }

        /// <summary>
        /// Navigated event handler.
        /// </summary>
        CLError IOnNavigated.HandleNavigated(object sender, NavigationEventArgs e)
        {
            try
            {
                // Hide the window.
                CLAppDelegate.HideMainWindow(Window.GetWindow(this));
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

    }
}
