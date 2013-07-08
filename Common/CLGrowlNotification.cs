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
        public bool WasClosed { get; private set; }

        public CLGrowlNotification(UIElement wpfControl, PopupAnimation animation, int? timeoutMilliseconds)
        {
            WpfControl = wpfControl;
            Animation = animation;
            TimeoutMilliseconds = timeoutMilliseconds;
        }

        public void TriggerClose()
        {
            lock (this)
            {
                if (!WasClosed)
                {
                    if (NeedsClose != null)
                    {
                        NeedsClose(this, EventArgs.Empty);
                    }
                    WasClosed = true;
                }
            }
        }

        public event EventHandler NeedsClose;
    }
}