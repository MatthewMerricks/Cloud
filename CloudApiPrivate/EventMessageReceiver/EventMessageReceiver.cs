//
//  EventMessageReceiver.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Linq;
using System.Windows.Threading;
using System.Windows.Input;
using CloudApiPrivate.Model;
using CloudApiPublic.Static;
using System.Windows.Media;
using CloudApiPublic.Model;

namespace CloudApiPrivate.EventMessageReceiver
{
    public sealed partial class EventMessageReceiver : NotifiableObject<EventMessageReceiver>, IDisposable
    {
        private const int MessageTimerDelayMilliseconds = 250;
        private const int GrowlProcessMouseCheckerMilliseconds = 250;
        private const double SecondsTillFadeInOnMouseOverIfFadedOut = 1d;

        #region singleton pattern
        public static EventMessageReceiver Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new EventMessageReceiver();
                    }
                    return _instance;
                }
            }
        }
        private static EventMessageReceiver _instance = null;
        private static readonly object InstanceLocker = new object();
        #endregion

        #region public properties
        /// <summary>
        /// Bind with one-time binding when used as ItemSource because reference to collection is readonly
        /// </summary>
        public DelayChangeObservableCollection<EventMessage> GrowlMessages
        {
            get
            {
                return _growlMessages;
            }
        }
        private readonly DelayChangeObservableCollection<EventMessage> _growlMessages = new DelayChangeObservableCollection<EventMessage>();

        public bool GrowlVisible
        {
            get
            {
                return _growlVisible;
            }
            set
            {
                if (_growlVisible != value)
                {
                    _growlVisible = value;
                    NotifyPropertyChanged(parent => parent.GrowlVisible);
                }
            }
        }
        private bool _growlVisible = false;

        public double SecondsTillFadeIn
        {
            get
            {
                return _secondsTillFadeIn;
            }
            set
            {
                if (_secondsTillFadeIn != value)
                {
                    _secondsTillFadeIn = value;
                    NotifyPropertyChanged(parent => parent.SecondsTillFadeIn);
                }
            }
        }
        private double _secondsTillFadeIn = 0d;

        public double SecondsTillStartFadeOut
        {
            get
            {
                return _secondsTillStartFadeOut;
            }
            set
            {
                if (_secondsTillStartFadeOut != value)
                {
                    _secondsTillStartFadeOut = value;
                    NotifyPropertyChanged(parent => parent.SecondsTillStartFadeOut);
                }
            }
        }
        private double _secondsTillStartFadeOut = 0d;

        public double SecondsTillCompleteFadeOut
        {
            get
            {
                return _secondsTillCompleteFadeOut;
            }
            set
            {
                if (_secondsTillCompleteFadeOut != value)
                {
                    _secondsTillCompleteFadeOut = value;
                    NotifyPropertyChanged(parent => parent.SecondsTillCompleteFadeOut);
                }
            }
        }
        private double _secondsTillCompleteFadeOut = 0d;

        public ICommand ClickedGrowlCommand
        {
            get
            {
                return _clickedGrowlCommand ?? (_clickedGrowlCommand =
                    new RelayCommand<object>(ClickedGrowl));
            }
        }
        private ICommand _clickedGrowlCommand = null;

        public ICommand ClosedGrowlCommand
        {
            get
            {
                return _closedGrowlCommand ?? (_closedGrowlCommand =
                    new RelayCommand<object>(ClosedGrowl));
            }
        }
        private ICommand _closedGrowlCommand = null;

        public ICommand MouseEnteredGrowlCommand
        {
            get
            {
                return _mouseEnteredGrowlCommand ?? (_mouseEnteredGrowlCommand =
                    new RelayCommand<UIElement>(MouseEnteredGrowl));
            }
        }
        private ICommand _mouseEnteredGrowlCommand = null;
        #endregion

        #region private fields
        private DownloadingMessage downloading = null;
        private DownloadedMessage downloaded = null;
        private UploadingMessage uploading = null;
        private UploadedMessage uploaded = null;

        private Nullable<DateTime> firstFadeInCompletion = null;
        private Nullable<DateTime> lastFadeOutStart = null;
        private Nullable<DateTime> lastFadeOutCompletion = null;

        #region detecting when mouse is over the growl
        private readonly GenericHolder<bool> growlCapturedMouse = new GenericHolder<bool>(false);
        private bool keepGrowlOpaque = false;
        #endregion

        private bool isDisposed = false;

        #endregion

        private EventMessageReceiver()
        {
            // changes made upon construction for other partial class portions should be handled via a ConstructedHolder

            // run custom construction setter method for the partial class portion WindowSyncStatusViewModel
            WindowSyncStatusViewModelConstructed.MarkConstructed(WindowSyncStatusViewModelConstructionSetters, this);

            MessageEvents.NewEventMessage += MessageEvents_NewEventMessage;
            MessageEvents.DownloadingCountSet += SetDownloadingCount;
            MessageEvents.UploadingCountSet += SetUploadingCount;
            MessageEvents.DownloadedCountIncremented += IncrementDownloadedCount;
            MessageEvents.UploadedCountIncremented += IncrementUploadedCount;
        }

        #region MessageEvents callbacks
        private void MessageEvents_NewEventMessage(object sender, EventMessageArgs e)
        {
            if (e.IsError)
            {
                if (((int)e.Level) > ((int)EventMessageLevel.Minor))
                {
                    DisplayErrorGrowl(e.Message);
                }
            }
            else if (((int)e.Level) > ((int)EventMessageLevel.Regular))
            {
                DisplayInformationalGrowl(e.Message);
            }

            e.MarkHandled();
        }

        private void DisplayErrorGrowl(string message)
        {
            Application.Current.Dispatcher.BeginInvoke((Action<EventMessage>)AddEventMessageToGrowl,
                (EventMessage)(new ErrorMessage(message)));
        }

        private void DisplayInformationalGrowl(string message)
        {
            Application.Current.Dispatcher.BeginInvoke((Action<EventMessage>)AddEventMessageToGrowl,
                (EventMessage)(new InformationalMessage(message)));
        }

        private void SetDownloadingCount(object sender, SetCountArgs e)
        {
            lock (GrowlMessages)
            {
                bool newMessage;

                if (downloading == null)
                {
                    downloading = new DownloadingMessage(e.NewCount);
                    newMessage = true;
                }
                else
                {
                    if (!downloading.SetCount(e.NewCount))
                    {
                        return;
                    }

                    newMessage = false;
                }

                if (newMessage)
                {
                    Application.Current.Dispatcher.BeginInvoke((Action<EventMessage>)AddEventMessageToGrowl,
                        (EventMessage)downloading);
                }
            }

            e.MarkHandled();
        }

        private void IncrementDownloadedCount(object sender, IncrementCountArgs e)
        {
            lock (GrowlMessages)
            {
                bool newMessage;

                if (downloaded == null)
                {
                    downloaded = new DownloadedMessage(e.IncrementAmount);
                    newMessage = true;
                }
                else
                {
                    downloaded.IncrementCount(e.IncrementAmount);

                    newMessage = false;
                }

                if (newMessage)
                {
                    Application.Current.Dispatcher.BeginInvoke((Action<EventMessage>)AddEventMessageToGrowl,
                        (EventMessage)downloaded);
                }
            }

            e.MarkHandled();
        }

        private void SetUploadingCount(object sender, SetCountArgs e)
        {
            lock (GrowlMessages)
            {
                bool newMessage;

                if (uploading == null)
                {
                    uploading = new UploadingMessage(e.NewCount);
                    newMessage = true;
                }
                else
                {
                    if (!uploading.SetCount(e.NewCount))
                    {
                        return;
                    }

                    newMessage = false;
                }

                if (newMessage)
                {
                    Application.Current.Dispatcher.BeginInvoke((Action<EventMessage>)AddEventMessageToGrowl,
                        (EventMessage)uploading);
                }
            }

            e.MarkHandled();
        }

        private void IncrementUploadedCount(object sender, IncrementCountArgs e)
        {
            lock (GrowlMessages)
            {
                bool newMessage;

                if (uploaded == null)
                {
                    uploaded = new UploadedMessage(e.IncrementAmount);
                    newMessage = true;
                }
                else
                {
                    uploaded.IncrementCount(e.IncrementAmount);

                    newMessage = false;
                }

                if (newMessage)
                {
                    Application.Current.Dispatcher.BeginInvoke((Action<EventMessage>)AddEventMessageToGrowl,
                        (EventMessage)uploaded);
                }
            }

            e.MarkHandled();
        }
        #endregion

        #region IDisposable members
        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~EventMessageReceiver()
        {
            Dispose(false);
        }
        // Standard IDisposable implementation based on MSDN System.IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region private methods
        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            // lock on instance locker for changing EventMessageReceiver so it cannot be stopped/started simultaneously
            lock (InstanceLocker)
            {
                if (!isDisposed)
                {
                    // set delay completed so processing will not fire
                    isDisposed = true;

                    // Dispose local unmanaged resources last
                }
            }
        }

        private void ClickedGrowl(object state)
        {
            ClosedGrowl(state);

            ICommand castState = state as ICommand;
            if (castState != null)
            {
                try
                {
                    if (castState.CanExecute(this))
                    {
                        castState.Execute(this);
                    }
                }
                catch
                {
                }
            }
        }

        private void ClosedGrowl(object state)
        {
            lock (GrowlMessages)
            lock (growlCapturedMouse)
            {
                GrowlVisible = false;
                DateTime closeTime = DateTime.UtcNow;

                foreach (EventMessage currentGrowl in GrowlMessages)
                {
                    currentGrowl.CompleteFadeOut = currentGrowl.StartFadeOut = currentGrowl.FadeInCompletion = closeTime;
                }

                keepGrowlOpaque = false;
                growlCapturedMouse.Value = false;
            }
        }

        #region detecting when mouse is over the growl
        private void MouseEnteredGrowl(UIElement growlElement)
        {
            bool processEnter;
            lock (growlCapturedMouse)
            {
                if (!keepGrowlOpaque)
                {
                    if (!growlCapturedMouse.Value)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(ProcessGrowlCheckingMouse, growlElement);
                        growlCapturedMouse.Value = true;
                    }
                    processEnter = keepGrowlOpaque = true;
                }
                else
                {
                    processEnter = false;
                }
            }

            if (processEnter)
            {
                lock (GrowlMessages)
                {
                    DateTime currentTime = DateTime.UtcNow;
                    DateTime earliestFadeIn = DateTime.MaxValue;

                    bool foundOpaqueOrFadeOut = false;

                    foreach (EventMessage currentGrowl in GrowlMessages)
                    {
                        if (currentTime.CompareTo(currentGrowl.FadeInCompletion) < 0)
                        {
                            if (earliestFadeIn.CompareTo(currentGrowl.FadeInCompletion) < 0)
                            {
                                earliestFadeIn = currentGrowl.FadeInCompletion;
                            }
                        }
                        else
                        {
                            foundOpaqueOrFadeOut = true;
                            break;
                        }
                    }

                    if (foundOpaqueOrFadeOut)
                    {
                        SecondsTillFadeIn = 0d;
                        SecondsTillStartFadeOut = 0d;
                        SecondsTillCompleteFadeOut = 0d;

                        SecondsTillFadeIn = SecondsTillFadeInOnMouseOverIfFadedOut;
                    }
                    else if (firstFadeInCompletion == null
                        || ((DateTime)firstFadeInCompletion).CompareTo(earliestFadeIn) != 0)
                    {
                        SecondsTillFadeIn = 0d;
                        SecondsTillStartFadeOut = 0d;
                        SecondsTillCompleteFadeOut = 0d;

                        SecondsTillFadeIn = earliestFadeIn.Subtract(currentTime).TotalSeconds;
                    }
                }
            }
        }

        private static void ProcessGrowlCheckingMouse(object state)
        {
            FrameworkElement castState = state as FrameworkElement;
            if (castState == null)
            {
                MessageBox.Show("Unable to cast state as FrameworkElement in ProcessGrowlCheckingMouse");
            }
            else
            {
                EventMessageReceiver thisReceiver = Instance;

                while (true)
                {
                    lock (thisReceiver)
                    {
                        if (thisReceiver.isDisposed)
                        {
                            return;
                        }
                    }

                    lock (thisReceiver.growlCapturedMouse)
                    {
                        if (!thisReceiver.growlCapturedMouse.Value)
                        {
                            return;
                        }
                    }

                    GenericHolder<bool> stopProcessing = new GenericHolder<bool>(false);

                    lock (stopProcessing)
                    {
                        Application.Current.Dispatcher.BeginInvoke((Action<object>)(combinedState =>
                            {
                                Nullable<KeyValuePair<GenericHolder<bool>, FrameworkElement>> castCombined = combinedState as Nullable<KeyValuePair<GenericHolder<bool>, FrameworkElement>>;
                                if (castCombined == null)
                                {
                                    MessageBox.Show("Unable to cast combinedState as KeyValuePair<GenericHolder<bool>, UIElement> in ProcessGrowlCheckingMouse");
                                }
                                else
                                {

                                    lock (thisReceiver.growlCapturedMouse)
                                    {
                                        if (thisReceiver.GrowlCheckIfMouseLeft(((KeyValuePair<GenericHolder<bool>, FrameworkElement>)combinedState).Value))
                                        {
                                            ((KeyValuePair<GenericHolder<bool>, FrameworkElement>)combinedState).Key.Value = true;
                                            thisReceiver.growlCapturedMouse.Value = false;
                                            thisReceiver.keepGrowlOpaque = false;
                                        }
                                    }

                                    lock (((KeyValuePair<GenericHolder<bool>, FrameworkElement>)combinedState).Key)
                                    {
                                        Monitor.Pulse(((KeyValuePair<GenericHolder<bool>, FrameworkElement>)combinedState).Key);
                                    }
                                }
                            }),
                            new KeyValuePair<GenericHolder<bool>, FrameworkElement>(stopProcessing, castState));

                        Monitor.Wait(stopProcessing);
                    }

                    if (stopProcessing.Value)
                    {
                        return;
                    }

                    Thread.Sleep(GrowlProcessMouseCheckerMilliseconds);
                }
            }
        }

        private bool GrowlCheckIfMouseLeft(FrameworkElement growlElement)
        {
            Point pointOnElement = Helpers.CorrectGetPosition(growlElement);

            if (pointOnElement.X < 0
                || pointOnElement.Y < 0
                || pointOnElement.X >= growlElement.Width
                || pointOnElement.Y >= growlElement.Height)
            {
                lock (GrowlMessages)
                {
                    if (GrowlMessages.Count > 0)
                    {
                        DateTime currentTime = DateTime.UtcNow;
                        TimeSpan latestStartFadeOut = TimeSpan.Zero;
                        TimeSpan latestCompleteFadeOut = TimeSpan.Zero;

                        foreach (EventMessage currentGrowl in GrowlMessages)
                        {
                            TimeSpan timeLeftToFadeIn;
                            if (currentTime.CompareTo(currentGrowl.FadeInCompletion) < 0)
                            {
                                timeLeftToFadeIn = currentGrowl.FadeInCompletion.Subtract(currentTime);
                            }
                            else
                            {
                                timeLeftToFadeIn = TimeSpan.Zero;
                            }

                            TimeSpan currentStartFadeOut = currentGrowl.StartFadeOut.Subtract(currentGrowl.FadeInCompletion)
                                .Add(timeLeftToFadeIn);
                            if (currentStartFadeOut.CompareTo(latestStartFadeOut) > 0)
                            {
                                latestStartFadeOut = currentStartFadeOut;
                            }

                            TimeSpan currentCompleteFadeOut = currentGrowl.CompleteFadeOut.Subtract(currentGrowl.FadeInCompletion)
                                .Add(timeLeftToFadeIn);
                            if (currentCompleteFadeOut.CompareTo(latestCompleteFadeOut) > 0)
                            {
                                latestCompleteFadeOut = currentCompleteFadeOut;
                            }
                        }

                        DateTime newStartFadeOut = currentTime.Add(latestStartFadeOut);
                        DateTime newCompleteFadeOut = currentTime.Add(latestCompleteFadeOut);

                        foreach (EventMessage currentGrowl in GrowlMessages)
                        {
                            currentGrowl.FadeInCompletion = newStartFadeOut.Subtract(currentGrowl.StartFadeOut.Subtract(currentGrowl.FadeInCompletion));
                            currentGrowl.StartFadeOut = newStartFadeOut;
                            currentGrowl.CompleteFadeOut = newCompleteFadeOut;
                        }

                        StartMessageTimerIfNeeded();
                    }
                }

                return true;
            }
            return false;
        }
        #endregion

        private void AddEventMessageToGrowl(EventMessage toAdd)
        {
            if (Dispatcher.CurrentDispatcher != Application.Current.Dispatcher)
            {
                MessageBox.Show("Cannot add growl message from any Dispatcher other than the Application's Dispatcher");
            }
            else
            {
                lock (this)
                {
                    if (isDisposed)
                    {
                        return;
                    }
                }

                lock (GrowlMessages)
                {
                    GrowlMessages.LockCollectionChanged();
                    GrowlMessages.Add(toAdd);
                    StartMessageTimerIfNeeded();
                    GrowlMessages.UnlockCollectionChanged();
                }
            }
        }

        private void StartMessageTimerIfNeeded()
        {
            lock (MessageTimerLocker)
            {
                if (!MessageTimerRunning)
                {
                    MessageTimerRunning = true;
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessMessageTimer, this);
                }
            }
        }
        private bool MessageTimerRunning = false;
        private readonly object MessageTimerLocker = new object();

        private bool CalculateGrowlFadeTimes()
        {
            lock (GrowlMessages)
            {
                DateTime toCompare = DateTime.UtcNow;
                DateTime earliestToFadeIn = DateTime.MaxValue;
                DateTime latestToStartFadeOut = DateTime.MinValue;
                DateTime latestToFinishFadeOut = DateTime.MinValue;

                bool foundFadeOut = false;
                bool foundOpaque = false;
                bool foundFadeIn = false;

                List<EventMessage> completedMessages = new List<EventMessage>();

                foreach (EventMessage currentGrowl in GrowlMessages)
                {
                    if (DateTime.Compare(toCompare,
                        currentGrowl.FadeInCompletion) < 0)
                    {
                        if (!foundOpaque)
                        {
                            foundFadeIn = true;

                            if (DateTime.Compare(earliestToFadeIn,
                                currentGrowl.FadeInCompletion) > 0)
                            {
                                earliestToFadeIn = currentGrowl.FadeInCompletion;
                            }
                        }

                        if (DateTime.Compare(latestToStartFadeOut,
                            currentGrowl.StartFadeOut) < 0)
                        {
                            latestToStartFadeOut = currentGrowl.StartFadeOut;
                        }
                    }
                    else if (DateTime.Compare(toCompare,
                        currentGrowl.StartFadeOut) < 0)
                    {
                        foundOpaque = true;

                        if (DateTime.Compare(latestToStartFadeOut,
                            currentGrowl.StartFadeOut) < 0)
                        {
                            latestToStartFadeOut = currentGrowl.StartFadeOut;
                        }
                    }
                    else if (DateTime.Compare(toCompare,
                            currentGrowl.CompleteFadeOut) < 0)
                    {
                        if (!foundFadeIn
                            && !foundOpaque)
                        {
                            foundFadeOut = true;

                            if (DateTime.Compare(latestToFinishFadeOut,
                                currentGrowl.CompleteFadeOut) < 0)
                            {
                                latestToFinishFadeOut = currentGrowl.CompleteFadeOut;
                            }
                        }
                    }
                    else if (currentGrowl.ShouldRemove)
                    {
                        completedMessages.Add(currentGrowl);
                    }
                }

                int remainingMessageCount = GrowlMessages.Count;

                if (foundFadeIn
                    || foundFadeOut
                    || foundOpaque)
                {
                    GrowlVisible = true;

                    if (foundOpaque)
                    {
                        firstFadeInCompletion = null;
                        lastFadeOutCompletion = null;

                        if (lastFadeOutStart == null
                            || ((DateTime)lastFadeOutStart).CompareTo(latestToStartFadeOut) != 0)
                        {
                            SecondsTillFadeIn = 0d;
                            SecondsTillStartFadeOut = 0d;
                            SecondsTillCompleteFadeOut = 0d;

                            SecondsTillStartFadeOut = latestToStartFadeOut.Subtract(toCompare).TotalSeconds;

                            lastFadeOutStart = latestToStartFadeOut;
                        }
                    }
                    else if (foundFadeIn)
                    {
                        lastFadeOutStart = null;
                        lastFadeOutCompletion = null;

                        if (firstFadeInCompletion == null
                            || ((DateTime)firstFadeInCompletion).CompareTo(earliestToFadeIn) != 0)
                        {
                            SecondsTillFadeIn = 0d;
                            SecondsTillStartFadeOut = 0d;
                            SecondsTillCompleteFadeOut = 0d;

                            SecondsTillFadeIn = earliestToFadeIn.Subtract(toCompare).TotalSeconds;

                            firstFadeInCompletion = earliestToFadeIn;
                        }
                    }
                    else
                    {
                        firstFadeInCompletion = null;
                        lastFadeOutStart = null;

                        if (lastFadeOutCompletion == null
                            || ((DateTime)lastFadeOutCompletion).CompareTo(latestToFinishFadeOut) != 0)
                        {
                            SecondsTillFadeIn = 0d;
                            SecondsTillStartFadeOut = 0d;
                            SecondsTillCompleteFadeOut = 0d;

                            SecondsTillCompleteFadeOut = latestToFinishFadeOut.Subtract(toCompare).TotalSeconds;

                            lastFadeOutCompletion = latestToFinishFadeOut;
                        }
                    }
                }
                else
                {
                    firstFadeInCompletion = null;
                    lastFadeOutStart = null;
                    lastFadeOutCompletion = null;

                    SecondsTillFadeIn = 0d;
                    SecondsTillStartFadeOut = 0d;
                    SecondsTillCompleteFadeOut = 0d;

                    GrowlVisible = false;

                    remainingMessageCount -= completedMessages.Count;
                    if (completedMessages.Count > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke((Action<IEnumerable<EventMessage>>)RemoveEventMessagesFromGrowl,
                            (IEnumerable<EventMessage>)completedMessages);
                    }
                }

                return remainingMessageCount != 0;
            }
        }

        private void RemoveEventMessagesFromGrowl(IEnumerable<EventMessage> toRemove)
        {
            if (Dispatcher.CurrentDispatcher != Application.Current.Dispatcher)
            {
                MessageBox.Show("Cannot remove growl messages from any Dispatcher other than the Application's Dispatcher");
            }
            else
            {
                lock (this)
                {
                    if (isDisposed)
                    {
                        return;
                    }
                }

                EventMessage[] toRemoveArray;
                if (toRemove != null
                    && (toRemoveArray = toRemove.ToArray()).Length > 0)
                {
                    lock (GrowlMessages)
                    {
                        GrowlMessages.LockCollectionChanged();
                        for (int removeIndex = 0; removeIndex < toRemoveArray.Length; removeIndex++)
                        {
                            if (toRemoveArray[removeIndex] is DownloadingMessage)
                            {
                                downloading = null;
                            }
                            else if (toRemoveArray[removeIndex] is DownloadedMessage)
                            {
                                downloaded = null;
                            }
                            else if (toRemoveArray[removeIndex] is UploadingMessage)
                            {
                                uploading = null;
                            }
                            else if (toRemoveArray[removeIndex] is UploadedMessage)
                            {
                                uploaded = null;
                            }

                            GrowlMessages.Remove(toRemoveArray[removeIndex]);
                        }
                        GrowlMessages.UnlockCollectionChanged();
                    }
                }
            }
        }

        private static void ProcessMessageTimer(object state)
        {
            EventMessageReceiver currentReceiver = state as EventMessageReceiver;
            if (currentReceiver == null)
            {
                MessageBox.Show("Unable to display growls: currentReceiver cannot be null");
            }
            else
            {
                bool foundMessage = true;

                while (foundMessage)
                {
                    lock (currentReceiver)
                    {
                        if (currentReceiver.isDisposed)
                        {
                            return;
                        }
                    }

                    bool skipCalculate;
                    lock (currentReceiver.growlCapturedMouse)
                    {
                        skipCalculate = currentReceiver.keepGrowlOpaque;
                    }

                    if (!skipCalculate)
                    {
                        foundMessage = currentReceiver.CalculateGrowlFadeTimes();
                    }

                    if (foundMessage)
                    {
                        Thread.Sleep(MessageTimerDelayMilliseconds);
                    }
                }

                lock (currentReceiver.MessageTimerLocker)
                {
                    bool terminateProcessing;

                    lock (currentReceiver.GrowlMessages)
                    {
                        terminateProcessing = currentReceiver.GrowlMessages.Count == 0;
                    }

                    if (terminateProcessing)
                    {
                        currentReceiver.MessageTimerRunning = false;
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(ProcessMessageTimer, state);
                    }
                }
            }
        }
        #endregion

        #region status view

        #endregion
    }
}