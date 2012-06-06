//
//  WindowInvisible.xaml.cs
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

namespace win_client.Views
{
    public partial class WindowInvisibleView : Window
    {
        private System.Windows.Forms.ContextMenu _cm =new System.Windows.Forms.ContextMenu();

        public WindowInvisibleView()
        {
            InitializeComponent();

            Width = 0;
            Height = 0;
            WindowStyle = WindowStyle.None;
            ShowInTaskbar = false;
            ShowActivated = false;

            Loaded += new RoutedEventHandler(OnLoadedCallback);

            Messenger.Default.Register<CLBalloonTooltipNotification>(this, "Message_BalloonTooltipSystemTrayNotification", (tooltipInfo) => { OnCLBalloonTooltipNotificationMessage(tooltipInfo);});
            Messenger.Default.Register<CLGrowlNotification>(this, "Message_GrowlSystemTrayNotification", (growlInfo) => { OnMessage_GrowlSystemTrayNotificationMessage(growlInfo); });
        }

        void OnLoadedCallback(object sender, RoutedEventArgs e)
        {
            tb.TrayToolTip = null;                      // use the standard Windows tooltip (fancy WPF tooltips are available)

#if SILVERLIGHT 
            var dispatcher = Deployment.Current.Dispatcher; 
#else
            var dispatcher = Dispatcher.CurrentDispatcher;
#endif
            dispatcher.DelayedInvoke(TimeSpan.FromMilliseconds(100), () => { CLAppDelegate.Instance.startCloudAppServicesAndUI(); });
        }

        void  MenuItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            System.Windows.MessageBox.Show(e.ClickedItem.Text);
        }

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

        void OnMessage_GrowlSystemTrayNotificationMessage(CLGrowlNotification growlInfo)
        {
            // Show this growl over the system tray.  It will automatically fade after several seconds.
            tb.ShowCustomBalloon(growlInfo.WpfControl, growlInfo.Animation, growlInfo.TimeoutMilliseconds);
        }
    }
}
