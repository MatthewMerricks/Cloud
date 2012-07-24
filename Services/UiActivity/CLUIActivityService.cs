﻿//
//  CLUIActivityService.cs
//  (was CLAgentService.cs)
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using CloudApiPublic.Support;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Hardcodet.Wpf.TaskbarNotification;
using win_client.Common;
using win_client.Services.Notification;
using win_client.ViewModels;

namespace win_client.Services.UiActivity
{
    public enum menuItemActivityLabelType
    {
        menuItemActivityLabelOffLine = 0,
        menuItemActivityLabelSyncing = 1,
        menuItemActivityLabelSynced = 2
    }

    public sealed class CLUIActivityService
    {
        private static CLUIActivityService _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace;
        private System.Threading.Timer _pollTimer = null;
        private uint _timerTestCount = 0;               //&&&& testing

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLUIActivityService Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLUIActivityService();

                        // Initialize at first Instance access here
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLUIActivityService()
        {
            // Initialize members, etc. here (at static initialization time).
            _trace = CLTrace.Instance;
        }

        /// <summary>
        /// Start the UI activity service.
        /// </summary>
        public void BeginUIActivityService()
        {
            // Start a timer for testing.
            _pollTimer = new System.Threading.Timer((s) => { TimerFiredCallback(); }, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(1));

            PageInvisibleViewModel vmPageInvisible = SimpleIoc.Default.GetInstance<PageInvisibleViewModel>();
            vmPageInvisible.TaskbarIconVisibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// End the UI activity service.
        /// </summary>
        public void EndUIActivityService()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
                _pollTimer = null;
            }
        }

        /// <summary>
        /// Test timer callback
        /// </summary>
        public void TimerFiredCallback()
        {
            // One second timer tick...
            ++_timerTestCount;
            if (_timerTestCount == 5)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    // Put up a test balloon tooltip.  It will automatically fade.
                    _trace.writeToLog(9, "CLUIActivityService: TimerFiredCallback: Put up a test balloon from the system tray.");
                    CLBalloonTooltipNotification tooltipInfo = new CLBalloonTooltipNotification("Test Title!", "This is the notification body text.", BalloonIcon.Error, null);
                    CLAppMessages.Message_BalloonTooltipSystemTrayNotification.Send(tooltipInfo);
                }));
            }

            if (_timerTestCount == 25)
            {
                // Put up a growl notification.  It will automatically fade.
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    _trace.writeToLog(9, "CLUIActivityService: TimerFiredCallback: Put up a test growl notification from the system tray.");
                    var window = SimpleIoc.Default.GetInstance<FancyBalloon>();
                    window.BalloonText = "Hello Cloud!";
                    CLGrowlNotification growlInfo = new CLGrowlNotification(window, System.Windows.Controls.Primitives.PopupAnimation.Slide, 2500);
                    CLAppMessages.Message_GrowlSystemTrayNotification.Send(growlInfo);
                }));
            }
        }
    }
}
