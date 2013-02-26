//  ICLNotificationEngine.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.PushNotification
{
    public enum NotificationEngines : uint
    {
        NotificationEngine_SSE,
        //NotificationEngine_WebSockets,
        NotificationEngine_ManualPolling,
    }

    public sealed class CLNotificationEvent
    {
        public string Name { get; set; }
        public string Data { get; set; }
        public string Origin { get; set; }
        public string LastEventId { get; set; }

        public CLNotificationEvent()
        {
            Name = string.Empty;
            Data = string.Empty;
            Origin = string.Empty;
            LastEventId = string.Empty;
        }
    }

    public delegate void CreateEngineTimer(object userState);
    public delegate void StartEngineTimeout(int timeoutMilliseconds);
    public delegate void CancelEngineTimeout();
    public delegate void DisposeEngineTimer();
    public delegate void SendManualPoll();
    public delegate void SendNotificationEvent(CLNotificationEvent evt);

    internal interface ICLNotificationEngine
    {
        int MaxSuccesses { get; }
        int MaxFailures { get; }
        bool Start();
        void Stop();
        void TimerExpired(object userState);
    }
}
