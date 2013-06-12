//
// MessageEvents.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.CLSync;
using Cloud.Interfaces;
using Cloud.Model;
using Cloud.Model.EventMessages;
using Cloud.Model.EventMessages.ErrorInfo;
using Cloud.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Cloud.Static
{
    public delegate void EventMessageArgsHandler(EventMessageArgs e);

    /// <summary>
    /// Exposes events to receive status notifications from <see cref="Cloud.CLSyncEngine"/>
    /// </summary>
    public static class MessageEvents
    {
        private const int removeHandlerTimeoutMilliseconds = 10000;

        #region IEventMessageReceiver subscription
        internal static CLError SubscribeMessageReceiver(long SyncboxId, string DeviceId, IEventMessageReceiver receiver)
        {
            try
            {
                if (receiver == null)
                {
                    throw new NullReferenceException("receiver cannot be null");
                }
                if (string.IsNullOrEmpty(DeviceId))
                {
                    throw new NullReferenceException("DeviceId cannot be null");
                }

                string receiverKey = SyncboxId.ToString() + " " + DeviceId;

                lock (_internalReceiversHandlers)
                {
                    if (!_internalReceiversHandlers.ContainsKey(receiverKey))
                    {
                        _internalReceiversHandlers.Add(receiverKey, new Tuple<IEventMessageReceiver, ManualResetEvent>(receiver, new ManualResetEvent(true)));
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        internal static CLError UnsubscribeMessageReceiver(long SyncboxId, string DeviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(DeviceId))
                {
                    throw new NullReferenceException("DeviceId cannot be null");
                }

                string receiverKey = SyncboxId.ToString() + " " + DeviceId;

                ManualResetEvent syncEvent = null;

                lock (_internalReceiversHandlers)
                {
                    Tuple<IEventMessageReceiver, ManualResetEvent> data_;
                    if (_internalReceiversHandlers.TryGetValue(receiverKey, out data_))
                    {
                        syncEvent = data_.Item2;
                        _internalReceiversHandlers.Remove(receiverKey);
                    }
                }

                if (syncEvent != null)
                {
                    bool needsWait;

                    lock (_internalReceiversThreads)
                    {
                        Thread executingThread;
                        needsWait = !_internalReceiversThreads.TryGetValue(syncEvent, out executingThread)
                                    || executingThread != Thread.CurrentThread;
                    }

                    if (needsWait
                        && !syncEvent.WaitOne(removeHandlerTimeoutMilliseconds))
                    {
                        throw new TimeoutException("Unable to synchronize removing event handler");
                    }
                }

            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        /// <summary>
        ///  every unique syncboxId-deviceId pair has exactly one event message receiver associated;
        ///  one event message receiver can handle messages for more than one syncboxId-deviceId unique pair;
        ///  event stream for every unique syncboxId-deviceId pair is guaranteed to be consistent:
        ///     - no call for a unique syncboxId-deviceId pair shall be made before the previous one has returned;
        ///     - unsubscribe for a unique syncboxId-deviceId pair is guaranteed to return after all queued before the time of the call events are handled;
        /// </summary>
        private static readonly Dictionary<string, Tuple<IEventMessageReceiver, ManualResetEvent>> _internalReceiversHandlers = new Dictionary<string, Tuple<IEventMessageReceiver, ManualResetEvent>>();
        private static readonly Dictionary<ManualResetEvent, Thread> _internalReceiversThreads = new Dictionary<ManualResetEvent, Thread>();
        #endregion

        #region IEventMessageReceiver helpers
        private static void FireInternalReceiverInternal(long syncboxId, string deviceId, EventMessageArgs newArgs, ref EventHandledLevel toReturn,
                                                            Action<IEventMessageReceiver, EventMessageArgs> action)
        {
            string receiverKey = syncboxId.ToString() + " " + deviceId;

            WaitCallback closure = null;

            lock (_internalReceiversHandlers)
            {
                Tuple<IEventMessageReceiver, ManualResetEvent> data_;
                if (_internalReceiversHandlers.TryGetValue(receiverKey, out data_))
                {
                    IEventMessageReceiver handler = data_.Item1;
                    ManualResetEvent syncEvent = new ManualResetEvent(false);
                    ManualResetEvent prevSyncEvent = data_.Item2;
                    _internalReceiversHandlers[receiverKey] = new Tuple<IEventMessageReceiver, ManualResetEvent>(handler, syncEvent);

                    closure = (object state) =>
                    {
                        prevSyncEvent.WaitOne();
                        try
                        {
                            lock (_internalReceiversThreads)
                            {
                                _internalReceiversThreads[syncEvent] = Thread.CurrentThread;
                            }
                            action(handler, newArgs);
                        }
                        catch
                        {
                        }
                        finally
                        {
                            lock (_internalReceiversThreads)
                            {
                                _internalReceiversThreads.Remove(syncEvent);
                            }
                        }
                        syncEvent.Set();
                    };
                }
            }

            if (closure != null)
            {
                try
                {
                    closure(null);
                }
                catch
                {
                }
                toReturn = (newArgs.Handled
                    ? EventHandledLevel.IsHandled
                    : EventHandledLevel.FiredButNotHandled);
            }
        }

        private static void FireAllInternalReceiversInternal(EventMessageArgs newArgs, ref EventHandledLevel toReturn,
                                                                Action<IEventMessageReceiver, EventMessageArgs> action)
        {
            List<WaitCallback> closures = new List<WaitCallback>();

            lock (_internalReceiversHandlers)
            {
                foreach (string receiverKey in _internalReceiversHandlers.Keys.ToArray())
                {
                    Tuple<IEventMessageReceiver, ManualResetEvent> data_ = _internalReceiversHandlers[receiverKey];
                    IEventMessageReceiver handler = data_.Item1;
                    ManualResetEvent syncEvent = new ManualResetEvent(false);
                    ManualResetEvent prevSyncEvent = data_.Item2;
                    _internalReceiversHandlers[receiverKey] = new Tuple<IEventMessageReceiver, ManualResetEvent>(handler, syncEvent);

                    closures.Add((object state) =>
                    {
                        prevSyncEvent.WaitOne();
                        try
                        {
                            lock (_internalReceiversThreads)
                            {
                                _internalReceiversThreads[syncEvent] = Thread.CurrentThread;
                            }
                            action(handler, newArgs);
                        }
                        catch
                        {
                        }
                        finally
                        {
                            lock (_internalReceiversThreads)
                            {
                                _internalReceiversThreads.Remove(syncEvent);
                            }
                        }
                        syncEvent.Set();
                    });
                }
            }

            if (closures.Count != 0)
            {
                try
                {
                    foreach (WaitCallback closure in closures)
                    {
                        closure(null);
                    }
                }
                catch
                {
                }
                toReturn = (newArgs.Handled
                    ? EventHandledLevel.IsHandled
                    : EventHandledLevel.FiredButNotHandled);
            }
        }
        #endregion

        #region public events
        public static event EventMessageArgsHandler NewEventMessage
        {
            add
            {
                lock (_newEventMessageHandlers)
                {
                    if (!_newEventMessageHandlers.ContainsKey(value))
                    {
                        _newEventMessageHandlers.Add(value, new ManualResetEvent(true));
                    }
                }
            }
            remove
            {
                ManualResetEvent syncEvent = null;

                lock (_newEventMessageHandlers)
                {
                    ManualResetEvent syncEvent_;
                    if (_newEventMessageHandlers.TryGetValue(value, out syncEvent_))
                    {
                        syncEvent = syncEvent_;
                        _newEventMessageHandlers.Remove(value);
                    }
                }

                if (syncEvent != null)
                {
                    bool needsWait;

                    lock (_newEventMessageThreads)
                    {
                        Thread executingThread;
                        needsWait = !_newEventMessageThreads.TryGetValue(syncEvent, out executingThread)
                                    || executingThread != Thread.CurrentThread;
                    }

                    if (needsWait
                        && !syncEvent.WaitOne(removeHandlerTimeoutMilliseconds))
                    {
                        throw new TimeoutException("Unable to synchronize removing event handler"); 
                    }
                }
            }
        }
        private static readonly Dictionary<EventMessageArgsHandler, ManualResetEvent> _newEventMessageHandlers = new Dictionary<EventMessageArgsHandler, ManualResetEvent>();
        private static readonly Dictionary<ManualResetEvent, Thread> _newEventMessageThreads = new Dictionary<ManualResetEvent, Thread>();

        /// <summary>
        /// This is a helper for invoking synchronously the generic NewEventMessage event; Event handlers are executed in the context of the calling thread;
        /// </summary>
        /// <param name="newArgs">the generic event message args holder; the concrete message is in newArgs.Message</param>
        /// <param name="toReturn">summary on the handled status as reported in newArgs.Handled</param>
        private static void FireNewEventMessageInternal(EventMessageArgs newArgs, ref EventHandledLevel toReturn)
        {
            List<WaitCallback> closures = new List<WaitCallback>();

            lock (_newEventMessageHandlers)
            {
                EventMessageArgsHandler[] handlers = _newEventMessageHandlers.Keys.ToArray();
                for (int i = 0; i < handlers.Length; ++i)
                {
                    EventMessageArgsHandler handler = handlers[i];
                    ManualResetEvent syncEvent = new ManualResetEvent(false);
                    ManualResetEvent prevSyncEvent = _newEventMessageHandlers[handler];
                    _newEventMessageHandlers[handler] = syncEvent;

                    closures.Add((object state) =>
                    {
                        prevSyncEvent.WaitOne();
                        try
                        {
                            lock (_newEventMessageThreads)
                            {
                                _newEventMessageThreads[syncEvent] = Thread.CurrentThread;
                            }
                            handler(newArgs);
                        }
                        catch
                        {
                        }
                        finally
                        {
                            lock (_newEventMessageThreads)
                            {
                                _newEventMessageThreads.Remove(syncEvent);
                            }
                        }
                        syncEvent.Set();
                    });
                }
            }

            if (closures.Count != 0)
            {
                try
                {
                    foreach (WaitCallback closure in closures)
                    {
                        closure(null);
                    }
                }
                catch
                {
                }
                toReturn = (newArgs.Handled
                    ? EventHandledLevel.IsHandled
                    : EventHandledLevel.FiredButNotHandled);
            }
        }

        public static EventHandledLevel FireNewEventMessage(
            string Message,
            EventMessageLevel Level = EventMessageLevel.Minor,
            BaseErrorInfo Error = null,
            Nullable<long> SyncboxId = null,
            string DeviceId = null)
        {
            if (Error != null
                && Error.ErrorType == ErrorMessageType.HaltAllOfCloudSDK)
            {
                Helpers.HaltAllOnUnrecoverableError();

                string stack;
                try
                {
                    stack = (new System.Diagnostics.StackTrace(fNeedFileInfo: true)).ToString();
                }
                catch (Exception ex)
                {
                    try
                    {
                        stack = ex.StackTrace;
                    }
                    catch
                    {
                        stack = "Unable to retrieve StackTrace";
                    }
                }

                CLTrace.Instance.writeToLog(1, "Helpers: HaltAllOnUnrecoverableError: ERROR: Sync engine halted.  Msg: {0}. Error: {1}. Stack trace: {2}.",
                    Message, Error == null ? "null" : Error.ErrorType.ToString(), stack);
            }

            EventHandledLevel toReturn = EventHandledLevel.NothingFired;
            
            EventMessageArgs newArgs = new EventMessageArgs(
                (Error != null
                    ? new ErrorMessage(
                        Message,
                        Level,
                        Error,
                        SyncboxId,
                        DeviceId)
                    : (BaseMessage)new InformationalMessage(
                        Message,
                        Level,
                        SyncboxId,
                        DeviceId)));

            FireNewEventMessageInternal(newArgs, ref toReturn);

            if (SyncboxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                FireInternalReceiverInternal((long)SyncboxId, DeviceId, newArgs, ref toReturn, 
                    (IEventMessageReceiver handler, EventMessageArgs newArgs_) => {
                        BaseMessageArgs newArgsMessage = new BaseMessageArgs(newArgs_); // informational or error message occurs
                        handler.MessageEvents_NewEventMessage(newArgsMessage);
                        handler.AddStatusMessage(newArgsMessage);
                    });
            }
            else if (Error != null
                && Error.ErrorType == ErrorMessageType.HaltAllOfCloudSDK)
            {
                FireAllInternalReceiversInternal(newArgs, ref toReturn,
                    (IEventMessageReceiver handler, EventMessageArgs newArgs_) => {
                        BaseMessageArgs newArgsMessage = new BaseMessageArgs(newArgs_); // severe halt all error message
                        handler.MessageEvents_NewEventMessage(newArgsMessage);
                        handler.AddStatusMessage(newArgsMessage);
                    });
            }

            return toReturn;
        }
        public static EventHandledLevel SetDownloadingCount(
            uint newCount,
            Nullable<long> SyncboxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn = EventHandledLevel.NothingFired;

            EventMessageArgs newArgs = new EventMessageArgs(
                new DownloadingCountMessage(
                    newCount,
                    SyncboxId,
                    DeviceId));

            FireNewEventMessageInternal(newArgs, ref toReturn);

            if (SyncboxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                FireInternalReceiverInternal((long)SyncboxId, DeviceId, newArgs, ref toReturn,
                    (IEventMessageReceiver handler, EventMessageArgs newArgs_) => {
                        handler.SetDownloadingCount(new SetCountMessageArgs(newArgs_));
                    });
            }

            return toReturn;
        }
        public static EventHandledLevel SetUploadingCount(
            uint newCount,
            Nullable<long> SyncboxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn = EventHandledLevel.NothingFired;

            EventMessageArgs newArgs = new EventMessageArgs(
                new UploadingCountMessage(
                    newCount,
                    SyncboxId,
                    DeviceId));

            FireNewEventMessageInternal(newArgs, ref toReturn);

            if (SyncboxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                FireInternalReceiverInternal((long)SyncboxId, DeviceId, newArgs, ref toReturn,
                    (IEventMessageReceiver handler, EventMessageArgs newArgs_) =>
                    {
                        handler.SetUploadingCount(new SetCountMessageArgs(newArgs_));
                    });
            }

            return toReturn;
        }
        public static EventHandledLevel IncrementDownloadedCount(
            uint incrementAmount = 1,
            Nullable<long> SyncboxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn = EventHandledLevel.NothingFired;

            EventMessageArgs newArgs = new EventMessageArgs(
                new SuccessfulDownloadsIncrementedMessage(
                    incrementAmount,
                    SyncboxId,
                    DeviceId));

            FireNewEventMessageInternal(newArgs, ref toReturn);

            if (SyncboxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                FireInternalReceiverInternal((long)SyncboxId, DeviceId, newArgs, ref toReturn,
                    (IEventMessageReceiver handler, EventMessageArgs newArgs_) =>
                    {
                        handler.IncrementDownloadedCount(new IncrementCountMessageArgs(newArgs_));
                    });
            }

            return toReturn;
        }
        public static EventHandledLevel IncrementUploadedCount(
            uint incrementAmount = 1,
            Nullable<long> SyncboxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn = EventHandledLevel.NothingFired;

            EventMessageArgs newArgs = new EventMessageArgs(
                new SuccessfulUploadsIncrementedMessage(
                    incrementAmount,
                    SyncboxId,
                    DeviceId));

            FireNewEventMessageInternal(newArgs, ref toReturn);

            if (SyncboxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                FireInternalReceiverInternal((long)SyncboxId, DeviceId, newArgs, ref toReturn,
                    (IEventMessageReceiver handler, EventMessageArgs newArgs_) =>
                    {
                        handler.IncrementUploadedCount(new IncrementCountMessageArgs(newArgs_));
                    });
            }

            return toReturn;
        }
        public static EventHandledLevel UpdateFileUpload(
            long eventId, 
            CLStatusFileTransferUpdateParameters parameters,
            Nullable<long> SyncboxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn = EventHandledLevel.NothingFired;

            EventMessageArgs newArgs = new EventMessageArgs(
                new UploadProgressMessage(
                    parameters,
                    eventId,
                    SyncboxId,
                    DeviceId));

            FireNewEventMessageInternal(newArgs, ref toReturn);

            if (SyncboxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                FireInternalReceiverInternal((long)SyncboxId, DeviceId, newArgs, ref toReturn,
                    (IEventMessageReceiver handler, EventMessageArgs newArgs_) =>
                    {
                        handler.UpdateFileUpload(new TransferUpdateMessageArgs(newArgs_));
                    });
            }

            return toReturn;
        }
        public static EventHandledLevel UpdateFileDownload(
            long eventId, 
            CLStatusFileTransferUpdateParameters parameters,
            Nullable<long> SyncboxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn = EventHandledLevel.NothingFired;

            EventMessageArgs newArgs = new EventMessageArgs(
                new DownloadProgressMessage(
                    parameters,
                    eventId,
                    SyncboxId,
                    DeviceId));

            FireNewEventMessageInternal(newArgs, ref toReturn);

            if (SyncboxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                FireInternalReceiverInternal((long)SyncboxId, DeviceId, newArgs, ref toReturn,
                    (IEventMessageReceiver handler, EventMessageArgs newArgs_) =>
                    {
                        handler.UpdateFileDownload(new TransferUpdateMessageArgs(newArgs_));
                    });
            }

            return toReturn;
        }
        public static EventHandledLevel DetectedInternetConnectivityChange(
            bool internetConnected)
        {
            EventHandledLevel toReturn = EventHandledLevel.NothingFired;

            EventMessageArgs newArgs = new EventMessageArgs(
                new InternetChangeMessage(internetConnected));

            FireNewEventMessageInternal(newArgs, ref toReturn);

            FireAllInternalReceiversInternal(newArgs, ref toReturn,
                (IEventMessageReceiver handler, EventMessageArgs newArgs_) =>
                {
                    handler.InternetConnectivityChanged(new InternetChangeMessageArgs(newArgs_));
                });

            return toReturn;
        }

        public static EventHandledLevel DetectedDownloadCompleteChange(
            long eventId,
            CLFileItem fileItem,
            Nullable<long> SyncboxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn = EventHandledLevel.NothingFired;

            EventMessageArgs newArgs = new EventMessageArgs(
                new DownloadCompleteMessage(eventId, fileItem, SyncboxId, DeviceId));

            FireNewEventMessageInternal(newArgs, ref toReturn);

            FireAllInternalReceiversInternal(newArgs, ref toReturn,
                (IEventMessageReceiver handler, EventMessageArgs newArgs_) =>
                {
                    handler.DownloadCompleteChanged(new DownloadCompleteMessageArgs(newArgs_));
                });

            return toReturn;
        }

        public static EventHandledLevel DetectedStorageQuotaExceededChange(
            Nullable<long> SyncboxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn = EventHandledLevel.NothingFired;

            EventMessageArgs newArgs = new EventMessageArgs(
                new StorageQuotaExceededMessage(SyncboxId, DeviceId));

            FireNewEventMessageInternal(newArgs, ref toReturn);

            FireAllInternalReceiversInternal(newArgs, ref toReturn,
                (IEventMessageReceiver handler, EventMessageArgs newArgs_) =>
                {
                    handler.StorageQuotaExceededChanged(new StorageQuotaExceededMessageArgs(newArgs_));
                });

            return toReturn;
        }
        #endregion


        #region internal events

        // Note: Internal events do not have to be as generic and protected as the external 
        //      since the implementation has full visibility and control on their use

        internal static event EventHandler<SetBadgeQueuedArgs> PathStateChanged
        {
            add
            {
                lock (PathStateChangedLocker)
                {
                    _pathStateChanged += value;
                }
            }
            remove
            {
                lock (PathStateChangedLocker)
                {
                    _pathStateChanged -= value;
                }
            }
        }
        private static event EventHandler<SetBadgeQueuedArgs> _pathStateChanged;
        private static readonly object PathStateChangedLocker = new object();
        internal static EventHandledLevel SetPathState(object sender, SetBadge badgeChange)
        {
            lock (PathStateChangedLocker)
            {
                if (_pathStateChanged == null)
                {
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    SetBadgeQueuedArgs newArgs = new SetBadgeQueuedArgs(badgeChange);

                    try
                    {
                        _pathStateChanged(sender, newArgs);
                    }
                    catch
                    {
                    }

                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }

        internal static event EventHandler<FileChangeMergeToStateArgs> FileChangeMergeToStateChanged
        {
            add
            {
                lock (FileChangeMergeToStateChangedLocker)
                {
                    _fileChangeMergeToStateChanged += value;
                }
            }
            remove
            {
                lock (FileChangeMergeToStateChangedLocker)
                {
                    _fileChangeMergeToStateChanged -= value;
                }
            }
        }
        private static event EventHandler<FileChangeMergeToStateArgs> _fileChangeMergeToStateChanged;
        private static readonly object FileChangeMergeToStateChangedLocker = new object();
        internal static EventHandledLevel ApplyFileChangeMergeToChangeState(object sender, FileChangeMerge mergedFileChanges)
        {
            lock (FileChangeMergeToStateChangedLocker)
            {
                if (_fileChangeMergeToStateChanged == null)
                {
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    FileChangeMergeToStateArgs newArgs = new FileChangeMergeToStateArgs(mergedFileChanges);

                    try
                    {
                        _fileChangeMergeToStateChanged(sender, newArgs);
                    }
                    catch
                    {
                    }

                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }

        internal static event EventHandler<SetBadgeQueuedArgs> SetBadgeQueued
        {
            add
            {
                lock (SetBadgeQueuedLocker)
                {
                    _setBadgeQueued += value;
                }
            }
            remove
            {
                lock (SetBadgeQueuedLocker)
                {
                    _setBadgeQueued -= value;
                }
            }
        }
        private static event EventHandler<SetBadgeQueuedArgs> _setBadgeQueued;
        private static readonly object SetBadgeQueuedLocker = new object();
        internal static EventHandledLevel QueueSetBadge(object sender, SetBadge badgeChange)
        {
            lock (SetBadgeQueuedLocker)
            {
                if (_setBadgeQueued == null)
                {
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    SetBadgeQueuedArgs newArgs = new SetBadgeQueuedArgs(badgeChange);

                    try
                    {
                        _setBadgeQueued(sender, newArgs);
                    }
                    catch
                    {
                    }

                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }

        internal static event EventHandler<BadgePathDeletedArgs> BadgePathDeleted
        {
            add
            {
                lock (BadgePathDeletedLocker)
                {
                    _badgePathDeleted += value;
                }
            }
            remove
            {
                lock (BadgePathDeletedLocker)
                {
                    _badgePathDeleted -= value;
                }
            }
        }
        private static event EventHandler<BadgePathDeletedArgs> _badgePathDeleted;
        private static readonly object BadgePathDeletedLocker = new object();
        internal static EventHandledLevel DeleteBadgePath(object sender, DeleteBadgePath badgePathDeleted, out bool isDeleted)
        {
            lock (BadgePathDeletedLocker)
            {
                if (_badgePathDeleted == null)
                {
                    isDeleted = false;
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    BadgePathDeletedArgs newArgs = new BadgePathDeletedArgs(badgePathDeleted);

                    try
                    {
                        _badgePathDeleted(sender, newArgs);
                    }
                    catch
                    {
                    }

                    isDeleted = newArgs.IsDeleted;
                    return (newArgs.IsDeleted
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }

        internal static event EventHandler<BadgePathRenamedArgs> BadgePathRenamed
        {
            add
            {
                lock (BadgePathRenamedLocker)
                {
                    _badgePathRenamed += value;
                }
            }
            remove
            {
                lock (BadgePathRenamedLocker)
                {
                    _badgePathRenamed -= value;
                }
            }
        }
        private static event EventHandler<BadgePathRenamedArgs> _badgePathRenamed;
        private static readonly object BadgePathRenamedLocker = new object();
        internal static EventHandledLevel RenameBadgePath(object sender, RenameBadgePath badgeRename)
        {
            lock (BadgePathRenamedLocker)
            {
                if (_badgePathRenamed == null)
                {
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    BadgePathRenamedArgs newArgs = new BadgePathRenamedArgs(badgeRename);

                    try
                    {
                        _badgePathRenamed(sender, newArgs);
                    }
                    catch
                    {
                    }

                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }
        #endregion
    }
}
