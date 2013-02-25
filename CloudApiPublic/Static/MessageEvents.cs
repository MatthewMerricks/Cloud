//
// MessageEvents.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Static
{
    public delegate void EventMessageArgsHandler(EventMessageArgs e);
    public delegate void SetCountArgsHandler(SetCountArgs e);
    public delegate void IncrementCountArgsHandler(IncrementCountArgs e);
    public delegate void TransferUpdateArgsHandler(TransferUpdateArgs e);

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
        // Holds subscribed EventMessageReceivers.
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
            bool IsError = false,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;
            
            EventMessageArgs newArgs = new EventMessageArgs(Message, Level, IsError, SyncBoxId, DeviceId);

            lock (NewEventMessageLocker)
            {
                if (_newEventMessage == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    _newEventMessage(newArgs);
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
                        foundReceiver.MessageEvents_NewEventMessage(newArgs); // informational or error message occurs
                        foundReceiver.AddStatusMessage(newArgs);

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }

        public static event SetCountArgsHandler DownloadingCountSet
        {
            add
            {
                lock (DownloadingCountSetLocker)
                {
                    _downloadingCountSet += value;
                }
            }
            remove
            {
                lock (DownloadingCountSetLocker)
                {
                    _downloadingCountSet -= value;
                }
            }
        }
        private static event SetCountArgsHandler _downloadingCountSet;
        private static readonly object DownloadingCountSetLocker = new object();
        public static EventHandledLevel SetDownloadingCount(
            uint newCount,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;

            SetCountArgs newArgs = new SetCountArgs(newCount, SyncBoxId, DeviceId);

            lock (DownloadingCountSetLocker)
            {
                if (_downloadingCountSet == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    _downloadingCountSet(newArgs);
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
                        foundReceiver.SetDownloadingCount(newArgs);

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }

        public static event SetCountArgsHandler UploadingCountSet
        {
            add
            {
                lock (UploadingCountSetLocker)
                {
                    _uploadingCountSet += value;
                }
            }
            remove
            {
                lock (UploadingCountSetLocker)
                {
                    _uploadingCountSet -= value;
                }
            }
        }
        private static event SetCountArgsHandler _uploadingCountSet;
        private static readonly object UploadingCountSetLocker = new object();
        public static EventHandledLevel SetUploadingCount(
            uint newCount,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;
            
            SetCountArgs newArgs = new SetCountArgs(newCount, SyncBoxId, DeviceId);

            lock (UploadingCountSetLocker)
            {
                if (_uploadingCountSet == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    _uploadingCountSet(newArgs);
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
                        foundReceiver.SetUploadingCount(newArgs);

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }

        public static event IncrementCountArgsHandler DownloadedCountIncremented
        {
            add
            {
                lock (DownloadedCountIncrementedLocker)
                {
                    _downloadedCountIncremented += value;
                }
            }
            remove
            {
                lock (DownloadedCountIncrementedLocker)
                {
                    _downloadedCountIncremented -= value;
                }
            }
        }
        private static event IncrementCountArgsHandler _downloadedCountIncremented;
        private static readonly object DownloadedCountIncrementedLocker = new object();
        public static EventHandledLevel IncrementDownloadedCount(
            uint incrementAmount = 1,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;
            
            IncrementCountArgs newArgs = new IncrementCountArgs(incrementAmount, SyncBoxId, DeviceId);

            lock (DownloadedCountIncrementedLocker)
            {
                if (_downloadedCountIncremented == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    _downloadedCountIncremented(newArgs);
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
                        foundReceiver.IncrementDownloadedCount(newArgs);

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }

        public static event IncrementCountArgsHandler UploadedCountIncremented
        {
            add
            {
                lock (UploadedCountIncrementedLocker)
                {
                    _uploadedCountIncremented += value;
                }
            }
            remove
            {
                lock (UploadedCountIncrementedLocker)
                {
                    _uploadedCountIncremented -= value;
                }
            }
        }
        private static event IncrementCountArgsHandler _uploadedCountIncremented;
        private static readonly object UploadedCountIncrementedLocker = new object();
        public static EventHandledLevel IncrementUploadedCount(
            uint incrementAmount = 1,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;
            
            IncrementCountArgs newArgs = new IncrementCountArgs(incrementAmount, SyncBoxId, DeviceId);

            lock (UploadedCountIncrementedLocker)
            {
                if (_uploadedCountIncremented == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    _uploadedCountIncremented(newArgs);
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
                        foundReceiver.IncrementUploadedCount(newArgs);

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }

        public static event TransferUpdateArgsHandler FileUploadUpdated
        {
            add
            {
                lock (FileUploadUpdatedLocker)
                {
                    _fileUploadUpdated += value;
                }
            }
            remove
            {
                lock (FileUploadUpdatedLocker)
                {
                    _fileUploadUpdated -= value;
                }
            }
        }
        private static event TransferUpdateArgsHandler _fileUploadUpdated;
        private static readonly object FileUploadUpdatedLocker = new object();
        public static EventHandledLevel UpdateFileUpload(
            long eventId, 
            CLStatusFileTransferUpdateParameters parameters,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;
            
            TransferUpdateArgs newArgs = new TransferUpdateArgs(eventId, parameters, SyncBoxId, DeviceId);

            lock (FileUploadUpdatedLocker)
            {
                if (_fileUploadUpdated == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    _fileUploadUpdated(newArgs);
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
                        foundReceiver.UpdateFileUpload(newArgs);

                        toReturn = (newArgs.Handled
                            ? EventHandledLevel.IsHandled
                            : EventHandledLevel.FiredButNotHandled);
                    }
                }
            }

            return toReturn;
        }

        public static event TransferUpdateArgsHandler FileDownloadUpdated
        {
            add
            {
                lock (FileDownloadUpdatedLocker)
                {
                    _fileDownloadUpdated += value;
                }
            }
            remove
            {
                lock (FileDownloadUpdatedLocker)
                {
                    _fileDownloadUpdated -= value;
                }
            }
        }
        private static event TransferUpdateArgsHandler _fileDownloadUpdated;
        private static readonly object FileDownloadUpdatedLocker = new object();
        public static EventHandledLevel UpdateFileDownload(
            long eventId, 
            CLStatusFileTransferUpdateParameters parameters,
            Nullable<long> SyncBoxId = null,
            string DeviceId = null)
        {
            EventHandledLevel toReturn;
            
            TransferUpdateArgs newArgs = new TransferUpdateArgs(eventId, parameters, SyncBoxId, DeviceId);

            lock (FileDownloadUpdatedLocker)
            {
                if (_fileDownloadUpdated == null)
                {
                    toReturn = EventHandledLevel.NothingFired;
                }
                else
                {
                    _fileDownloadUpdated(newArgs);
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
                        foundReceiver.UpdateFileDownload(newArgs);

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
                    _pathStateChanged(sender, newArgs);
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
                    _fileChangeMergeToStateChanged(sender, newArgs);
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
                    _setBadgeQueued(sender, newArgs);
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
                    _badgePathDeleted(sender, newArgs);
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
                    _badgePathRenamed(sender, newArgs);
                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }
        #endregion
    }
}
