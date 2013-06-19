//
// MessageEvents.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.Model.EventMessages;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Interfaces
{
    /// <summary>
    /// Interface for implementing a view model for sync status, see EventMessageReceiver in SampleLiveSync code for sample implementation
    /// </summary>
    public interface IEventMessageReceiver
    {
        /// <summary>
        /// Fired when NewEventMessage event fires in Cloud.Static.MessageEvents, filtered down to a specific Syncbox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void MessageEvents_NewEventMessage(IBasicMessage e);
        /// <summary>
        /// Fired when NewEventMessage event fires in Cloud.Static.MessageEvents, filtered down to a specific Syncbox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void AddStatusMessage(IBasicMessage e);
        /// <summary>
        /// Fired when DownloadingCountSet event fires in Cloud.Static.MessageEvents, filtered down to a specific Syncbox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void SetDownloadingCount(ISetCountMessage e);
        /// <summary>
        /// Fired when DownloadingCountSet event fires in Cloud.Static.MessageEvents, filtered down to a specific Syncbox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void SetUploadingCount(ISetCountMessage e);
        /// <summary>
        /// Fired when DownloadedCountIncremented event fires in Cloud.Static.MessageEvents, filtered down to a specific Syncbox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void IncrementDownloadedCount(IIncrementCountMessage e);
        /// <summary>
        /// Fired when UploadedCountIncremented event fires in Cloud.Static.MessageEvents, filtered down to a specific Syncbox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void IncrementUploadedCount(IIncrementCountMessage e);
        /// <summary>
        /// Fired when FileUploadUpdated event fires in Cloud.Static.MessageEvents, filtered down to a specific Syncbox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void UpdateFileUpload(ITransferUpdateMessage e);
        /// <summary>
        /// Fired when FileDownloadUpdated event fires in Cloud.Static.MessageEvents, filtered down to a specific Syncbox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void UpdateFileDownload(ITransferUpdateMessage e);
        /// <summary>
        /// Fired when internet connectivity changes as detected when any Syncboxes are actively syncing
        /// </summary>
        /// <param name="e">Message parameters</param>
        void InternetConnectivityChanged(IInternetConnectivityMessage e);
        /// <summary>
        /// Fired when a file download is complete.
        /// </summary>
        /// <param name="e">Message parameters</param>
        void DownloadCompleteChanged(IDownloadCompleteMessage e);
        /// <summary>
        /// Fired when a file upload is complete.
        /// </summary>
        /// <param name="e">Message parameters</param>
        void UploadCompleteChanged(IUploadCompleteMessage e);
        /// <summary>
        /// Fired when storage quota has been exceeded.
        /// </summary>
        /// <param name="e">Message parameters</param>
        void StorageQuotaExceededChanged(IStorageQuotaExceededMessage e);
        /// <summary>
        /// Fired when live syncing has started on a syncbox.
        /// </summary>
        /// <param name="e">Message parameters</param>
        void SyncboxDidStartLiveSyncChanged(ISyncboxDidStartLiveSyncMessage e);
        /// <summary>
        /// Fired when live syncing has stopped on a syncbox.
        /// </summary>
        /// <param name="e">Message parameters</param>
        void SyncboxDidStopLiveSyncChanged(ISyncboxDidStopLiveSyncMessage e);
        /// <summary>
        /// Fired when live syncing has stopped due to an error.
        /// </summary>
        /// <param name="e">Message parameters</param>
        void SyncboxLiveSyncFailedWithErrorChanged(ISyncboxLiveSyncFailedWithErrorMessage e);
    }
    public interface IMinimalMessage : IHandleableArgs
    {
        string Message { get; }
        Nullable<long> SyncboxId { get; }
        string DeviceId { get; }
        BaseMessage BaseMessage { get; }
    }
    public interface IBasicMessage : IMinimalMessage
    {
        bool IsError { get; }
        EventMessageLevel Level { get; }
    }
    public interface ISetCountMessage : IMinimalMessage
    {
        uint NewCount { get; }
    }
    public interface IIncrementCountMessage : IMinimalMessage
    {
        uint IncrementAmount { get; }
    }
    public interface ITransferUpdateMessage : IMinimalMessage
    {
        long EventId { get; }
        CLStatusFileTransferUpdateParameters Parameters { get; }
    }
    public interface IInternetConnectivityMessage : IMinimalMessage
    {
        bool InternetConnected { get; }
    }
    public interface IDownloadCompleteMessage : IMinimalMessage
    {
        long EventId { get; }
        CLFileItem FileItem { get; }
    }
    public interface IUploadCompleteMessage : IMinimalMessage
    {
        long EventId { get; }
        CLFileItem FileItem { get; }
    }
    public interface IStorageQuotaExceededMessage : IMinimalMessage
    {
    }
    public interface ISyncboxDidStartLiveSyncMessage : IMinimalMessage
    {
        CLSyncbox Syncbox { get; }
    }
    public interface ISyncboxDidStopLiveSyncMessage : IMinimalMessage
    {
        CLSyncbox Syncbox { get; }
    }
    public interface ISyncboxLiveSyncFailedWithErrorMessage : IMinimalMessage
    {
        CLSyncbox Syncbox { get; }
        CLError Error { get; }
    }
}