using RateBar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace CloudSdkSyncSample.EventMessageReceiver
{
    /// <summary>
    /// Interface for exposed properties on a file transfer status object
    /// </summary>
    public interface ICLStatusFileTransfer
    {
        /// <summary>
        /// Relative path to the transferring file from the sync root folder
        /// </summary>
        string SyncRelativePath { get; }
        /// <summary>
        /// Whether this file transfer status should be visible (visible for actual transfers and not visible for blank placeholders)
        /// </summary>
        Visibility Visibility { get; }
        /// <summary>
        /// Number from 0 to 1 for the current transfer rate out of historical maximum or twice the starting rate (whichever is greater)
        /// </summary>
        Double DisplayRateAtCurrentSample { get; }
        /// <summary>
        /// A WPF Control for display of the rate history for the current transfer
        /// </summary>
        RateGraph StatusGraph { get; }
        /// <summary>
        /// The percentage completeness of transfer
        /// </summary>
        Double PercentComplete { get; }
        /// <summary>
        /// String for display of total transfer size in appropriate scale representation of bytes (i.e. "X bytes" or "X.Y KB" or "X.Y MB" or "X.Y GB"...)
        /// </summary>
        string DisplayFileSize { get; }
        /// <summary>
        /// Estimated time remaining for transfer completion
        /// </summary>
        string DisplayTimeLeft { get; }
        /// <summary>
        /// Total time already elapsed since the transfer start
        /// </summary>
        string DisplayElapsedTime { get; }
    }
}