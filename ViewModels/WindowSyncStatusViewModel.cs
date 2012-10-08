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
using win_client.Model;
using System.Collections.Generic;
using System.Windows.Threading;
using System;

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

        public WindowSyncStatusViewModel()
        {
            // Start the Object Creation timer
            _timerDebugObjectCreation = new DispatcherTimer();
            _timerDebugObjectCreation.Interval = TimeSpan.FromMilliseconds(100);
            _timerDebugObjectCreation.Tick += OnTimerDebugObjectCreation_Tick;

            // Start the Bandwidth Setting timer
            _timerDebugBandwidthSetting = new DispatcherTimer();
            _timerDebugBandwidthSetting.Interval = TimeSpan.FromMilliseconds(100);
            _timerDebugBandwidthSetting.Tick += OnTimerDebugBandwidthSetting_Tick;

            // Start the Object Creation timer
            _timerDebugProcessLists = new DispatcherTimer();
            _timerDebugProcessLists.Interval = TimeSpan.FromMilliseconds(200);
            _timerDebugProcessLists.Tick += OnTimerDebugProcessLists_Tick;

        }

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
            List<CLStatusFileTransfer> targetList = isUpload ? ListFilesUploading : ListFilesDownloading;
            lock (targetList)
            {
                if (targetList.Count <= 5)
                {
                    // Choose a random file size between zero and 2 GB
                    long fileSize = (long)(_random.NextDouble() * 2048000000.0);

                    // Build a random path and fileNameExt.
                    int pathIndex = _random.Next(_pathList.Count);
                    string path = _pathList[pathIndex];
                    int fileNameExtIndex = _random.Next(_fileNameList.Count);
                    string fileNameExt = _fileNameList[fileNameExtIndex];
                    string fullPath = path + fileNameExt;

                    // Determine the bandwidth allocation for this object at this moment.
                    Double dblCurrentBandwidthBitsPerSecond = isUpload ? _dblCurrentBandwidthBitsPerSecondUpload : _dblCurrentBandwidthBitsPerSecondDownload;
                    Double dblHistoricBandwidthBitsPerSecond = isUpload ? _historicAverageBandwidthBitsPerSecondUpload : _historicAverageBandwidthBitsPerSecondDownload;

                    // Determine the object's beginning rate.  This is 0.0 to 1.0.  _kPercentOfDisplayHeightRepresentingMaxBandwidth represents
                    // the vertical display height (the rate) calculated as 1/6 of the historical aggregate bandwidth.  Therefore, 1.0
                    // represents _historicAverageBandwidthBitsPerSecondUpload / _kPercentOfDisplayHeightRepresentingMaxBandwidth.
                    // Or: rate = Max(1.0, dblCurrentBandwidthMegabitsPerSecond / (dblHistoricBandwidthMegabitsPerSecond / _kPercentOfDisplayHeightRepresentingMaxBandwidth)
                    Double dblCurrentRate = Math.Max(1.0, dblCurrentBandwidthBitsPerSecond / (dblHistoricBandwidthBitsPerSecond / _kPercentOfDisplayHeightRepresentingMaxBandwidth));

                    // Allocate a new object, set it up and add it to the list.
                    CLStatusFileTransfer objXfer = new CLStatusFileTransfer();
                    objXfer.BytesTransferedAtCurrentSample = 0;
                    objXfer.CloudRelativePath = fullPath;
                    objXfer.CurrentSampleTime = DateTime.MinValue;
                    objXfer.FileSizeBytes = fileSize;
                    objXfer.IsComplete = false;
                    objXfer.IsDirectionUpload = isUpload;
                    objXfer.PercentComplete = 0.0;
                    objXfer.RateAtCurrentSample = dblCurrentRate;
                    objXfer.SamplesTaken = 0;
                    objXfer.StartTime = DateTime.Now;
                    targetList.Add(objXfer);
                }
            }

            // Choose the time of the next creation tick
            Double dblRandomCreationTimerInterval = _random.NextDouble() * 1000.0;      // random number between 0 and 1 seconds.
            _timerDebugObjectCreation.Interval = TimeSpan.FromMilliseconds(dblRandomCreationTimerInterval) ;
            _timerDebugObjectCreation.Start();

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
            // Process the upload list.  First remove any completed uploads.
            lock (ListFilesUploading)
            {
                for (int i = ListFilesUploading.Count - 1; i > -1; i--)
                {
                    if (ListFilesUploading[i].IsComplete)
                    {
                        ListFilesUploading.RemoveAt(i);
                    }
                }

                // Adjust the remaining active objects
                foreach (CLStatusFileTransfer objXfer in ListFilesUploading)
                {
                    objXfer.SamplesTaken++;
                    //&&&&&&&&&&&&&&&&&&&&&&&&&objXfer.
                    // 
                    //o Any model data required will be stored in the CLStatusFileTransfer object and marked debug.
                    //o From the previous bandwidth allocated to this object, determine the bytes transferred in the last time slot.
                    //o Adjust the object's properties by the amount transferred and the previous rate..
                    //o Determine the rate from the current bandwidth at this moment and save it for next time.
                }
            }

            // Process the download list
            foreach (CLStatusFileTransfer objXfer in ListFilesDownloading)
            {
            }

        }

        #region Bindable Properties

        /// <summary>
        /// Sets and gets the ListFilesDownloading property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public const string ListFilesDownloadingPropertyName = "ListFilesDownloading";
        private List<CLStatusFileTransfer> _listFilesDownloadingPropertyName = new List<CLStatusFileTransfer>();
        public List<CLStatusFileTransfer> ListFilesDownloading
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
        private List<CLStatusFileTransfer> _listFilesUploadingPropertyName = new List<CLStatusFileTransfer>();
        public List<CLStatusFileTransfer> ListFilesUploading
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

        #region Support Functions

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