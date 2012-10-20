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

        public static event EventHandler<UpdatePathArgs> PathStateChanged
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
        private static event EventHandler<UpdatePathArgs> _pathStateChanged;
        private static readonly object PathStateChangedLocker = new object();
        public static EventHandledLevel SetPathState(object sender, PathState state, FilePath path)
        {
            lock (PathStateChangedLocker)
            {
                if (_pathStateChanged == null)
                {
                    return EventHandledLevel.NothingFired;
                }
                else
                {
                    UpdatePathArgs newArgs = new UpdatePathArgs(state, path);
                    _pathStateChanged(sender, newArgs);
                    return (newArgs.Handled
                        ? EventHandledLevel.IsHandled
                        : EventHandledLevel.FiredButNotHandled);
                }
            }
        }
    }
}