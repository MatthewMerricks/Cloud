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
        NotificationEngineState_Idle = 0,
        NotificationEngineState_Starting,
        NotificationEngineState_Started,
        NotificationEngineState_Cancelled,
        NotificationEngineState_Failed,
    }

    public enum NotificationEngines : uint
    {
        //NotificationEngine_SSE,
        //NotificationEngine_WebSockets,
        NotificationEngine_ManualPolling,
    }

    public delegate void StartEngineTimeout(int timeoutMilliseconds, object userState);
    public delegate void CancelEngineTimeout();
    public delegate void SendManualPoll();

    internal interface ICLNotificationEngine
    {
        int MaxSuccesses { get; }
        int MaxFailures { get; }
        bool Start();
        void TimerExpired(object userState);
    }
}
