//
// MessageEvents.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using CloudApiPublic.Model.EventMessages;
using CloudApiPublic.Model.EventMessages.ErrorInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Static
{
    public delegate void EventMessageArgsHandler(EventMessageArgs e);

    /// <summary>
    /// Exposes events to receive status notifications from <see cref="CloudApiPublic.CLSyncEngine"/>
    /// </summary>
    public static class MessageEvents
    {
        #region IEventMessageReceiver subscription
        public static CLError SubscribeMessageReceiver(long SyncBoxId, string DeviceId, IEventMessageReceiver receiver)
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

                string receiverKey = SyncBoxId.ToString() + " " + DeviceId;
                lock (InternalReceivers)
                {
                    InternalReceivers[receiverKey] = receiver;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        public static CLError UnsubscribeMessageReceiver(long SyncBoxId, string DeviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(DeviceId))
                {
                    throw new NullReferenceException("DeviceId cannot be null");
                }

                string receiverKey = SyncBoxId.ToString() + " " + DeviceId;
                lock (InternalReceivers)
                {
                    if (!InternalReceivers.Remove(receiverKey))
                    {
                        throw new ArgumentException("Receiver with given SyncBoxId and DeviceId not found to unsubscribe");
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        // Holds subscribed EventMessageReceivers. <-- incomplete comment???
        private static readonly Dictionary<string, IEventMessageReceiver> InternalReceivers = new Dictionary<string, IEventMessageReceiver>(StringComparer.InvariantCulture);
        #endregion

        #region public events
        public static event EventMessageArgsHandler NewEventMessage
        {
            add
            {
                lock (NewEventMessageLocker)
                {
                    _newEventMessage += value;
                }
            }
            remove
            {
                lock (NewEventMessageLocker)
                {
                    _newEventMessage -= value;
                }
            }
        }
        private static event EventMessageArgsHandler _newEventMessage;
        private static readonly object NewEventMessageLocker = new object();
        public static EventHandledLevel FireNewEventMessage(
            string Message,
            EventMessageLevel Level = EventMessageLevel.Minor,
            BaseErrorInfo Error = null,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;
            
            EventMessageArgs newArgs = new EventMessageArgs(
                (Error != null
                    ? new ErrorMessage(
                        Message,
                        Level,
                        Error,
                        SyncBoxId,
                        DeviceId)
                    : (BaseMessage)new InformationalMessage(
                        Message,
                        Level,
                        SyncBoxId,
                        DeviceId)));

            lock (NewEventMessageLocker)
            {
                if (_newEventMessage == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    try
                    {
                        _newEventMessage(newArgs);
                    }
                    catch
                    {
                    }
                    toReturn = (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }

            if (SyncBoxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                string receiverKey = ((long)SyncBoxId).ToString() + " " + DeviceId;
                lock (InternalReceivers)
                {
                    IEventMessageReceiver foundReceiver;
                    if (InternalReceivers.TryGetValue(receiverKey, out foundReceiver))
                    {
                        try
                        {
                            BasicMessage newArgsMessage = new BasicMessage(newArgs); // informational or error message occurs
                            foundReceiver.MessageEvents_NewEventMessage(newArgsMessage);
                            foundReceiver.AddStatusMessage(newArgsMessage);
                        }
                        catch
                        {
                        }

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }
        public static EventHandledLevel SetDownloadingCount(
            uint newCount,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;

            EventMessageArgs newArgs = new EventMessageArgs(
                new DownloadingCountMessage(
                    newCount,
                    SyncBoxId,
                    DeviceId));

            lock (NewEventMessageLocker)
            {
                if (_newEventMessage == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    try
                    {
                        _newEventMessage(newArgs);
                    }
                    catch
                    {
                    }
                    toReturn = (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }

            if (SyncBoxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                string receiverKey = ((long)SyncBoxId).ToString() + " " + DeviceId;
                lock (InternalReceivers)
                {
                    IEventMessageReceiver foundReceiver;
                    if (InternalReceivers.TryGetValue(receiverKey, out foundReceiver))
                    {
                        try
                        {
                            foundReceiver.SetDownloadingCount(new SetCountMessage(newArgs));
                        }
                        catch
                        {
                        }

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }
        public static EventHandledLevel SetUploadingCount(
            uint newCount,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;

            EventMessageArgs newArgs = new EventMessageArgs(
                new UploadingCountMessage(
                    newCount,
                    SyncBoxId,
                    DeviceId));

            lock (NewEventMessageLocker)
            {
                if (_newEventMessage == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    try
                    {
                        _newEventMessage(newArgs);
                    }
                    catch
                    {
                    }

                    toReturn = (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }

            if (SyncBoxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                string receiverKey = ((long)SyncBoxId).ToString() + " " + DeviceId;
                lock (InternalReceivers)
                {
                    IEventMessageReceiver foundReceiver;
                    if (InternalReceivers.TryGetValue(receiverKey, out foundReceiver))
                    {
                        try
                        {
                            foundReceiver.SetUploadingCount(new SetCountMessage(newArgs));
                        }
                        catch
                        {
                        }

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }
        public static EventHandledLevel IncrementDownloadedCount(
            uint incrementAmount = 1,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;

            EventMessageArgs newArgs = new EventMessageArgs(
                new SuccessfulDownloadsIncrementedMessage(
                    incrementAmount,
                    SyncBoxId,
                    DeviceId));

            lock (NewEventMessageLocker)
            {
                if (_newEventMessage == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    try
                    {
                        _newEventMessage(newArgs);
                    }
                    catch
                    {
                    }

                    toReturn = (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }

            if (SyncBoxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                string receiverKey = ((long)SyncBoxId).ToString() + " " + DeviceId;
                lock (InternalReceivers)
                {
                    IEventMessageReceiver foundReceiver;
                    if (InternalReceivers.TryGetValue(receiverKey, out foundReceiver))
                    {
                        try
                        {
                            foundReceiver.IncrementDownloadedCount(new IncrementCountMessage(newArgs));
                        }
                        catch
                        {
                        }

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }
        public static EventHandledLevel IncrementUploadedCount(
            uint incrementAmount = 1,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;

            EventMessageArgs newArgs = new EventMessageArgs(
                new SuccessfulUploadsIncrementedMessage(
                    incrementAmount,
                    SyncBoxId,
                    DeviceId));

            lock (NewEventMessageLocker)
            {
                if (_newEventMessage == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    try
                    {
                        _newEventMessage(newArgs);
                    }
                    catch
                    {
                    }

                    toReturn = (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }

            if (SyncBoxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                string receiverKey = ((long)SyncBoxId).ToString() + " " + DeviceId;
                lock (InternalReceivers)
                {
                    IEventMessageReceiver foundReceiver;
                    if (InternalReceivers.TryGetValue(receiverKey, out foundReceiver))
                    {
                        try
                        {
                            foundReceiver.IncrementUploadedCount(new IncrementCountMessage(newArgs));
                        }
                        catch
                        {
                        }

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }
        public static EventHandledLevel UpdateFileUpload(
            long eventId, 
            CLStatusFileTransferUpdateParameters parameters,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;

            EventMessageArgs newArgs = new EventMessageArgs(
                new UploadProgressMessage(
                    parameters,
                    eventId,
                    SyncBoxId,
                    DeviceId));

            lock (NewEventMessageLocker)
            {
                if (_newEventMessage == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    try
                    {
                        _newEventMessage(newArgs);
                    }
                    catch
                    {
                    }

                    toReturn = (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }

            if (SyncBoxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                string receiverKey = ((long)SyncBoxId).ToString() + " " + DeviceId;
                lock (InternalReceivers)
                {
                    IEventMessageReceiver foundReceiver;
                    if (InternalReceivers.TryGetValue(receiverKey, out foundReceiver))
                    {
                        try
                        {
                            foundReceiver.UpdateFileUpload(new TransferUpdateMessage(newArgs));
                        }
                        catch
                        {
                        }

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }
        public static EventHandledLevel UpdateFileDownload(
            long eventId, 
            CLStatusFileTransferUpdateParameters parameters,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;

            EventMessageArgs newArgs = new EventMessageArgs(
                new DownloadProgressMessage(
                    parameters,
                    eventId,
                    SyncBoxId,
                    DeviceId));

            lock (NewEventMessageLocker)
            {
                if (_newEventMessage == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    try
                    {
                        _newEventMessage(newArgs);
                    }
                    catch
                    {
                    }

                    toReturn = (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }

            if (SyncBoxId != null
                && !string.IsNullOrEmpty(DeviceId))
            {
                string receiverKey = ((long)SyncBoxId).ToString() + " " + DeviceId;
                lock (InternalReceivers)
                {
                    IEventMessageReceiver foundReceiver;
                    if (InternalReceivers.TryGetValue(receiverKey, out foundReceiver))
                    {
                        try
                        {
                            foundReceiver.UpdateFileDownload(new TransferUpdateMessage(newArgs));
                        }
                        catch
                        {
                        }

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }
        #endregion

        #region internal events
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
