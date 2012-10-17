//
//  CLStatusFileTransferBlank.cs
//  Cloud Windows
//
//  Created by DavidBruck.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace CloudApiPrivate.EventMessageReceiver.Status
{
    public sealed class CLStatusFileTransferBlank : CLStatusFileTransferBase<CLStatusFileTransferBlank>
    {
        public override Visibility Visibility
        {
            get { return Visibility.Hidden; }
        }
    }
}