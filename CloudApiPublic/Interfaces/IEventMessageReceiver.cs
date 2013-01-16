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
        /// <param name="sender">Do not rely on sender being a specific type of object, subject to change without notice</param>
        /// <param name="e">Message parameters</param>
        void MessageEvents_NewEventMessage(object sender, EventMessageArgs e);
        /// <summary>
        /// Fired when NewEventMessage event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="sender">Do not rely on sender being a specific type of object, subject to change without notice</param>
        /// <param name="e">Message parameters</param>
        void AddStatusMessage(object sender, EventMessageArgs e);
        /// <summary>
        /// Fired when DownloadingCountSet event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="sender">Do not rely on sender being a specific type of object, subject to change without notice</param>
        /// <param name="e">Message parameters</param>
        void SetDownloadingCount(object sender, SetCountArgs e);
        /// <summary>
        /// Fired when DownloadingCountSet event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="sender">Do not rely on sender being a specific type of object, subject to change without notice</param>
        /// <param name="e">Message parameters</param>
        void SetUploadingCount(object sender, SetCountArgs e);
        /// <summary>
        /// Fired when DownloadedCountIncremented event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="sender">Do not rely on sender being a specific type of object, subject to change without notice</param>
        /// <param name="e">Message parameters</param>
        void IncrementDownloadedCount(object sender, IncrementCountArgs e);
        /// <summary>
        /// Fired when UploadedCountIncremented event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="e">Message parameters</param>
        void IncrementUploadedCount(object sender, IncrementCountArgs e);
        /// <summary>
        /// Fired when FileUploadUpdated event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="sender">Do not rely on sender being a specific type of object, subject to change without notice</param>
        /// <param name="e">Message parameters</param>
        void UpdateFileUpload(object sender, TransferUpdateArgs e);
        /// <summary>
        /// Fired when FileDownloadUpdated event fires in CloudApiPublic.Static.MessageEvents, filtered down to a specific SyncBox/Device combination
        /// </summary>
        /// <param name="sender">Do not rely on sender being a specific type of object, subject to change without notice</param>
        /// <param name="e">Message parameters</param>
        void UpdateFileDownload(object sender, TransferUpdateArgs e);
    }
}