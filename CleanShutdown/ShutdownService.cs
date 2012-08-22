//
//  CLShutdownService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Changes to this module Copyright (c) Cloud.com. All rights reserved.
//
// ****************************************************************************
// <copyright file="ShutdownService.cs" company="GalaSoft Laurent Bugnion">
// Copyright © GalaSoft Laurent Bugnion 2009
// </copyright>
// ****************************************************************************
// <author>Laurent Bugnion</author>
// <email>laurent@galasoft.ch</email>
// <date>15.10.2009</date>
// <project>CleanShutdown</project>
// <web>http://www.galasoft.ch</web>
// <license>
// See license.txt in this solution or http://www.galasoft.ch/license_MIT.txt
// </license>
// ****************************************************************************

using System;
using System.Windows;
using System.Windows.Threading;
using CleanShutdown.Messaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using win_client.AppDelegate;

namespace CleanShutdown.Helpers
{
    public static class ShutdownService
    {
        private static bool weAreReallyShuttingDown = false;

        /// <summary>
        /// Request shutdown.
        /// <param name="void"></param>/>
        /// <returns>bool: true: Cancel the original Window.Closing event to prevent automatic window close.</returns>
        /// </summary>
        public static bool RequestShutdown()
         {
            var shouldAbortShutdown = false;

            // Short circuit shutdown loops
            if (weAreReallyShuttingDown)
            {
                return shouldAbortShutdown;
            }

            Messenger.Default.Send(new CleanShutdown.Messaging.NotificationMessageAction<bool>(
                                       Notifications.ConfirmShutdown,
                                       shouldAbort => shouldAbortShutdown |= shouldAbort));

            if (!shouldAbortShutdown)
            {
                // This time it is for real
                weAreReallyShuttingDown = true;

                //Original: Messenger.Default.Send(new CommandMessage(Notifications.NotifyShutdown));
                Messenger.Default.Send(new NotificationMessage(Notifications.NotifyShutdown));

                CLAppDelegate.Instance.MainDispatcher.BeginInvoke((Action)delegate()
                {
                    if (Application.Current != null)
                    {
                        // Actually shut down the application.
                        CLAppDelegate.Instance.ExitApplication();
                    }
                });
            }

            return shouldAbortShutdown;
        }
    }
}

