//
// MessageEvents.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Static
{
    /// <summary>
    /// Exposes events to receive status notifications from <see cref="CloudApiPublic.CLSync"/>
    /// </summary>
    public static class MessageEvents
    {
        public static event EventHandler<EventMessageArgs> NewEventMessage
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
        private static event EventHandler<EventMessageArgs> _newEventMessage;
        private static readonly object NewEventMessageLocker = new object();
        public static EventHandledLevel FireNewEventMessage(object sender,
            string Message,
            EventMessageLevel Level = EventMessageLevel.Minor,
            bool IsError = false)
        {
            lock (NewEventMessageLocker)
            {
                if (_newEventMessage == null)
                {
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    EventMessageArgs newArgs = new EventMessageArgs(Message, Level, IsError);
                    _newEventMessage(sender, newArgs);
                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }

        public static event EventHandler<SetCountArgs> DownloadingCountSet
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
        private static event EventHandler<SetCountArgs> _downloadingCountSet;
        private static readonly object DownloadingCountSetLocker = new object();
        public static EventHandledLevel SetDownloadingCount(object sender, uint newCount)
        {
            lock (DownloadingCountSetLocker)
            {
                if (_downloadingCountSet == null)
                {
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    SetCountArgs newArgs = new SetCountArgs(newCount);
                    _downloadingCountSet(sender, newArgs);
                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }

        public static event EventHandler<SetCountArgs> UploadingCountSet
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
        private static event EventHandler<SetCountArgs> _uploadingCountSet;
        private static readonly object UploadingCountSetLocker = new object();
        public static EventHandledLevel SetUploadingCount(object sender, uint newCount)
        {
            lock (UploadingCountSetLocker)
            {
                if (_uploadingCountSet == null)
                {
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    SetCountArgs newArgs = new SetCountArgs(newCount);
                    _uploadingCountSet(sender, newArgs);
                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }

        public static event EventHandler<IncrementCountArgs> DownloadedCountIncremented
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
        private static event EventHandler<IncrementCountArgs> _downloadedCountIncremented;
        private static readonly object DownloadedCountIncrementedLocker = new object();
        public static EventHandledLevel IncrementDownloadedCount(object sender, uint incrementAmount = 1)
        {
            lock (DownloadedCountIncrementedLocker)
            {
                if (_downloadedCountIncremented == null)
                {
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    IncrementCountArgs newArgs = new IncrementCountArgs(incrementAmount);
                    _downloadedCountIncremented(sender, newArgs);
                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }

        public static event EventHandler<IncrementCountArgs> UploadedCountIncremented
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
        private static event EventHandler<IncrementCountArgs> _uploadedCountIncremented;
        private static readonly object UploadedCountIncrementedLocker = new object();
        public static EventHandledLevel IncrementUploadedCount(object sender, uint incrementAmount = 1)
        {
            lock (UploadedCountIncrementedLocker)
            {
                if (_uploadedCountIncremented == null)
                {
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    IncrementCountArgs newArgs = new IncrementCountArgs(incrementAmount);
                    _uploadedCountIncremented(sender, newArgs);
                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }

        public static event EventHandler<TransferUpdateArgs> FileUploadUpdated
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
        private static event EventHandler<TransferUpdateArgs> _fileUploadUpdated;
        private static readonly object FileUploadUpdatedLocker = new object();
        public static EventHandledLevel UpdateFileUpload(object sender, long eventId, CLStatusFileTransferUpdateParameters parameters)
        {
            lock (FileUploadUpdatedLocker)
            {
                if (_fileUploadUpdated == null)
                {
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    TransferUpdateArgs newArgs = new TransferUpdateArgs(eventId, parameters);
                    _fileUploadUpdated(sender, newArgs);
                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }

        public static event EventHandler<TransferUpdateArgs> FileDownloadUpdated
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
        private static event EventHandler<TransferUpdateArgs> _fileDownloadUpdated;
        private static readonly object FileDownloadUpdatedLocker = new object();
        public static EventHandledLevel UpdateFileDownload(object sender, long eventId, CLStatusFileTransferUpdateParameters parameters)
        {
            lock (FileDownloadUpdatedLocker)
            {
                if (_fileDownloadUpdated == null)
                {
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    TransferUpdateArgs newArgs = new TransferUpdateArgs(eventId, parameters);
                    _fileDownloadUpdated(sender, newArgs);
                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }

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
    }
}