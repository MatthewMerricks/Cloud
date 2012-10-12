//
//  HandlingEventTrigger.cs
//  Cloud Windows
//
//  Created by DavidBruck.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interactivity;

namespace win_client.Common
{
    public class HandlingEventTrigger : global::System.Windows.Interactivity.EventTrigger
    {
        protected override void OnEvent(EventArgs eventArgs)
        {
            RoutedEventArgs routedArgs = eventArgs as RoutedEventArgs;
            if (routedArgs != null)
            {
                routedArgs.Handled = true;
            }

            base.OnEvent(eventArgs);
        }
    }
}