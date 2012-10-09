//
//  WindowSyncStatusViewModel.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Dialog.Implementors.Wpf.MVVM;
using win_client.ViewModels;
using win_client.Model;
using System.Windows;
using CloudApiPublic.Support;
using System.Resources;
using win_client.AppDelegate;
using System.ComponentModel;
using win_client.Common;
using System.Collections.Generic;
using System.Windows.Threading;
using System;
using System.Collections.ObjectModel;

namespace win_client.ViewModels
{
    public class WindowSyncStatusViewModel : ValidatingViewModelBase
    {
        #region Private Instance Variables

        private CLTrace _trace = CLTrace.Instance;
        private DispatcherTimer _timerDebugObjectCreation = null;
        private DispatcherTimer _timerDebugBandwidthSetting = null;
        private DispatcherTimer _timerDebugProcessLists = null;
        private Random _random = new Random();
        private Double _dblCurrentBandwidthBitsPerSecondUpload = 0.0;
        private Double _dblCurrentBandwidthBitsPerSecondDownload = 0.0;
        private const Double _historicAverageBandwidthBitsPerSecondUpload = 1024000.0;
        private const Double _historicAverageBandwidthBitsPerSecondDownload = 2048000.0;

        private const Double _kPercentOfDisplayHeightRepresentingMaxBandwidth = 0.8;
        private const int _kBitsPerByteFudgeFactor = 10;
        private const int _kTimerDebugObjectCreationBasePeriodMs = 1000;
        private const int _kTimerDebugBandwidthSettingBasePeriodMs = 100;
        private const int _kTimerDebugProcessListsBasePeriodMs = 200;

        private List<string> _pathList = new List<string>()
            {
                "\\Pictures\\MyPictures",
                "\\Pictures\\MyPictures\\Party",
                "\\Documents\\Words",
                "\\Documents\\Words",
                "\\Videos\\Weddings\\Julia",
                "\\Videos\\Weddings\\Tom",
            };
        private List<string> _fileNameList = new List<string>()
            {
                "\\CloudDesign20121008.doc",
                "\\Betty.png",
                "\\Taxes2012.xlsx",
                "\\Movie.avi",
                "\\CloudImageList.xml",
                "\\CloudCode.cs",
            };

        #endregion

        #region Events from View

        public void OnViewLoaded()
        {
            // Prime the pump with the current transfer rate.
            OnTimerDebugBandwidthSetting_Tick(null, null);

            // Start the Object Creation timer
            _timerDebugObjectCreation = new DispatcherTimer();
            _timerDebugObjectCreation.Interval = TimeSpan.FromMilliseconds(100);
            _timerDebugObjectCreation.Tick += OnTimerDebugObjectCreation_Tick;
            _timerDebugObjectCreation.Start();

            // Start the Bandwidth Setting timer
            _timerDebugBandwidthSetting = new DispatcherTimer();
            _timerDebugBandwidthSetting.Interval = TimeSpan.FromMilliseconds(_kTimerDebugBandwidthSettingBasePeriodMs);
            _timerDebugBandwidthSetting.Tick += OnTimerDebugBandwidthSetting_Tick;
            _timerDebugBandwidthSetting.Start();

            // Start the list processing timer
            _timerDebugProcessLists = new DispatcherTimer();
            _timerDebugProcessLists.Interval = TimeSpan.FromMilliseconds(_kTimerDebugProcessListsBasePeriodMs);
            _timerDebugProcessLists.Tick += OnTimerDebugProcessLists_Tick;
            _timerDebugProcessLists.Start();
        }

        #endregion

        #region Bindable Properties

        /// <summary>
        /// Sets and gets the ListFilesDownloading property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public const string ListFilesDownloadingPropertyName = "ListFilesDownloading";
        private ObservableCollection<CLStatusFileTransfer> _listFilesDownloadingPropertyName = new ObservableCollection<CLStatusFileTransfer>();
        public ObservableCollection<CLStatusFileTransfer> ListFilesDownloading
        {
            get
            {
                return _listFilesDownloadingPropertyName;
            }

            set
            {
                if (_listFilesDownloadingPropertyName == value)
                {
                    return;
                }

                _listFilesDownloadingPropertyName = value;
                RaisePropertyChanged(ListFilesDownloadingPropertyName);
            }
        }

        /// <summary>
        /// Sets and gets the ListFilesUploading property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public const string ListFilesUploadingPropertyName = "ListFilesUploading";
        private ObservableCollection<CLStatusFileTransfer> _listFilesUploadingPropertyName = new ObservableCollection<CLStatusFileTransfer>();
        public ObservableCollection<CLStatusFileTransfer> ListFilesUploading
        {
            get
            {
                return _listFilesUploadingPropertyName;
            }

            set
            {
                if (_listFilesUploadingPropertyName == value)
                {
                    return;
                }

                _listFilesUploadingPropertyName = value;
                RaisePropertyChanged(ListFilesUploadingPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="WindowSyncStatus_Title" /> property's name.
        /// </summary>
        public const string WindowSyncStatus_TitlePropertyName = "WindowSyncStatus_Title";
        private string _windowSyncStatus_Title = "Cloud Sync Status";
        public string WindowSyncStatus_Title
        {
            get
            {
                return _windowSyncStatus_Title;
            }

            set
            {
                if (_windowSyncStatus_Title == value)
                {
                    return;
                }

                _windowSyncStatus_Title = value;
                RaisePropertyChanged(WindowSyncStatus_TitlePropertyName);
            }
        }

        #endregion

        #region Relay Commands

        #endregion

        #region Methods

        /// <summary>
        /// Debug: Timer tick.  Handle the random upload/download object creation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerDebugObjectCreation_Tick(object sender, EventArgs e)
        {

            // Stop the timer while we process.
            _timerDebugObjectCreation.Stop();

            // Choose the direction (upload or download)
            bool isUpload = _random.NextDouble() > 0.5 ? true : false;

            // Generate a new upload or download only if the target list is not full.
            ObservableCollection<CLStatusFileTransfer> targetList = isUpload ? ListFilesUploading : ListFilesDownloading;
            lock (targetList)
            {
                if (targetList.Count <= 5)
                {
                    // Choose a random file size between zero and 204800.
                    long fileSize = (long)(_random.NextDouble() * 204800.0);

                    // Build a random path and fileNameExt.
                    int pathIndex = _random.Next(_pathList.Count);
                    string path = _pathList[pathIndex];
                    int fileNameExtIndex = _random.Next(_fileNameList.Count);
                    string fileNameExt = _fileNameList[fileNameExtIndex];
                    string fullPath = path + fileNameExt;

                    // Get the current rates for this object
                    Double dblCurrentDisplayRate;
                    Double dblXferRateBytesPerSecond;
                    GetCurrentTransferRates(isUpload, targetList, out dblCurrentDisplayRate, out dblXferRateBytesPerSecond);

                    // Allocate a new object, set it up and add it to the list.
                    CLStatusFileTransfer objXfer = new CLStatusFileTransfer();
                    objXfer.IsDirectionUpload = isUpload;
                    objXfer.CloudRelativePath = fullPath;
                    objXfer.FileSizeBytes = fileSize;
                    objXfer.SamplesTaken = 0;
                    objXfer.CumulativeBytesTransfered = 0;
                    objXfer.StartTime = DateTime.Now;
                    objXfer.CurrentSampleTime = objXfer.StartTime;
                    objXfer.TransferRateBytesPerSecondAtCurrentSample = dblXferRateBytesPerSecond;
                    objXfer.DisplayRateAtCurrentSample = dblCurrentDisplayRate * 100;
                    objXfer.PercentComplete = 0.0;
                    objXfer.IsComplete = false;
                    objXfer.DisplayElapsedTime = String.Empty;
                    objXfer.DisplayTimeLeft = String.Empty;
                    objXfer.DisplayFileSize = String.Empty;
                    targetList.Add(objXfer);
                }
            }

            // Choose the time of the next creation tick
            Double dblRandomCreationTimerInterval = _random.NextDouble() * _kTimerDebugObjectCreationBasePeriodMs;      // random number between 0 and base-period seconds.
            _timerDebugObjectCreation.Interval = TimeSpan.FromMilliseconds(dblRandomCreationTimerInterval) ;
            _timerDebugObjectCreation.Start();

        }

        /// <summary>
        /// Determine the current transfer rates for a transfer object
        /// </summary>
        /// <param name="isUpload">true: upload object</param>
        /// <param name="targetList">Upload or download list, depending on isUpload.  Assumed to be locked.</param>
        /// <param name="dblCurrentDisplayRate">Output object display rate (0.0 - 1.0)</param>
        /// <param name="dblXferRateBytesPerSecond">Output object transfer rate in bytes per second</param>
        private void GetCurrentTransferRates(bool isUpload, ObservableCollection<CLStatusFileTransfer> targetList, out Double dblCurrentDisplayRate, out Double dblXferRateBytesPerSecond)
        {
            // Determine the bandwidth allocation for this object at this moment.
            Double dblAggregateCurrentBandwidthBitsPerSecond = isUpload ? _dblCurrentBandwidthBitsPerSecondUpload : _dblCurrentBandwidthBitsPerSecondDownload;
            Double dblAggregateHistoricBandwidthBitsPerSecond = isUpload ? _historicAverageBandwidthBitsPerSecondUpload : _historicAverageBandwidthBitsPerSecondDownload;
            Double dblThisObjectCurrentBandwidthBitsPerSecond = dblAggregateCurrentBandwidthBitsPerSecond / (targetList.Count + 1);    // we haven't added this one to the count yet
            Double dblThisObjectHistoricBandwidthBitsPerSecond = dblAggregateHistoricBandwidthBitsPerSecond / (targetList.Count + 1);    // we haven't added this one to the count yet

            // Determine the object's beginning rate to be displayed.  This is a number from 0.0 to 1.0.
            // This object will get a portion of the total bandwidth depending on how many transfers are currently in progress.
            // If this is the only active transfer object, it will get the total bandwidth.
            // So, thisObjectBandwidthBitsPerSecond = dblAggregateCurrentBandwidthBitsPerSecond / (targetList.Count + 1).  // we haven't added this one yet
            // Now _kPercentOfDisplayHeightRepresentingMaxBandwidth represents the percent up the vertical display height (the display rate) that will
            // be the historical bandwidth in bits peer second for this object.  e.g., 80% or 0.8.  
            // So, if the current rate is equal to this object's historic rate allocation, the display rate would be _kPercentOfDisplayHeightRepresentingMaxBandwidth.
            dblCurrentDisplayRate = Math.Min(1.0, (dblThisObjectCurrentBandwidthBitsPerSecond / dblThisObjectHistoricBandwidthBitsPerSecond) * _kPercentOfDisplayHeightRepresentingMaxBandwidth);

            // Determine the object's actual transfer rate in bytes per second.
            dblXferRateBytesPerSecond = dblThisObjectCurrentBandwidthBitsPerSecond / _kBitsPerByteFudgeFactor;
        }

        /// <summary>
        /// Debug: Timer tick.  Set a random bandwidth in bits per second.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerDebugBandwidthSetting_Tick(object sender, EventArgs e)
        {
            // Set the current bandwith to a random number between 50% and 100% of the average historical bandwith.
            _dblCurrentBandwidthBitsPerSecondUpload = (_random.NextDouble() * (_historicAverageBandwidthBitsPerSecondUpload / 2)) + (_historicAverageBandwidthBitsPerSecondUpload / 2);
            _dblCurrentBandwidthBitsPerSecondDownload = (_random.NextDouble() * (_historicAverageBandwidthBitsPerSecondDownload / 2)) + (_historicAverageBandwidthBitsPerSecondDownload / 2);
        }

        /// <summary>
        /// Debug: Timer tick.  Process the upload/download lists.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerDebugProcessLists_Tick(object sender, EventArgs e)
        {
            // Process the lists
            for (int listIndex = 0; listIndex < 2; listIndex++)
            {
                // Choose the list.
                ObservableCollection<CLStatusFileTransfer> targetList = listIndex == 0 ? ListFilesUploading : ListFilesDownloading;

                // Process the list
                lock (targetList)
                {
                    // First remove any completed uploads.
                    for (int i = targetList.Count - 1; i > -1; i--)
                    {
                        if (targetList[i].IsComplete)
                        {
                            targetList.RemoveAt(i);
                        }
                    }

                    // Adjust the remaining active objects
                    foreach (CLStatusFileTransfer objXfer in targetList)
                    {
                        // Get the current rates for this object
                        Double dblCurrentDisplayRate;
                        Double dblXferRateBytesPerSecond;
                        GetCurrentTransferRates(isUpload: true, targetList: targetList, dblCurrentDisplayRate: out dblCurrentDisplayRate, dblXferRateBytesPerSecond: out dblXferRateBytesPerSecond);

                        objXfer.SamplesTaken++;

                        DateTime currentTime = DateTime.Now;
                        TimeSpan samplePeriodTimeSpan = currentTime - objXfer.CurrentSampleTime;    // time span of this sample period
                        objXfer.CurrentSampleTime = currentTime;

                        // Calculate the bytes transferred in the sample period just past.
                        long bytesPossibleToTransferInPeriod = (long)(dblXferRateBytesPerSecond * samplePeriodTimeSpan.TotalSeconds);
                        long bytesTransferedInPeriod = Math.Min(objXfer.FileSizeBytes - objXfer.CumulativeBytesTransfered, bytesPossibleToTransferInPeriod);
                        objXfer.CumulativeBytesTransfered += bytesTransferedInPeriod;

                        objXfer.TransferRateBytesPerSecondAtCurrentSample = dblXferRateBytesPerSecond;
                        objXfer.DisplayRateAtCurrentSample = dblCurrentDisplayRate * 100;

                        objXfer.PercentComplete = (objXfer.CumulativeBytesTransfered / objXfer.FileSizeBytes) * 100;
                        objXfer.IsComplete = (objXfer.CumulativeBytesTransfered >= objXfer.FileSizeBytes);

                        // Fill in the display strings.
                        TimeSpan elapsedTime = objXfer.CurrentSampleTime - objXfer.StartTime;
                        objXfer.DisplayElapsedTime = String.Format("Elapsed time: {0}:{1}", Math.Floor(elapsedTime.TotalMinutes), elapsedTime.Seconds);

                        int secondsLeft = (int)((objXfer.FileSizeBytes - objXfer.CumulativeBytesTransfered) / objXfer.TransferRateBytesPerSecondAtCurrentSample);
                        TimeSpan timeLeft = new TimeSpan(0, 0, secondsLeft);
                        objXfer.DisplayTimeLeft = String.Format("Time left: {0}:{1}", Math.Floor(elapsedTime.TotalMinutes), elapsedTime.Seconds);

                        objXfer.DisplayFileSize = String.Format("File size: {0:#,0}", objXfer.FileSizeBytes);
                    }
                }
            }
        }

        /// <summary>
        /// Implement window closing logic.
        /// <remarks>Note: This function will be called twice when the user clicks the Cancel button, and only once when the user
        /// clicks the 'X'.  Be careful to check for the "already cleaned up" case.</remarks>
        /// <<returns>true to cancel the cancel.</returns>
        /// </summary>
        private bool OnClosing()
        {
            // Clean-up logic here.
            return false;                   // don't cancel the user's request to cancel
        }

        #endregion
    }
}