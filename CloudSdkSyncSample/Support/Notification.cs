using System;

namespace SampleLiveSync.Support
{
    public class NotificationEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    public class NotificationEventArgs<TOutgoing> : NotificationEventArgs
    {
        public TOutgoing Data { get; set; }
    }

    public class NotificationEventArgs<TOutgoing, TIncoming> : NotificationEventArgs<TOutgoing>
    {
        // Completion callback
        public Action<TIncoming> Completed { get; set; }
    }
}
