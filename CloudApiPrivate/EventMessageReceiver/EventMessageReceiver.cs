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

namespace CloudApiPrivate.EventMessageReceiver
{
    public sealed class EventMessageReceiver : NotifiableObject<EventMessageReceiver>
    {
        private const int MessageTimerDelayMilliseconds = 250;

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
        public readonly DelayChangeObservableCollection<EventMessage> GrowlMessages = new DelayChangeObservableCollection<EventMessage>();

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
        #endregion

        #region private fields
        private DownloadingMessage downloading = null;
        #endregion

        private EventMessageReceiver() { }

        #region public static methods
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void SetDownloadingCount(uint newCount)
        {
            EventMessageReceiver thisReceiver = Instance;

            bool newMessage;

            if (thisReceiver.downloading == null)
            {
                thisReceiver.downloading = new DownloadingMessage(newCount);
                newMessage = true;
            }
            else
            {
                if (!thisReceiver.downloading.SetCount(newCount))
                {
                    return;
                }

                newMessage = false;
            }

            if (newMessage)
            {
                thisReceiver.AddEventMessageToGrowl(thisReceiver.downloading);
            }
        }
        #endregion

        #region private methods
        private void AddEventMessageToGrowl(EventMessage toAdd)
        {
            lock (GrowlMessages)
            {
                GrowlMessages.LockCollectionChanged();
                GrowlMessages.Add(toAdd);
                StartMessageTimerIfNeeded();
                GrowlMessages.UnlockCollectionChanged();
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
            SecondsTillFadeIn = 0d;
            SecondsTillStartFadeOut = 0d;
            SecondsTillCompleteFadeOut = 0d;

            lock (GrowlMessages)
            {
                DateTime toCompare = DateTime.UtcNow;
                DateTime latestToFadeIn = DateTime.MinValue;
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

                            if (DateTime.Compare(latestToFadeIn,
                                currentGrowl.FadeInCompletion) < 0)
                            {
                                latestToFadeIn = currentGrowl.FadeInCompletion;
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
                    else
                    {
                        completedMessages.Add(currentGrowl);
                    }
                }

                if (foundFadeIn
                    || foundFadeOut
                    || foundOpaque)
                {
                    GrowlVisible = true;

                    if (foundOpaque)
                    {
                        SecondsTillStartFadeOut = latestToStartFadeOut.Subtract(toCompare).TotalSeconds;
                    }
                    else if (foundFadeIn)
                    {
                        SecondsTillFadeIn = latestToFadeIn.Subtract(toCompare).TotalSeconds;
                    }
                    else
                    {
                        _secondsTillCompleteFadeOut = latestToFinishFadeOut.Subtract(toCompare).TotalSeconds;
                    }
                }
                else
                {
                    GrowlVisible = false;
                }

                if (completedMessages.Count > 0)
                {
                    GrowlMessages.LockCollectionChanged();
                    foreach (EventMessage toRemove in completedMessages)
                    {
                        if (toRemove is DownloadingMessage)
                        {
                            if (((DownloadingMessage)toRemove).CurrentCount == 0)
                            {
                                downloading = null;
                                GrowlMessages.Remove(toRemove);
                            }
                        }
                        else
                        {
                            GrowlMessages.Remove(toRemove);
                        }
                    }
                    GrowlMessages.UnlockCollectionChanged();
                }

                return GrowlMessages.Count != 0;
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
                    foundMessage = currentReceiver.CalculateGrowlFadeTimes();

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
    }
}