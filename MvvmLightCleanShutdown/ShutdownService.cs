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

using System.Windows;
using CleanShutdown.Messaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace CleanShutdown.Helpers
{
    public static class ShutdownService
    {
        /// <summary>
        /// Request shutdown.
        /// <param name="void"></param>/>
        /// <returns>bool: true: Cancel the original Window.Closing event to prevent automatic window close.</returns>
        /// </summary>
        public static bool RequestShutdown()
        {
            var shouldAbortShutdown = false;

            Messenger.Default.Send(new CleanShutdown.Messaging.NotificationMessageAction<bool>(
                                       Notifications.ConfirmShutdown,
                                       shouldAbort => shouldAbortShutdown |= shouldAbort));

            if (!shouldAbortShutdown)
            {
                // This time it is for real
                //Original: Messenger.Default.Send(new CommandMessage(Notifications.NotifyShutdown));
                Messenger.Default.Send(new NotificationMessage(Notifications.NotifyShutdown));

                Application.Current.Shutdown();
            }

            return shouldAbortShutdown;
        }
    }
}

