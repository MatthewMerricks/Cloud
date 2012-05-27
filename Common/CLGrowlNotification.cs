//
//  CLGrowlNotification.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;
using System.Windows.Controls.Primitives;


namespace win_client.Common
{
    public class CLGrowlNotification
    {
        public UIElement WpfControl;
        public PopupAnimation Animation;
        public int? TimeoutMilliseconds;

        public CLGrowlNotification()
        {
            throw new NotImplementedException("Default constructor not supported.");
        }

        public CLGrowlNotification(UIElement wpfControl, PopupAnimation animation, int? timeoutMilliseconds)
        {
            WpfControl = wpfControl;
            Animation = animation;
            TimeoutMilliseconds = timeoutMilliseconds;
        }
    }
}
