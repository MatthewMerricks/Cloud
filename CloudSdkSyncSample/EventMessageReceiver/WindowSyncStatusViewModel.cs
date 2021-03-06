﻿using Cloud.Interfaces;
using Cloud.Model;
using Cloud.Static;
using Cloud.Support;
using SampleLiveSync.EventMessageReceiver.Status;
using SampleLiveSync.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SampleLiveSync.EventMessageReceiver
{
    // class WindowSyncStatusViewModel <-- this message receiver acts as view models for multiple wpf controls, i.e. sync status window and growls
    // see other partial class for base summary
    public sealed partial class EventMessageReceiver : NotifiableObject<EventMessageReceiver>, IEventMessageReceiver
    {
        // private fields for WindowSyncStatusViewModel
        // -David
        #region private fields
        private const int MillisecondDelayBetweenUploadParameterProcessing = 500;
        private const int MillisecondDelayBetweenDownloadParameterProcessing = MillisecondDelayBetweenUploadParameterProcessing;
        private int MaxStatusMessages;
        private EventMessageLevel ImportanceFilterNonErrors;
        private EventMessageLevel ImportanceFilterErrors;
        private static readonly TimeSpan MinimumStartupProcessingTime = TimeSpan.FromSeconds(1d);
        private static CLTrace _trace = CLTrace.Instance;

        // needs to be set from settings upon construction
        private double _dblCurrentBandwidthBitsPerSecondUpload = 0d;
        // needs to be set from settings upon construction
        private double _dblCurrentBandwidthBitsPerSecondDownload = 0d;

        private readonly Dictionary<long, int> UploadEventIdToStatusFileTransferIndex = new Dictionary<long, int>();
        private readonly Dictionary<long, int> DownloadEventIdToStatusFileTransferIndex = new Dictionary<long, int>();

        private readonly Dictionary<long, CLStatusFileTransferUpdateParameters> UploadEventIdToQueuedUpdateParameters = new Dictionary<long, CLStatusFileTransferUpdateParameters>();
        private readonly Dictionary<long, CLStatusFileTransferUpdateParameters> DownloadEventIdToQueuedUpdateParameters = new Dictionary<long, CLStatusFileTransferUpdateParameters>();

        private bool UploadProcessingTimerRunning = false;
        private bool DownloadProcessingTimerRunning = false;
        #endregion

        // static message-receiving methods
        // -David
        #region message events callbacks
        void IEventMessageReceiver.UpdateFileUpload(ITransferUpdateMessage e)
        {
            lock (UploadEventIdToQueuedUpdateParameters)
            {
                UploadEventIdToQueuedUpdateParameters[e.EventId] = e.Parameters;

                if (!UploadProcessingTimerRunning)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessUploadParameters, this);

                    UploadProcessingTimerRunning = true;
                }
            }

            e.MarkHandled();
        }

        void IEventMessageReceiver.UpdateFileDownload(ITransferUpdateMessage e)
        {
            lock (DownloadEventIdToQueuedUpdateParameters)
            {
                DownloadEventIdToQueuedUpdateParameters[e.EventId] = e.Parameters;

                if (!DownloadProcessingTimerRunning)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessDownloadParameters, this);

                    DownloadProcessingTimerRunning = true;
                }
            }

            e.MarkHandled();
        }

        void IEventMessageReceiver.AddStatusMessage(IBasicMessage e)
        {
            // forward to private helper
            AddStatusMessage(e.DeviceId,
                e.IsError,
                e.Level,
                e.Message,
                e.SyncboxId);

            // event is handled
            e.MarkHandled();
        }

        /// <summary>
        /// Private helper forwarded from individual message arguments in IEventMessageReceiver.AddStatusMessage,
        /// allows self-recursion for the Application Dispatcher
        /// </summary>
        /// <param name="DeviceId">Unique ID for the device in the Syncbox</param>
        /// <param name="IsError">Whether the message is for an error</param>
        /// <param name="Level">The importance of this message from 1 to 9 (as an enum)</param>
        /// <param name="Message">The actual message</param>
        /// <param name="SyncboxId">ID of the Syncbox</param>
        private void AddStatusMessage(
            string DeviceId,
            bool IsError,
            EventMessageLevel Level,
            string Message,
            Nullable<long> SyncboxId)
        {
            // if this message should not be filtered out, then process adding the status message
            if (IsError
                    && ((int)Level) > ((int)ImportanceFilterErrors) // if this is an error, filter by the error filter
                || (!IsError
                    && ((int)Level) > ((int)ImportanceFilterNonErrors)) // else if this is not an error, filter by the non-error filter

                // I noticed an exception after the application was closed where the Dispatcher had been shut down
                && Application.Current != null
                && Application.Current.Dispatcher != null
                && !Application.Current.Dispatcher.HasShutdownStarted
                && !Application.Current.Dispatcher.HasShutdownFinished)
            {
                // adding the status message must be done under the application dispatcher to change a collection which may be bound to the UI,
                // so if it is already under the dispather then continue processing otherwise queue this method again under the dispatcher

                if (Dispatcher.CurrentDispatcher == Application.Current.Dispatcher)
                {
                    // once the message count reaches the maximum limit, start removing the oldest messages

                    if (ListMessages.Count >= MaxStatusMessages)
                    {
                        for (int currentDeleteIndex = ListMessages.Count - MaxStatusMessages; currentDeleteIndex >= 0; currentDeleteIndex--)
                        {
                            ListMessages.RemoveAt(currentDeleteIndex);
                        }
                    }

                    // add the new message to the end
                    _trace.writeToLog(9, "WindowSyncStatusViewModel: AddStatusMessage: Message: {0}.", Message);
                    ListMessages.Add(new CLStatusMessage()
                    {
                        MessageText = Message
                    });
                }
                else
                {
                    // run this method again with the same parameters under the dispatcher
                    Application.Current.Dispatcher.BeginInvoke((Action<string, bool, EventMessageLevel, string, Nullable<long>>)(AddStatusMessage),

                        // pass through the existing params again for when this method refires under the dispatcher
                        DeviceId,
                        IsError,
                        Level,
                        Message,
                        SyncboxId);
                }
            }
        }
        #endregion

        // static helper private methods
        // -David
        #region private static methods
        private static void ProcessUploadParameters(object state)
        {
            EventMessageReceiver thisReceiver = state as EventMessageReceiver;
            if (thisReceiver == null)
            {
                MessageBox.Show(System.Windows.Application.Current.MainWindow, "Could not cast state as EventMessageReceiver");
            }
            else
            {
                while (true)
                {
                    KeyValuePair<long, CLStatusFileTransferUpdateParameters>[] dequeuedParameters;
                    lock (thisReceiver.UploadEventIdToQueuedUpdateParameters)
                    {
                        dequeuedParameters = thisReceiver.UploadEventIdToQueuedUpdateParameters.ToArray();
                        thisReceiver.UploadEventIdToQueuedUpdateParameters.Clear();
                    }

                    GenericHolder<int> dispatchersFired = new GenericHolder<int>(0);
                    bool atLeastOneDispatcherFired = false;
                    List<Tuple<Action<bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>, bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>> dispatchersToFire =
                        new List<Tuple<Action<bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>, bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>>();

                    foreach (KeyValuePair<long, CLStatusFileTransferUpdateParameters> currentParameter in dequeuedParameters)
                    {
                        bool currentUpdateFired;

                        ProcessUpdateParameters(thisReceiver,
                            dispatchersFired,
                            out currentUpdateFired,
                            dispatchersToFire,
                            currentParameter,
                            isUpload: true);

                        if (currentUpdateFired)
                        {
                            atLeastOneDispatcherFired = true;
                        }
                    }

                    foreach (Tuple<Action<bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>, bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>> currentDispatch
                        in dispatchersToFire)
                    {
                        Application.Current.Dispatcher.BeginInvoke(currentDispatch.Item1,
                            DispatcherPriority.Render,
                            currentDispatch.Item2,
                            currentDispatch.Item3,
                            currentDispatch.Item4,
                            currentDispatch.Item5,
                            currentDispatch.Item6);
                    }

                    if (atLeastOneDispatcherFired)
                    {
                        break;
                    }

                    lock (thisReceiver.UploadEventIdToQueuedUpdateParameters)
                    {
                        if (thisReceiver.UploadEventIdToQueuedUpdateParameters.Count == 0)
                        {
                            thisReceiver.UploadProcessingTimerRunning = false;

                            break;
                        }
                    }

                    Thread.Sleep(MillisecondDelayBetweenUploadParameterProcessing);
                }
            }
        }

        private static void ProcessDownloadParameters(object state)
        {
            EventMessageReceiver thisReceiver = state as EventMessageReceiver;
            if (thisReceiver == null)
            {
                MessageBox.Show(System.Windows.Application.Current.MainWindow, "Could not cast state as EventMessageReceiver");
            }
            else
            {
                while (true)
                {
                    KeyValuePair<long, CLStatusFileTransferUpdateParameters>[] dequeuedParameters;
                    lock (thisReceiver.DownloadEventIdToQueuedUpdateParameters)
                    {
                        dequeuedParameters = thisReceiver.DownloadEventIdToQueuedUpdateParameters.ToArray();
                        thisReceiver.DownloadEventIdToQueuedUpdateParameters.Clear();
                    }

                    GenericHolder<int> dispatchersFired = new GenericHolder<int>(0);
                    bool atLeastOneDispatcherFired = false;
                    List<Tuple<Action<bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>, bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>> dispatchersToFire =
                        new List<Tuple<Action<bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>, bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>>();

                    foreach (KeyValuePair<long, CLStatusFileTransferUpdateParameters> currentParameter in dequeuedParameters)
                    {
                        bool currentUpdateFired;

                        ProcessUpdateParameters(thisReceiver,
                            dispatchersFired,
                            out currentUpdateFired,
                            dispatchersToFire,
                            currentParameter,
                            isUpload: false);

                        if (currentUpdateFired)
                        {
                            atLeastOneDispatcherFired = true;
                        }
                    }

                    foreach (Tuple<Action<bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>, bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>> currentDispatch
                        in dispatchersToFire)
                    {
                        Application.Current.Dispatcher.BeginInvoke(currentDispatch.Item1,
                            DispatcherPriority.Render,
                            currentDispatch.Item2,
                            currentDispatch.Item3,
                            currentDispatch.Item4,
                            currentDispatch.Item5,
                            currentDispatch.Item6);
                    }

                    if (atLeastOneDispatcherFired)
                    {
                        break;
                    }

                    lock (thisReceiver.DownloadEventIdToQueuedUpdateParameters)
                    {
                        if (thisReceiver.DownloadEventIdToQueuedUpdateParameters.Count == 0)
                        {
                            thisReceiver.DownloadProcessingTimerRunning = false;

                            break;
                        }
                    }

                    Thread.Sleep(MillisecondDelayBetweenUploadParameterProcessing);
                }
            }
        }

        private static void ProcessUpdateParameters(EventMessageReceiver thisReceiver, GenericHolder<int> dispatchersFired, out bool atLeastOneDispatcherFired, List<Tuple<Action<bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>, bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>> dispatchersToFire, KeyValuePair<long, CLStatusFileTransferUpdateParameters> currentParameter, bool isUpload)
        {
            atLeastOneDispatcherFired = false; // default to false for output, will be set to true only under certain conditions later
            DateTime currentSampleTime = DateTime.Now;
            TimeSpan elapsedTime = currentSampleTime.Subtract(currentParameter.Value.TransferStartTime);
            if (currentParameter.Value.ByteProgress >= currentParameter.Value.ByteSize
                || elapsedTime.CompareTo(MinimumStartupProcessingTime) >= 0)
            {
                CLStatusFileTransfer transferToUpdate;
                GenericHolder<Nullable<int>> addTransferIndex = new GenericHolder<Nullable<int>>(null);

                Func<bool, DateTime, EventMessageReceiver, GenericHolder<Nullable<int>>, DateTime, long, long, CLStatusFileTransfer> getNewTransferToUpdate = (forUpload, firstSampleTime, innerReceiver, innerTransferIndex, startTransferTime, firstSampleBytes, innerEvent) =>
                {
                    // define an action which will take a holder of the index int and set it to 0 to start or otherwise increment by 1
                    Action<GenericHolder<Nullable<int>>> incrementIndexHolder = holderToIncrement =>
                        {
                            if (holderToIncrement.Value == null)
                            {
                                holderToIncrement.Value = 0; // start by setting to first index
                            }
                            else
                            {
                                holderToIncrement.Value = ((int)holderToIncrement.Value) + 1; // otherwise, increment to next index
                            }
                        };

                    // innerTransferIndex.Value always should start null here (addTransferIndex.Value is the way to access the same data outside of this function)
                    // we will increment it until it's at the index of the first CLStatusFileTransferBlank in the appropriate collection,
                    // or it will be the next available index (equal to the length of the collection)

                    bool transferBlankFound = (forUpload
                        ? innerReceiver.ListFilesUploading
                        : innerReceiver.ListFilesDownloading)

                        // use Any since when you return a true, it will immediately stop enumeration
                        .Any(currentTransferring =>
                            {
                                incrementIndexHolder(innerTransferIndex);

                                // innerTransferIndex.Value should now represent the current index that is being checked as a 'Blank

                                return currentTransferring is CLStatusFileTransferBlank; // return if blank found to stop enumerating and to signal that a blank was found
                            });

                    // no 'Blank found to replace, so index should be equal to the length of the collection for insertion of a new item
                    if (!transferBlankFound)
                    {
                        // simply increment one more time since the previous Any enumeration will have counted up to Length - 1 to check all existing values
                        incrementIndexHolder(innerTransferIndex);
                    }

                    (forUpload
                        ? innerReceiver.UploadEventIdToStatusFileTransferIndex
                        : innerReceiver.DownloadEventIdToStatusFileTransferIndex)[innerEvent] = (int)innerTransferIndex.Value;

                    double initialRateBarMax = Math.Max(1d, (((double)firstSampleBytes) / firstSampleTime.Subtract(startTransferTime).TotalSeconds) * 8 * CLStatusFileTransfer.HighestDisplayRateMultiplier);

                    CLStatusFileTransfer toReturn = new CLStatusFileTransfer()
                    {
                        StartTime = startTransferTime,
                        InitialRateBitsPerSecondForMaxRateBar = initialRateBarMax,
                        HighestRateBitsPerSecond = initialRateBarMax
                    };

                    toReturn.HistorySamples.Add(new KeyValuePair<DateTime, long>(startTransferTime, 0));

                    return toReturn;
                };

                int existingStatusIndex;
                if ((isUpload
                    ? thisReceiver.UploadEventIdToStatusFileTransferIndex
                    : thisReceiver.DownloadEventIdToStatusFileTransferIndex).TryGetValue(currentParameter.Key, out existingStatusIndex))
                {
                    if ((isUpload
                        ? thisReceiver.ListFilesUploading
                        : thisReceiver.ListFilesDownloading).Count > existingStatusIndex)
                    {
                        ICLStatusFileTransfer currentNotifyChanged = (isUpload
                            ? thisReceiver.ListFilesUploading
                            : thisReceiver.ListFilesDownloading)[existingStatusIndex];
                        if (currentNotifyChanged is CLStatusFileTransfer)
                        {
                            transferToUpdate = (CLStatusFileTransfer)currentNotifyChanged;
                        }
                        else
                        {
                            transferToUpdate = getNewTransferToUpdate(isUpload, currentSampleTime, thisReceiver, addTransferIndex, currentParameter.Value.TransferStartTime, currentParameter.Value.ByteProgress, currentParameter.Key);
                        }
                    }
                    else
                    {
                        transferToUpdate = getNewTransferToUpdate(isUpload, currentSampleTime, thisReceiver, addTransferIndex, currentParameter.Value.TransferStartTime, currentParameter.Value.ByteProgress, currentParameter.Key);
                    }
                }
                else
                {
                    transferToUpdate = getNewTransferToUpdate(isUpload, currentSampleTime, thisReceiver, addTransferIndex, currentParameter.Value.TransferStartTime, currentParameter.Value.ByteProgress, currentParameter.Key);
                }

                if (currentParameter.Value.ByteProgress >= currentParameter.Value.ByteSize)
                {
                    transferToUpdate.IsComplete = true;
                    if (addTransferIndex.Value == null)
                    {
                        atLeastOneDispatcherFired = true;
                        dispatchersFired.Value++;

                        dispatchersToFire.Add(new Tuple<Action<bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>, bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>((forUpload, innerTransfer, newIndex, innerReceiver, dispatchCounter) =>
                            {
                                List<ICLStatusFileTransfer> fileTransferBases = new List<ICLStatusFileTransfer>((forUpload
                                        ? innerReceiver.ListFilesUploading
                                        : innerReceiver.ListFilesDownloading).Take(newIndex)
                                    .Concat((forUpload
                                        ? innerReceiver.ListFilesUploading
                                        : innerReceiver.ListFilesDownloading).Skip(newIndex + 1)));

                                // clear and readd to force reset
                                (forUpload
                                    ? innerReceiver.ListFilesUploading
                                    : innerReceiver.ListFilesDownloading).Clear();
                                foreach (ICLStatusFileTransfer toAdd in fileTransferBases.Take(newIndex))
                                {
                                    (forUpload
                                        ? innerReceiver.ListFilesUploading
                                        : innerReceiver.ListFilesDownloading).Add(toAdd);
                                }
                                (forUpload
                                    ? innerReceiver.ListFilesUploading
                                    : innerReceiver.ListFilesDownloading).Add(new CLStatusFileTransferBlank()); // inject a blank as a means of clearing an existing finished transfer
                                foreach (ICLStatusFileTransfer toAdd in fileTransferBases.Skip(newIndex))
                                {
                                    (forUpload
                                        ? innerReceiver.ListFilesUploading
                                        : innerReceiver.ListFilesDownloading).Add(toAdd);
                                }

                                dispatchCounter.Value = dispatchCounter.Value - 1;
                                if (dispatchCounter.Value == 0)
                                {
                                    ThreadPool.UnsafeQueueUserWorkItem((forUpload ? (WaitCallback)ProcessUploadParameters : ProcessDownloadParameters), innerReceiver);
                                }
                            },
                            isUpload,
                            (CLStatusFileTransfer)(isUpload
                                ? thisReceiver.ListFilesUploading
                                : thisReceiver.ListFilesDownloading)[existingStatusIndex],
                            existingStatusIndex,
                            thisReceiver,
                            dispatchersFired));
                    }

                    (isUpload
                        ? thisReceiver.UploadEventIdToStatusFileTransferIndex
                        : thisReceiver.DownloadEventIdToStatusFileTransferIndex).Remove(currentParameter.Key);
                }
                else
                {
                    transferToUpdate.IsDirectionUpload = isUpload;
                    transferToUpdate.SyncRelativePath = currentParameter.Value.RelativePath;
                    transferToUpdate.FileSizeBytes = currentParameter.Value.ByteSize;
                    transferToUpdate.SamplesTaken++;
                    transferToUpdate.CumulativeBytesTransferred = currentParameter.Value.ByteProgress;
                    transferToUpdate.CurrentSampleTime = currentSampleTime;
                    transferToUpdate.HistorySamples.Add(new KeyValuePair<DateTime, long>(currentSampleTime, currentParameter.Value.ByteProgress));

                    KeyValuePair<DateTime, long> historySampleToCompare = transferToUpdate.HistorySamples[Math.Max(0, transferToUpdate.HistorySamples.Count - CLStatusFileTransfer.RateMaxSamplesToCalculate)];
                    double calculatedBytesPerSecond = ((double)(currentParameter.Value.ByteProgress - historySampleToCompare.Value)) / currentSampleTime.Subtract(historySampleToCompare.Key).TotalSeconds;
                    transferToUpdate.TransferRateBytesPerSecondAtCurrentSample = calculatedBytesPerSecond;
                    double calculatedBitsPerSecond = calculatedBytesPerSecond * 8;
                    double calculatedDisplayRate = calculatedBitsPerSecond / transferToUpdate.InitialRateBitsPerSecondForMaxRateBar;
                    if (calculatedBitsPerSecond > transferToUpdate.HighestRateBitsPerSecond)
                    {
                        transferToUpdate.HighestRateBitsPerSecond = calculatedBitsPerSecond;
                        if (calculatedBitsPerSecond > transferToUpdate.InitialRateBitsPerSecondForMaxRateBar)
                        {
                            Application.Current.Dispatcher.BeginInvoke((Action<RateBar.RateGraph, double>)((statusGraph, setDisplayRate) =>
                                {
                                    statusGraph.RateMaximum = setDisplayRate;
                                }),
                                transferToUpdate.StatusGraph,
                                calculatedDisplayRate);
                        }
                    }
                    transferToUpdate.DisplayRateAtCurrentSample = calculatedDisplayRate;
                    transferToUpdate.PercentComplete = (((double)currentParameter.Value.ByteProgress) / ((double)currentParameter.Value.ByteSize));

                    // Fill in the display strings.
                    transferToUpdate.DisplayElapsedTime = String.Format("Elapsed time: {0}:{1}", (int)Math.Floor(elapsedTime.TotalMinutes), elapsedTime.Seconds.ToString("00"));

                    int secondsLeft = (int)Math.Ceiling(((double)(currentParameter.Value.ByteSize - currentParameter.Value.ByteProgress)) / calculatedBytesPerSecond);
                    TimeSpan timeLeft = new TimeSpan(0, 0, secondsLeft);
                    transferToUpdate.DisplayTimeLeft = String.Format("Time left: {0}:{1}", (int)Math.Floor(timeLeft.TotalMinutes), timeLeft.Seconds.ToString("00"));

                    transferToUpdate.DisplayFileSize = String.Format("File size: {0}", Helpers.FormatBytes(currentParameter.Value.ByteSize));

                    if (addTransferIndex.Value != null)
                    {
                        atLeastOneDispatcherFired = true;
                        dispatchersFired.Value++;

                        dispatchersToFire.Add(new Tuple<Action<bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>, bool, CLStatusFileTransfer, int, EventMessageReceiver, GenericHolder<int>>((forUpload, innerTransfer, newIndex, innerReceiver, dispatchCounter) =>
                        {
                            List<ICLStatusFileTransfer> fileTransferBases = new List<ICLStatusFileTransfer>((forUpload
                                    ? innerReceiver.ListFilesUploading
                                    : innerReceiver.ListFilesDownloading).Take(newIndex)
                                .Concat((forUpload
                                    ? innerReceiver.ListFilesUploading
                                    : innerReceiver.ListFilesDownloading).Skip(newIndex + 1)));

                            // clear and readd to force reset
                            (forUpload
                                ? innerReceiver.ListFilesUploading
                                : innerReceiver.ListFilesDownloading).Clear();
                            foreach (ICLStatusFileTransfer toAdd in fileTransferBases.Take(newIndex))
                            {
                                (forUpload
                                    ? innerReceiver.ListFilesUploading
                                    : innerReceiver.ListFilesDownloading).Add(toAdd);
                            }
                            innerTransfer.InitializeRateGraph();
                            (forUpload
                                ? innerReceiver.ListFilesUploading
                                : innerReceiver.ListFilesDownloading).Add(innerTransfer);
                            foreach (ICLStatusFileTransfer toAdd in fileTransferBases.Skip(newIndex))
                            {
                                (forUpload
                                    ? innerReceiver.ListFilesUploading
                                    : innerReceiver.ListFilesDownloading).Add(toAdd);
                            }

                            dispatchCounter.Value = dispatchCounter.Value - 1;
                            if (dispatchCounter.Value == 0)
                            {
                                ThreadPool.UnsafeQueueUserWorkItem((forUpload ? (WaitCallback)ProcessUploadParameters : ProcessDownloadParameters), innerReceiver);
                            }
                        },
                        isUpload,
                        transferToUpdate,
                        (int)addTransferIndex.Value,
                        thisReceiver,
                        dispatchersFired));
                    }
                }
            }
        }
        #endregion

        // setters to run upon construction
        // -David
        #region construction setters
        private readonly ConstructedHolder WindowSyncStatusViewModelConstructed = new ConstructedHolder();
        private static void WindowSyncStatusViewModelConstructionSetters(KeyValuePair<KeyValuePair<EventMessageReceiver, Nullable<EventMessageLevel>>, KeyValuePair<Nullable<EventMessageLevel>, Nullable<int>>> constructionParams)
        {
            if (constructionParams.Key.Key == null)
            {
                throw new NullReferenceException("constructionParams Key Key cannot be null");
            }
            if (constructionParams.Key.Value == null)
            {
                if (constructionParams.Value.Key == null)
                {
                    if (constructionParams.Value.Value == null)
                    {
                        constructionParams.Key.Key.WindowSyncStatusViewModelConstructionSettersInstance();
                    }
                    else
                    {
                        constructionParams.Key.Key.WindowSyncStatusViewModelConstructionSettersInstance(
                            MaxStatusMessages: (int)constructionParams.Value.Value);
                    }
                }
                else if (constructionParams.Value.Value == null)
                {
                    constructionParams.Key.Key.WindowSyncStatusViewModelConstructionSettersInstance(
                        ImportanceFilterErrors: (EventMessageLevel)constructionParams.Value.Key);
                }
                else
                {
                    constructionParams.Key.Key.WindowSyncStatusViewModelConstructionSettersInstance(
                        ImportanceFilterErrors: (EventMessageLevel)constructionParams.Value.Key,
                        MaxStatusMessages: (int)constructionParams.Value.Value);
                }
            }
            else if (constructionParams.Value.Key == null)
            {
                if (constructionParams.Value.Value == null)
                {
                    constructionParams.Key.Key.WindowSyncStatusViewModelConstructionSettersInstance(
                        ImportanceFilterNonErrors: (EventMessageLevel)constructionParams.Key.Value);
                }
                else
                {
                    constructionParams.Key.Key.WindowSyncStatusViewModelConstructionSettersInstance(
                        ImportanceFilterNonErrors: (EventMessageLevel)constructionParams.Key.Value,
                        MaxStatusMessages: (int)constructionParams.Value.Value);
                }
            }
            else if (constructionParams.Value.Value == null)
            {
                constructionParams.Key.Key.WindowSyncStatusViewModelConstructionSettersInstance(
                    ImportanceFilterNonErrors: (EventMessageLevel)constructionParams.Key.Value,
                    ImportanceFilterErrors: (EventMessageLevel)constructionParams.Value.Key);
            }
            else
            {
                constructionParams.Key.Key.WindowSyncStatusViewModelConstructionSettersInstance(
                    ImportanceFilterNonErrors: (EventMessageLevel)constructionParams.Key.Value,
                    ImportanceFilterErrors: (EventMessageLevel)constructionParams.Value.Key,
                    MaxStatusMessages: (int)constructionParams.Value.Value);
            }
        }
        private void WindowSyncStatusViewModelConstructionSettersInstance(EventMessageLevel ImportanceFilterNonErrors = EventMessageLevel.Regular, EventMessageLevel ImportanceFilterErrors = EventMessageLevel.Minor, int MaxStatusMessages = 100)
        {
            // Get the current bandwidth settings.
            if (this._getHistoricBandwidthSettingsDelegate != null)
            {
                _getHistoricBandwidthSettingsDelegate(out _dblCurrentBandwidthBitsPerSecondUpload, out _dblCurrentBandwidthBitsPerSecondDownload);
            }

            this.MaxStatusMessages = MaxStatusMessages;
            this.ImportanceFilterNonErrors = ImportanceFilterNonErrors;
            this.ImportanceFilterErrors = ImportanceFilterErrors;
        }
        #endregion

        #region public methods

        /// <summary>
        /// Call this method to clear the status messages
        /// </summary>
        public void ClearStatusMessages()
        {
            if (Dispatcher.CurrentDispatcher == Application.Current.Dispatcher)
            {
                ListMessages.Clear();
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() => ClearStatusMessages()));
            }
        }

        #endregion

        // code to dispose partial class portion for WindowSyncStatusViewModel
        // -David
        #region disposal
        private void DisposeWindowSyncStatusViewModel()
        {
            // update settings with bandwidth history for next startup
            if (this._setHistoricBandwidthSettingsDelegate != null)
            {
                _setHistoricBandwidthSettingsDelegate(_dblCurrentBandwidthBitsPerSecondUpload, _dblCurrentBandwidthBitsPerSecondDownload);
            }
        }
        #endregion

        #region Bindable Properties

        /// <summary>
        /// Collection of statuses for file transfers for downloads, check (ICLStatusFileTransfer).Visibility for hidden because those are blank placeholders
        /// </summary>
        public ObservableCollection<ICLStatusFileTransfer> ListFilesDownloading
        {
            get
            {
                return _listFilesDownloading;
            }
        }
        private readonly ObservableCollection<ICLStatusFileTransfer> _listFilesDownloading = new ObservableCollection<ICLStatusFileTransfer>();

        /// <summary>
        /// Collection of statuses for file transfers for uploads, check (ICLStatusFileTransfer).Visibility for hidden because those are blank placeholders
        /// </summary>
        public ObservableCollection<ICLStatusFileTransfer> ListFilesUploading
        {
            get
            {
                return _listFilesUploading;
            }
        }
        private readonly ObservableCollection<ICLStatusFileTransfer> _listFilesUploading = new ObservableCollection<ICLStatusFileTransfer>();

        /// <summary>
        /// Collection of status messages
        /// </summary>
        public ObservableCollection<CLStatusMessage> ListMessages
        {
            get
            {
                return _listMessages;
            }
        }
        private readonly ObservableCollection<CLStatusMessage> _listMessages = new ObservableCollection<CLStatusMessage>();

        #endregion
    }
}