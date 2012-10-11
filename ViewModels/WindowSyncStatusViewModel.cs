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
using System.Linq;
using CloudApiPrivate.Model;

namespace win_client.ViewModels
{
    public class WindowSyncStatusViewModel : ValidatingViewModelBase
    {
        #region Private Instance Variables

        private CLTrace _trace = CLTrace.Instance;
        private DispatcherTimer _timerDebugObjectCreation = null;
        private DispatcherTimer _timerDebugBandwidthSetting = null;
        private DispatcherTimer _timerDebugProcessLists = null;
        private DispatcherTimer _timerDebugCreateMessages = null;
        private Random _random = new Random();
        private Double _dblCurrentBandwidthBitsPerSecondUpload = 0.0;
        private Double _dblCurrentBandwidthBitsPerSecondDownload = 0.0;
        private const Double _historicAverageBandwidthBitsPerSecondUpload = 1024000.0;
        private const Double _historicAverageBandwidthBitsPerSecondDownload = 2048000.0;

        private const Double _kPercentOfDisplayHeightRepresentingMaxBandwidth = 0.8;
        private const int _kBitsPerByteFudgeFactor = 10;
        private const int _kTimerDebugObjectCreationBasePeriodMs = 500;
        private const int _kTimerDebugBandwidthSettingBasePeriodMs = 100;
        private const int _kTimerDebugProcessListsBasePeriodMs = 200;
        private const int _kTimerDebugCreateMessagesBasePeriodMs = 5000;

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

        private List<string> _messageList = new List<string>()
            {
                "File \\Pictures\\abc.png downloaded.",
                "File \\Documents\\MyStory.doc uploaded.",
                "ERROR: Connection lost.",
                "Connection restored.",
                "Very long message. Very long message. Very long message. Very long message. Very long message. Very long message. Very long message. Very long message. Very long message."
            };

        #endregion

        #region Constructor

        public WindowSyncStatusViewModel()
        {
            //TODO: Initialize the historic bandwidth

            // Prime the pump with the current transfer rate.
            OnTimerDebugBandwidthSetting_Tick(null, null);

            // Start the Object Creation timer
            _timerDebugObjectCreation = new DispatcherTimer(DispatcherPriority.Input,
                Application.Current.Dispatcher);
            _timerDebugObjectCreation.Interval = TimeSpan.FromMilliseconds(100);
            _timerDebugObjectCreation.Tick += OnTimerDebugObjectCreation_Tick;
            _timerDebugObjectCreation.Start();

            // Start the Bandwidth Setting timer
            _timerDebugBandwidthSetting = new DispatcherTimer(DispatcherPriority.Input,
                Application.Current.Dispatcher);
            _timerDebugBandwidthSetting.Interval = TimeSpan.FromMilliseconds(_kTimerDebugBandwidthSettingBasePeriodMs);
            _timerDebugBandwidthSetting.Tick += OnTimerDebugBandwidthSetting_Tick;
            _timerDebugBandwidthSetting.Start();

            // Start the list processing timer
            _timerDebugProcessLists = new DispatcherTimer(DispatcherPriority.Input,
                Application.Current.Dispatcher);
            _timerDebugProcessLists.Interval = TimeSpan.FromMilliseconds(_kTimerDebugProcessListsBasePeriodMs);
            _timerDebugProcessLists.Tick += OnTimerDebugProcessLists_Tick;
            _timerDebugProcessLists.Start();

            // Start the message creation timer
            _timerDebugCreateMessages = new DispatcherTimer(DispatcherPriority.Input, Application.Current.Dispatcher);
            _timerDebugCreateMessages.Interval = TimeSpan.FromMilliseconds(100);
            _timerDebugCreateMessages.Tick += OnTimerDebugCreateMessages_Tick;
            _timerDebugCreateMessages.Start();

            // Allocate the fixed collections of upload and download objects (6 each)
            ListFilesUploading = new ObservableCollection<object>();
            ListFilesDownloading = new ObservableCollection<object>();
            for (int i = 0; i < 6; i++)
            {
                _trace.writeToLog(9, "WindowSyncStatusViewModel: OnViewLoaded: Add upload list item: {0}.", i);
                CLStatusFileTransferBlank objXfer = new CLStatusFileTransferBlank();
                ListFilesUploading.Add(objXfer);

                _trace.writeToLog(9, "WindowSyncStatusViewModel: OnViewLoaded: Add download list item: {0}.", i);
                objXfer = new CLStatusFileTransferBlank();
                ListFilesDownloading.Add(objXfer);
            }

            // Allocate the message list
            ListMessages = new ObservableCollection<CLStatusMessage>();
        }

        #endregion

        #region Bindable Properties

        /// <summary>
        /// Sets and gets the ListFilesDownloading property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public const string ListFilesDownloadingPropertyName = "ListFilesDownloading";
        private ObservableCollection<object> _listFilesDownloading = null;
        public ObservableCollection<object> ListFilesDownloading
        {
            get
            {
                return _listFilesDownloading;
            }

            set
            {
                if (_listFilesDownloading == value)
                {
                    return;
                }

                _listFilesDownloading = value;
                RaisePropertyChanged(ListFilesDownloadingPropertyName);
            }
        }

        /// <summary>
        /// Sets and gets the ListFilesUploading property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public const string ListFilesUploadingPropertyName = "ListFilesUploading";
        private ObservableCollection<object> _listFilesUploading = null;
        public ObservableCollection<object> ListFilesUploading
        {
            get
            {
                return _listFilesUploading;
            }

            set
            {
                if (_listFilesUploading == value)
                {
                    return;
                }

                _listFilesUploading = value;
                RaisePropertyChanged(ListFilesUploadingPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ListMessages" /> property's name.
        /// </summary>
        public ObservableCollection<CLStatusMessage> ListMessages
        {
            get
            {
                return _listMessages;
            }

            set
            {
                if (_listMessages == value)
                {
                    return;
                }

                _listMessages = value;
                RaisePropertyChanged(ListMessagesPropertyName);
            }
        }
        public const string ListMessagesPropertyName = "ListMessages";
        private ObservableCollection<CLStatusMessage> _listMessages = null;

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

        /// <summary>
        /// Gets the WindowSyncStatus_DoneCommand.
        /// </summary>
        public ICommand WindowSyncStatus_DoneCommand
        {
            get
            {
                return _windowSyncStatus_DoneCommand
                    ?? (_windowSyncStatus_DoneCommand = new RelayCommand(
                                          () =>
                                          {
                                              CLAppMessages.Message_WindowSyncStatus_ShouldClose.Send(String.Empty);
                                          }));
            }
        }
        private ICommand _windowSyncStatus_DoneCommand;

        /// <summary>
        /// Gets the WindowSyncStatus_ShowLogCommand.
        /// </summary>
        public ICommand WindowSyncStatus_ShowLogCommand
        {
            get
            {
                return _windowSyncStatus_ShowLogCommand
                    ?? (_windowSyncStatus_ShowLogCommand = new RelayCommand(
                                          () =>
                                          {
                                              //TODO: Implement.
                                              MessageBox.Show("Not implemented.", "Not Implemented!", MessageBoxButton.OK);
                                          }));
            }
        }
        private ICommand _windowSyncStatus_ShowLogCommand;

        /// <summary>
        /// Gets the WindowSyncStatus_SaveLogCommand.
        /// </summary>
        public ICommand WindowSyncStatus_SaveLogCommand
        {
            get
            {
                return _windowSyncStatus_SaveLogCommand
                    ?? (_windowSyncStatus_SaveLogCommand = new RelayCommand(
                                          () =>
                                          {
                                              //TODO: Implement.
                                              MessageBox.Show("Not implemented.", "Not Implemented!", MessageBoxButton.OK);
                                          }));
            }
        }
        private ICommand _windowSyncStatus_SaveLogCommand;

        /// <summary>
        /// Gets the WindowSyncStatus_ShowErrorLogCommand.
        /// </summary>
        public ICommand WindowSyncStatus_ShowErrorLogCommand
        {
            get
            {
                return _windowSyncStatus_ShowErrorLogCommand
                    ?? (_windowSyncStatus_ShowErrorLogCommand = new RelayCommand(
                                          () =>
                                          {
                                              //TODO: Implement.
                                              MessageBox.Show("Not implemented.", "Not Implemented!", MessageBoxButton.OK);
                                          }));
            }
        }
        private ICommand _windowSyncStatus_ShowErrorLogCommand;

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
            _trace.writeToLog(9, "WindowSyncStatusViewModel: OnTimerDebugObjectCreation_Tick: Entry.");
            _timerDebugObjectCreation.Stop();

            // Choose the direction (upload or download)
            bool isUpload = _random.NextDouble() > 0.5 ? true : false;

            // Generate a new upload or download only if the target list is not full.
            ObservableCollection<object> targetList = isUpload ? ListFilesUploading : ListFilesDownloading;
            lock (targetList)
            {
                _trace.writeToLog(9, "WindowSyncStatusViewModel: OnTimerDebugObjectCreation_Tick: Work on upload list: {0}. Currently active items: {1}.  Count: {2}.", isUpload, GetCurrentActiveCountInList(targetList), targetList.Count);
                // Loop through the target list looking for an available spot.
                for (int i = 0; i < targetList.Count; i++)
                {
                    _trace.writeToLog(9, "WindowSyncStatusViewModel: OnTimerDebugObjectCreation_Tick: Top of loop.  IsUpload: {0}. Index: {1}.", isUpload, i);
                    if (targetList[i] is CLStatusFileTransferBlank)
                    {
                        // Make a new display object
                        _trace.writeToLog(9, "WindowSyncStatusViewModel: OnTimerDebugObjectCreation_Tick: Create an item.  IsUpload: {0}. item: {1}.", isUpload, i);
                        CLStatusFileTransfer objXfer = new CLStatusFileTransfer();

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
                        GetCurrentTransferRates(isUpload, targetList, GetCurrentActiveCountInList(targetList), out dblCurrentDisplayRate, out dblXferRateBytesPerSecond);

                        // Allocate a new object, set it up and add it to the list.
                        objXfer.IsDirectionUpload = isUpload;
                        objXfer.CloudRelativePath = fullPath;
                        objXfer.FileSizeBytes = fileSize;
                        objXfer.SamplesTaken = 0;
                        objXfer.CumulativeBytesTransferred = 0;
                        objXfer.StartTime = DateTime.Now;
                        objXfer.CurrentSampleTime = objXfer.StartTime;
                        objXfer.TransferRateBytesPerSecondAtCurrentSample = dblXferRateBytesPerSecond;
                        objXfer.DisplayRateAtCurrentSample = dblCurrentDisplayRate;
                        objXfer.PercentComplete = 0.0;
                        objXfer.IsComplete = false;
                        objXfer.DisplayElapsedTime = String.Empty;
                        objXfer.DisplayTimeLeft = String.Empty;
                        objXfer.DisplayFileSize = String.Empty;

                        _trace.writeToLog(9, "WindowSyncStatusViewModel: OnTimerDebugObjectCreation_Tick: IsUpload: {0}. Count {1}.", isUpload, targetList.Count);
                        List<object> fileTransferBases = new List<object>((i == 0
                                ? Enumerable.Empty<object>()
                                //: targetList.Take(i - 1))
                                : targetList.Take(i))
                            .Concat(targetList.Skip(i + 1)));

                        targetList.Clear();
                        foreach (object toAdd in fileTransferBases.Take(i))
                        {
                            targetList.Add(toAdd);
                        }
                        targetList.Add(objXfer);
                        foreach (object toAdd in fileTransferBases.Skip(i))
                        {
                            targetList.Add(toAdd);
                        }
                        _trace.writeToLog(9, "WindowSyncStatusViewModel: OnTimerDebugObjectCreation_Tick: IsUpload: {0}. CountAfter {1}.", isUpload, targetList.Count);

                        // Break out.  Done now creating this transfer object.
                        break;
                    }
                    _trace.writeToLog(9, "WindowSyncStatusViewModel: OnTimerDebugObjectCreation_Tick: After if.");
                }
                _trace.writeToLog(9, "WindowSyncStatusViewModel: OnTimerDebugObjectCreation_Tick: After loop.");
            }

            // Choose the time of the next creation tick
            Double dblRandomCreationTimerInterval = _random.NextDouble() * _kTimerDebugObjectCreationBasePeriodMs;      // random number between 0 and base-period seconds.
            _timerDebugObjectCreation.Interval = TimeSpan.FromMilliseconds(dblRandomCreationTimerInterval) ;
            _timerDebugObjectCreation.Start();

        }

        /// <summary>
        /// Handle the creation of messages on a timer. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerDebugCreateMessages_Tick(object sender, EventArgs e)
        {
            // Stop the timer while we process.
            _trace.writeToLog(9, "WindowSyncStatusViewModel: OnTimerDebugCreateMessages_Tick: Entry.");
            _timerDebugCreateMessages.Stop();

            CLStatusMessage newMessage = new CLStatusMessage();
            int msgIndex = _random.Next(_messageList.Count);
            newMessage.MessageText = DateTime.Now.ToString("G") + ": " + _messageList[msgIndex];
            ListMessages.Add(newMessage);

            // Choose the time of the next creation tick
            Double dblRandomMessageCreationTimerInterval = _random.NextDouble() * _kTimerDebugCreateMessagesBasePeriodMs;      // random number between 0 and base-period seconds.
            _timerDebugCreateMessages.Interval = TimeSpan.FromMilliseconds(dblRandomMessageCreationTimerInterval);
            _timerDebugCreateMessages.Start();
        }

        /// <summary>
        /// Determine the count of the currently active transfer objects in a list.
        /// </summary>
        /// <param name="targetList">The target list.</param>
        /// <returns></returns>
        private int GetCurrentActiveCountInList(ObservableCollection<object> targetList)
        {
            int returnCount = 0;
            for (int i = 0; i < targetList.Count; i++)
            {
                if (targetList[i] is CLStatusFileTransfer)
                {
                    returnCount++;
                }
            }
            return returnCount;
        }

        /// <summary>
        /// Determine the current transfer rates for a transfer object
        /// </summary>
        /// <param name="isUpload">true: upload object</param>
        /// <param name="targetList">Upload or download list, depending on isUpload.  Assumed to be locked.</param>
        /// <param name="countInTargetList">The count in the target list to use to apportion the bandwidth.</param>
        /// <param name="dblCurrentDisplayRate">Output object display rate (0.0 - 1.0)</param>
        /// <param name="dblXferRateBytesPerSecond">Output object transfer rate in bytes per second</param>
        private void GetCurrentTransferRates(bool isUpload, ObservableCollection<object> targetList, int countInTargetList, out Double dblCurrentDisplayRate, out Double dblXferRateBytesPerSecond)
        {
            // Determine the bandwidth allocation for this object at this moment.
            Double dblAggregateCurrentBandwidthBitsPerSecond = isUpload ? _dblCurrentBandwidthBitsPerSecondUpload : _dblCurrentBandwidthBitsPerSecondDownload;
            Double dblAggregateHistoricBandwidthBitsPerSecond = isUpload ? _historicAverageBandwidthBitsPerSecondUpload : _historicAverageBandwidthBitsPerSecondDownload;
            Double dblThisObjectCurrentBandwidthBitsPerSecond = dblAggregateCurrentBandwidthBitsPerSecond / countInTargetList;    // we haven't added this one to the count yet
            Double dblThisObjectHistoricBandwidthBitsPerSecond = dblAggregateHistoricBandwidthBitsPerSecond / countInTargetList;    // we haven't added this one to the count yet

            // Determine the object's beginning rate to be displayed.  This is a number from 0.0 to 1.0.
            // This object will get a portion of the total bandwidth depending on how many transfers are currently in progress.
            // If this is the only active transfer object, it will get the total bandwidth.
            // So, thisObjectBandwidthBitsPerSecond = dblAggregateCurrentBandwidthBitsPerSecond / countInTargetList.
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
                ObservableCollection<object> targetList = listIndex == 0 ? ListFilesUploading : ListFilesDownloading;

                // Process the list
                lock (targetList)
                {
                    // First mark any completed uploads.
                    for (int i = 0; i < targetList.Count; i++)
                    {
                        if (targetList[i] is CLStatusFileTransfer
                            && ((CLStatusFileTransfer)targetList[i]).IsComplete)
                        {
                            _trace.writeToLog(9, "WindowSyncStatusViewModel: OnTimerDebugProcessLists_Tick: Delete item at listIndex: {0}. item: {1}.", listIndex, i);
                            targetList[i] = new CLStatusFileTransferBlank();
                        }
                    }

                    // Adjust the remaining active objects
                    for (int i = 0; i < targetList.Count; i++)
                    {
                        CLStatusFileTransfer objXfer = targetList[i] as CLStatusFileTransfer;
                        if (objXfer != null)
                        {
                            // Get the current rates for this object
                            _trace.writeToLog(9, "WindowSyncStatusViewModel: OnTimerDebugProcessLists_Tick: Adjust listIndex: {0}. item: {1}.", listIndex, i);
                            Double dblCurrentDisplayRate;
                            Double dblXferRateBytesPerSecond;
                            GetCurrentTransferRates(isUpload: true, targetList: targetList, countInTargetList: GetCurrentActiveCountInList(targetList), 
                                                    dblCurrentDisplayRate: out dblCurrentDisplayRate, dblXferRateBytesPerSecond: out dblXferRateBytesPerSecond);

                            objXfer.SamplesTaken++;

                            DateTime currentTime = DateTime.Now;
                            TimeSpan samplePeriodTimeSpan = currentTime - objXfer.CurrentSampleTime;    // time span of this sample period
                            objXfer.CurrentSampleTime = currentTime;

                            // Calculate the bytes transferred in the sample period just past.
                            long bytesPossibleToTransferInPeriod = (long)(dblXferRateBytesPerSecond * samplePeriodTimeSpan.TotalSeconds);
                            long bytesTransferedInPeriod = Math.Min(objXfer.FileSizeBytes - objXfer.CumulativeBytesTransferred, bytesPossibleToTransferInPeriod);
                            objXfer.CumulativeBytesTransferred += bytesTransferedInPeriod;

                            objXfer.TransferRateBytesPerSecondAtCurrentSample = dblXferRateBytesPerSecond;
                            objXfer.DisplayRateAtCurrentSample = dblCurrentDisplayRate;

                            objXfer.PercentComplete = ((Double)objXfer.CumulativeBytesTransferred / (Double)objXfer.FileSizeBytes);
                            objXfer.IsComplete = (objXfer.CumulativeBytesTransferred >= objXfer.FileSizeBytes);

                            // Fill in the display strings.
                            TimeSpan elapsedTime = objXfer.CurrentSampleTime - objXfer.StartTime;
                            objXfer.DisplayElapsedTime = String.Format("Elapsed time: {0}:{1}", Math.Floor(elapsedTime.TotalMinutes), elapsedTime.Seconds.ToString("00"));

                            int secondsLeft = (int)Math.Ceiling(((objXfer.FileSizeBytes - objXfer.CumulativeBytesTransferred) / objXfer.TransferRateBytesPerSecondAtCurrentSample));
                            TimeSpan timeLeft = new TimeSpan(0, 0, secondsLeft);
                            objXfer.DisplayTimeLeft = String.Format("Time left: {0}:{1}", Math.Floor(timeLeft.TotalMinutes), timeLeft.Seconds.ToString("00"));

                            objXfer.DisplayFileSize = String.Format("File size: {0:#,0}", objXfer.FileSizeBytes);
                        }
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
            // Free the upload list.
            if (ListFilesUploading != null)
            {
                ListFilesUploading.Clear();
                ListFilesUploading = null;
            }

            // Free the download list.
            if (ListFilesDownloading != null)
            {
                ListFilesDownloading.Clear();
                ListFilesDownloading = null;
            }

            // Free the message list.
            if (ListMessages != null)
            {
                ListMessages.Clear();
                ListMessages = null;
            }

            return false;                   // don't cancel the user's request to cancel
        }

        #endregion
    }
}