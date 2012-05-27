//
//  CLBalloonTooltipNotification.cs
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


namespace win_client.Common
{
    public class CLBalloonTooltipNotification
    {
        public string Title;
        public string Text;
        public BalloonIcon IconType;           // the type of the standard icon: None, Info, Warning or Error.
        public Icon CustomIcon;                // if not null, the custom icon to display (overrides iconType above).

        public CLBalloonTooltipNotification()
        {
            throw new NotImplementedException("Default constructor not supported.");
        }

        public CLBalloonTooltipNotification(string title, string text, BalloonIcon iconType, Icon customIcon)
        {
            Title = title;
            Text = text;
            IconType = iconType;
            CustomIcon = customIcon;
        }
    }
}
