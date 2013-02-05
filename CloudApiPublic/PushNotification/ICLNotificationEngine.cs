//  ICLNotificationEngine.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.PushNotification
{
    public enum NotificationEngineStates : uint
    {
        NotificationEngineState_Idle,
        NotificationEngineState_Starting,
        NotificationEngineState_Started,
        NotificationEngineState_Cancelled,
        NotificationEngineState_Failed,
    }

    internal interface ICLNotificationEngine
    {
        NotificationEngineStates State { get; set; }
        void Open();
        void Close();
    }
}
