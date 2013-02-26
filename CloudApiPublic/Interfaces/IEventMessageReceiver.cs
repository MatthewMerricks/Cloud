//
// MessageEvents.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using CloudApiPublic.Model.EventMessages;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Interfaces
{
    /// <summary>
    /// Interface for implementing a view model for sync status, see EventMessageReceiver in CloudSdkSyncSample code for sample implementation
    /// </summary>
    public interface IEventMessageReceiver
    {
        /// <summary>
        /// Fired when NewEventMessage event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void MessageEvents_NewEventMessage(IBasicMessage e);
        /// <summary>
        /// Fired when NewEventMessage event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void AddStatusMessage(IBasicMessage e);
        /// <summary>
        /// Fired when DownloadingCountSet event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void SetDownloadingCount(ISetCountMessage e);
        /// <summary>
        /// Fired when DownloadingCountSet event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void SetUploadingCount(ISetCountMessage e);
        /// <summary>
        /// Fired when DownloadedCountIncremented event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void IncrementDownloadedCount(IIncrementCountMessage e);
        /// <summary>
        /// Fired when UploadedCountIncremented event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void IncrementUploadedCount(IIncrementCountMessage e);
        /// <summary>
        /// Fired when FileUploadUpdated event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void UpdateFileUpload(ITransferUpdateMessage e);
        /// <summary>
        /// Fired when FileDownloadUpdated event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void UpdateFileDownload(ITransferUpdateMessage e);
    }
    public interface IMinimalMessage : IHandleableArgs
    {
        string Message { get; }
        Nullable<long> SyncBoxId { get; }
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
}