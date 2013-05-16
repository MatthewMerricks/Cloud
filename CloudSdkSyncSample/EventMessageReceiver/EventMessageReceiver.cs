using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Linq;
using System.Windows.Threading;
using System.Windows.Input;
using Cloud.Static;
using System.Windows.Media;
using Cloud.Model;
using Cloud.Support;
using Cloud.Interfaces;

namespace SampleLiveSync.EventMessageReceiver
{
    /// <summary>
    /// View model for views to display changes from status changes, such as growls and sync status; split into partial classes for view-specific portions (i.e. Sync Status window)
    /// </summary>
    public sealed partial class EventMessageReceiver : NotifiableObject<EventMessageReceiver>, IDisposable, IEventMessageReceiver
    {
        // timer repeat delay for processing loop which recalculates timing for animations for fading growls in and out
        private const int MessageTimerDelayMilliseconds = 250;
        // timer repeat delay for processing whether the mouse cursor is within the growl window
        private const int GrowlProcessMouseCheckerMilliseconds = 250;
        // time until growl fades back into fully opaque if it was fading out upon mouse-over
        private const double SecondsTillFadeInOnMouseOverIfFadedOut = 1d;
        private readonly object _locker = new object();

        // Delegates to handle saving the average bandwidth to settings.
        public delegate void GetHistoricBandwidthSettings(out double historicUploadBandwidthBitsPS, out double historicDownloadBandwidthBitsPS);
        public delegate void SetHistoricBandwidthSettings(double historicUploadBandwidthBitsPS, double historicDownloadBandwidthBitsPS);

        #region internal properties (used to be public, but we're not sure about exposing growls)
        /// <summary>
        /// Retrieves the collection of growl messages for display;
        /// bind with one-time binding when used as ItemSource because reference to collection is readonly
        /// </summary>
        internal ObservableCollection<EventMessage> GrowlMessages
        {
            get
            {
                return _growlMessages;
            }
        }
        // Create the collection for storing growl messages for display
        private readonly DelayChangeObservableCollection<EventMessage> _growlMessages = new DelayChangeObservableCollection<EventMessage>();

        /// <summary>
        /// Retrieves whether the growl messages should be visible (meaning the growl window should not be completely faded out) which should be bound to display visibility; notifies on property change
        /// </summary>
        internal bool GrowlVisible
        {
            // retrieves the visibility
            get
            {
                return _growlVisible;
            }
            // (privately) sets the visibility and notifies
            private set
            {
                if (_growlVisible != value)
                {
                    _growlVisible = value;
                    NotifyPropertyChanged(parent => parent.GrowlVisible);
                }
            }
        }
        // define whether the growl messages should be visible, defaulting to not visible
        private bool _growlVisible = false;

        /// <summary>
        /// Retrieves the amount of seconds until the growl messages should be faded in and completely opaque which should be bound to an animation timer; notifies on property change
        /// </summary>
        internal double SecondsTillFadeIn
        {
            // retrieves the amount of seconds till complete fade in
            get
            {
                return _secondsTillFadeIn;
            }
            // (privately) sets the amount of seconds till complete fade in and notifies
            private set
            {
                if (_secondsTillFadeIn != value)
                {
                    _secondsTillFadeIn = value;
                    NotifyPropertyChanged(parent => parent.SecondsTillFadeIn);
                }
            }
        }
        // define the amount of seconds till complete fade in, defaulting to none
        private double _secondsTillFadeIn = 0d;

        /// <summary>
        /// Retrieves the amount of seconds for the growl messages to remain completely opaque before starting to fade out which should be bound to an animation timer; notifies on property change
        /// </summary>
        internal double SecondsTillStartFadeOut
        {
            // retrieves the amount of seconds to remain completely opaque
            get
            {
                return _secondsTillStartFadeOut;
            }
            // (privately) sets the amount of seconds to remain completely opaque and notifies
            private set
            {
                if (_secondsTillStartFadeOut != value)
                {
                    _secondsTillStartFadeOut = value;
                    NotifyPropertyChanged(parent => parent.SecondsTillStartFadeOut);
                }
            }
        }
        // define the amount of seconds for the growl messages to remain completely opaque, defaulting to none
        private double _secondsTillStartFadeOut = 0d;

        /// <summary>
        /// Retrieves the amount of seconds before the growl messages completely fade out which should be bound to an animation timer; notifies on property change
        /// </summary>
        internal double SecondsTillCompleteFadeOut
        {
            // retrieves the amount of seconds before fade out
            get
            {
                return _secondsTillCompleteFadeOut;
            }
            // (privately) sets the amount of seconds before fade out and notifies
            private set
            {
                if (_secondsTillCompleteFadeOut != value)
                {
                    _secondsTillCompleteFadeOut = value;
                    NotifyPropertyChanged(parent => parent.SecondsTillCompleteFadeOut);
                }
            }
        }
        // define the amount of seconds for the growl messages to completely fade out, defaulting to none
        private double _secondsTillCompleteFadeOut = 0d;

        /// <summary>
        /// Retrieves the command which needs to be passed another ICommand as a command parameter which it will fire with this message receiver as its parameter;
        /// this command should be bound to an action for clicking on the growl;
        /// this will also trigger the action for closing the growl
        /// </summary>
        internal ICommand ClickedGrowlCommand
        {
            get
            {
                return _clickedGrowlCommand ?? (_clickedGrowlCommand =
                    new RelayCommand<object>(ClickedGrowl));
            }
        }
        // defines the command for binding the action for clicking on the growl, defaulting to null to be set upon first retrieval
        private ICommand _clickedGrowlCommand = null;

        /// <summary>
        /// Retrieves the command which does not use any parameter which will go through all messages and set their times to expire so the growl will no longer be visible;
        /// this command should be bound to an action for closing the growl
        /// </summary>
        internal ICommand ClosedGrowlCommand
        {
            get
            {
                return _closedGrowlCommand ?? (_closedGrowlCommand =
                    new RelayCommand<object>(ClosedGrowl));
            }
        }
        // defines the command for binding the action for closing the growl, defaulting to null to be set upon first retrieval
        private ICommand _closedGrowlCommand = null;

        /// <summary>
        /// Retrieves the command which needs the element from the visual tree for the entire growl which will be used to track when the mouse is inside the growl to keep it opaque;
        /// this command should be bound to an action for the mouse entering the element from the visual tree for the entire growl
        /// </summary>
        internal ICommand MouseEnteredGrowlCommand
        {
            get
            {
                return _mouseEnteredGrowlCommand ?? (_mouseEnteredGrowlCommand =
                    new RelayCommand<FrameworkElement>(MouseEnteredGrowl));
            }
        }
        // defines the command for binding the action for the mouse entering the growl, defaulting to null to be set upon first retrieval
        private ICommand _mouseEnteredGrowlCommand = null;
        #endregion

        #region private fields
        // define the message for setting the number of downloading files which can dynamically change to update the growl, defaulting to none
        private DownloadingMessage downloading = null;
        // define the message for incrementing the downloaded file count which can dynamically change to update the growl, defaulting to none
        private DownloadedMessage downloaded = null;
        // define the message for setting the number of uploading files which can dynamically change to update the growl, defaulting to none
        private UploadingMessage uploading = null;
        // define the message for incrementing the uploaded file cound which can dynamically change to update the growl, defaulting to none
        private UploadedMessage uploaded = null;

        // define the time at which the growl will finish fading into being completely opaque, defaulting to none
        private Nullable<DateTime> firstFadeInCompletion = null;
        // define the time for which the growl will remain completely opaque before starting to fade out, defaulting to none
        private Nullable<DateTime> lastFadeOutStart = null;
        // define the time at which the growl will have completed faded out when it will no longer be visible, defaulting to none
        private Nullable<DateTime> lastFadeOutCompletion = null;

        // Identifies this EventMessageReceiver in the MessageEvents filter
        private readonly long SyncboxId;
        private readonly string DeviceId;

        // Delegates
        private readonly GetHistoricBandwidthSettings _getHistoricBandwidthSettingsDelegate;
        private readonly SetHistoricBandwidthSettings _setHistoricBandwidthSettingsDelegate;

        #region detecting when mouse is over the growl
        // define a holder for whether the growl is watching for the mouse cursor to go outside the growl, defaulting to not watching (false)
        private readonly GenericHolder<bool> growlCapturedMouse = new GenericHolder<bool>(false);
        // define a bool for whether the growl is continually being kept opaque, such as when the mouse cursor is hovering over, defaulting to not keeping it opaque (false)
        private bool keepGrowlOpaque = false;
        #endregion

        // define a bool for whether this message receiver has been disposed, defaulting to false
        private bool isDisposed = false;

        #endregion
 
        /// <summary>
        /// Create a new EventMessageReceiver ViewModel.  Optionally use this ViewModel with your sync status controls.
        /// It exposes three ObservableCollections (ListFilesDownloading, ListFilesUploading and ListMessages).
        /// </summary>
        /// <param name="SyncboxId">The ID of the related CLSyncbox.</param>
        /// <param name="DeviceId">The ID of the related device.</param>
        /// <param name="receiver">(output) The created EventMessageReceiver, or null.</param>
        /// <param name="getHistoricBandwidthSettings">(optional) Your callback method to retrieve the historic bandwidth for upload and download from persistent storage.</param>
        /// <param name="setHistoricBandwidthSettings">(optional) Your callback method to persist the historic bandwidth for upload and download.</param>
        /// <param name="OverrideImportanceFilterNonErrors">(optional) A parameter to filter the number of non-error messages you will receive.</param>
        /// <param name="OverrideImportanceFilterErrors">(optional) A parameter to filter the number of error messages you will receive.</param>
        /// <param name="OverrideDefaultMaxStatusMessages">(optional) The maximum number of messages that will be maintained in ListMessages.</param>
        /// <returns>CLError: Any error that occurs, with exception information, or null.</returns>
        public static CLError CreateAndInitialize(
            long SyncboxId,
            string DeviceId,
            out EventMessageReceiver receiver,
            GetHistoricBandwidthSettings getHistoricBandwidthSettings = null,
            SetHistoricBandwidthSettings setHistoricBandwidthSettings = null,
            Nullable<EventMessageLevel> OverrideImportanceFilterNonErrors = null,
            Nullable<EventMessageLevel> OverrideImportanceFilterErrors = null,
            Nullable<int> OverrideDefaultMaxStatusMessages = null)
        {
            try
            {
                receiver = new EventMessageReceiver(SyncboxId,
                    DeviceId,
                    getHistoricBandwidthSettings,
                    setHistoricBandwidthSettings,
                    OverrideImportanceFilterNonErrors,
                    OverrideImportanceFilterErrors,
                    OverrideDefaultMaxStatusMessages);
            }
            catch (Exception ex)
            {
                receiver = Helpers.DefaultForType<EventMessageReceiver>();
                return ex;
            }
            return null;
        }
        private EventMessageReceiver(
            long SyncboxId,
            string DeviceId,
            GetHistoricBandwidthSettings getHistoricBandwidthSettings,
            SetHistoricBandwidthSettings setHistoricBandwidthSettings,
            Nullable<EventMessageLevel> OverrideImportanceFilterNonErrors,
            Nullable<EventMessageLevel> OverrideImportanceFilterErrors,
            Nullable<int> OverrideDefaultMaxStatusMessages)
        {
            CLError subscriptionError = MessageEvents.SubscribeMessageReceiver(SyncboxId,
                DeviceId,
                this);
            if (subscriptionError != null)
            {
                throw new AggregateException("Error subscribing this receiver to MessageEvents", subscriptionError.Exceptions);
            }

            // Save the parameters to private fields.
            _getHistoricBandwidthSettingsDelegate = getHistoricBandwidthSettings;
            _setHistoricBandwidthSettingsDelegate = setHistoricBandwidthSettings;

            this.SyncboxId = SyncboxId;
            this.DeviceId = DeviceId;

            // changes made upon construction for other partial class portions should be handled via a ConstructedHolder (see the next custom construction setter directly below)

            // run custom construction setter method for the partial class portion WindowSyncStatusViewModel
            WindowSyncStatusViewModelConstructed.MarkConstructed(WindowSyncStatusViewModelConstructionSetters,
                new KeyValuePair<KeyValuePair<EventMessageReceiver, Nullable<EventMessageLevel>>, KeyValuePair<Nullable<EventMessageLevel>, Nullable<int>>>(
                    new KeyValuePair<EventMessageReceiver, Nullable<EventMessageLevel>>(
                        this,
                        OverrideImportanceFilterNonErrors),
                    new KeyValuePair<Nullable<EventMessageLevel>, Nullable<int>>(
                        OverrideImportanceFilterErrors,
                        OverrideDefaultMaxStatusMessages)));
        }
 
        #region MessageEvents callbacks
        // informational or error message occurs, displayed only if high priority
        void IEventMessageReceiver.MessageEvents_NewEventMessage(IBasicMessage e)
        {
            // if the message represents an error, then check if the error is not minor in order to display
            if (e.IsError)
            {
                if (((int)e.Level) > ((int)EventMessageLevel.Minor))
                {
                    DisplayErrorGrowl(e.Message);
                }
            }
            // else if the message does not represent an error, then check if the error is more important than regular to display
            else if (((int)e.Level) > ((int)EventMessageLevel.Regular))
            {
                DisplayInformationalGrowl(e.Message);
            }

            // event was handled
            e.MarkHandled();
        }

        // displays an error using a new ErrorMessage
        private void DisplayErrorGrowl(string message)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke((Action<EventMessage>)AddEventMessageToGrowl, // the method which adds a growl message locks for modification
                    (EventMessage)(new ErrorMessage(message)));
            }
        }

        // displays an informational message using a new InformationalMessage
        private void DisplayInformationalGrowl(string message)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke((Action<EventMessage>)AddEventMessageToGrowl, // the method which adds a growl message locks for modification
                    (EventMessage)(new InformationalMessage(message)));
            }
        }

        // when the number of currently downloading files changes, create or update a message for the downloading count
        void IEventMessageReceiver.SetDownloadingCount(ISetCountMessage e)
        {
            // lock on the growl messages for modification
            lock (_growlMessages)
            {
                // declare a bool for whether a new growl message needed to be created
                bool newMessage;

                // if there is no current message for the downloading files, then create the new message with the current count and mark that a new message was created
                if (downloading == null)
                {
                    downloading = new DownloadingMessage(e.NewCount);
                    newMessage = true;
                }
                // else if there is already an existing message for the downloading files, then update the count and mark that a new message was not created
                else
                {
                    downloading.SetCount(e.NewCount);
                    newMessage = false;
                }

                // if a new message was created, then add the new message to the growl
                if (newMessage)
                {
                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke((Action<EventMessage>)AddEventMessageToGrowl, // the method which adds a growl message locks for modification
                            (EventMessage)downloading);
                    }
                }
            }

            // event was handled
            e.MarkHandled();
        }

        // when a file completes downloading, create or update a message for the incrementing completed downloads
        void IEventMessageReceiver.IncrementDownloadedCount(IIncrementCountMessage e)
        {
            // lock on the growl messages for modification
            lock (_growlMessages)
            {
                // declare a bool for whether a new growl message needed to be created
                bool newMessage;

                // if there is no current message for the completed downloads, then create the new message with the initial increment amount and mark that a new message was created
                if (downloaded == null)
                {
                    downloaded = new DownloadedMessage(e.IncrementAmount);
                    newMessage = true;
                }
                // else if there was not already an existing message for completed downloads, then increment the count and mark that a new message was not created
                else
                {
                    downloaded.IncrementCount(e.IncrementAmount);

                    newMessage = false;
                }

                // if a new message was created, then add the new message to the growl
                if (newMessage)
                {
                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke((Action<EventMessage>)AddEventMessageToGrowl, // the method which adds a growl message locks for modification
                            (EventMessage)downloaded);
                    }
                }
            }

            // event was handled
            e.MarkHandled();
        }

        // when the number of currently uploading files changes, create or update a message for the uploading count
        void IEventMessageReceiver.SetUploadingCount(ISetCountMessage e)
        {
            // lock on the growl messages for modification
            lock (_growlMessages)
            {
                // declare a bool for whether a new growl message needed to be created
                bool newMessage;

                // if there is no current message for the uploading files, then create the new message with the current count and mark that a new message was created
                if (uploading == null)
                {
                    uploading = new UploadingMessage(e.NewCount);
                    newMessage = true;
                }
                // else if there is already an existing message for the uploading files, then update the count and mark that a new message was not created
                else
                {
                    uploading.SetCount(e.NewCount);
                    newMessage = false;
                }

                // if a new message was created, then add the new message to the growl
                if (newMessage)
                {
                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke((Action<EventMessage>)AddEventMessageToGrowl, // the method which adds a growl message locks for modification
                            (EventMessage)uploading);
                    }
                }
            }

            // event was handled
            e.MarkHandled();
        }

        // when a file completes uploading, create or update a message for the incrementing uploaded downloads
        void IEventMessageReceiver.IncrementUploadedCount(IIncrementCountMessage e)
        {
            // lock on the growl messages for modification
            lock (_growlMessages)
            {
                // declare a bool for whether a new growl message needed to be created
                bool newMessage;

                // if there is no current message for the completed uploads, then create the new message with the initial increment amount and mark that a new message was created
                if (uploaded == null)
                {
                    uploaded = new UploadedMessage(e.IncrementAmount);
                    newMessage = true;
                }
                // else if there was not already an existing message for completed uploads, then increment the count and mark that a new message was not created
                else
                {
                    uploaded.IncrementCount(e.IncrementAmount);

                    newMessage = false;
                }

                // if a new message was created, then add the new message to the growl
                if (newMessage)
                {
                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke((Action<EventMessage>)AddEventMessageToGrowl, // the method which adds a growl message locks for modification
                            (EventMessage)uploaded);
                    }
                }
            }

            // event was handled
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
            lock (_locker)
            {
                try
                {
                    if (!isDisposed)
                    {
                        MessageEvents.UnsubscribeMessageReceiver(
                            SyncboxId,
                            DeviceId);
                    }
                }
                catch
                {
                }

                if (!isDisposed)
                {
                    // set delay completed so processing will not fire
                    isDisposed = true;

                    // Dispose local unmanaged resources last
                }
            }
        }

        // handler for the command for when a growl was clicked, looks for another ICommand as the parameter to fire again
        private void ClickedGrowl(object state)
        {
            // runs the handler for the command for when a growl was closed first
            ClosedGrowl(state);

            // try to cast the command parameter as a command to fire
            ICommand castState = state as ICommand;
            // if the command parameter was successfully cast as a command, then fire it 
            if (castState != null)
            {
                // try/catch to check for executing the command parameter command in order to execute it, silently failing
                try
                {
                    // if the command parameter command can be executed with this message receiver as a parameter, then execute it with this message receiver as a paramter
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

        // handler for the command for when a growl was closed, doesn't require any parameters
        private void ClosedGrowl(object state)
        {
            // lock on the growl messages for modification and lock on capturing the mouse (since closing the growl will move the mouse outside anyways)
            lock (_growlMessages)
            lock (growlCapturedMouse)
            {
                // set that the growl should not be visible
                GrowlVisible = false;
                // set the time that all the messages closed
                DateTime closeTime = DateTime.UtcNow;

                // loop through the growl messages
                foreach (EventMessage currentGrowl in _growlMessages)
                {
                    // mark all the times for fading in and out for the current growl message to the close time
                    currentGrowl.CompleteFadeOut = currentGrowl.StartFadeOut = currentGrowl.FadeInCompletion = closeTime;
                }

                // do not keep the growl opaque since it should close
                keepGrowlOpaque = false;
                // stop capturing the mouse if it was being captured since closing the growl will move the mouse outside
                growlCapturedMouse.Value = false;
            }
        }

        #region detecting when mouse is over the growl
        // handler for the command when the mouse entered the growl, need to keep growl opaque until the mouse leaves
        private class GrowlUserState
        {
            public FrameworkElement GrowlElement { get; set; }
            public EventMessageReceiver Receiver { get; set; }
        }
        private void MouseEnteredGrowl(FrameworkElement growlElement)
        {
            // declare a bool for whether to process that the mouse just entered the growl and it needs to stay opaque
            bool processEnter;
            // lock for capturing the mouse
            lock (growlCapturedMouse)
            {
                // if the growl is not staying opaque then a thread is not already watching for when the mouse leaves, so start that thread and mark that the mouse enter is processing
                if (!keepGrowlOpaque)
                {
                    // if the thread is not watching for mouse leave, then start that thread and mark that it started
                    if (!growlCapturedMouse.Value)
                    {
                        GrowlUserState userState = new GrowlUserState() { GrowlElement = growlElement, Receiver = this };
                        ThreadPool.UnsafeQueueUserWorkItem(ProcessGrowlCheckingMouse, userState);
                        growlCapturedMouse.Value = true;
                    }

                    // set that the growl is staying opaque and that the mouse entering is processing
                    processEnter = keepGrowlOpaque = true;
                }
                // else if the growl is already staying opaque then a thread is already watching for the when the mouse leaves, so only mark that the mouse entered does not need to be processed
                else
                {
                    processEnter = false;
                }
            }

            // if the mouse enter is processing, then check the growl messages to figure out if the growl needs to fade in first and if so how quickly
            if (processEnter)
            {
                // lock the growl messages to check them
                lock (_growlMessages)
                {
                    // define the current time for comparisons to growl times
                    DateTime currentTime = DateTime.UtcNow;
                    // define the earliest fade in time which would have been the time when the growl would have faded in
                    DateTime earliestFadeIn = DateTime.MaxValue;

                    // define a bool for whether a message was found which would have caused the growl to be opaque or fading out, defaulting to not found (false)
                    bool foundOpaqueOrFadeOut = false;

                    // loop through the growl messages
                    foreach (EventMessage currentGrowl in _growlMessages)
                    {
                        // if the current time is before the time the current message would have caused the growl to finish fading in, then check if its fade in completion time is earliest to store
                        if (currentTime.CompareTo(currentGrowl.FadeInCompletion) < 0)
                        {
                            // if the fade in completion time for the current message is earlier than the earliest recorded so far, then mark it as the earliest
                            if (earliestFadeIn.CompareTo(currentGrowl.FadeInCompletion) > 0)
                            {
                                earliestFadeIn = currentGrowl.FadeInCompletion;
                            }
                        }
                        // else if the current message would have caused the growl to have already finished fading in,
                        // then mark that the growl is already opaque or fading out and stop checking growl messages
                        else
                        {
                            foundOpaqueOrFadeOut = true;
                            break;
                        }
                    }

                    // if a message was found that would have caused the growl to already be opaque or to fade out, then fade back in using a special condition time
                    if (foundOpaqueOrFadeOut)
                    {
                        // set all times to 0 to trigger stopping existing animations
                        SecondsTillFadeIn = 0d;
                        SecondsTillStartFadeOut = 0d;
                        SecondsTillCompleteFadeOut = 0d;

                        // set the fade in time with a special condition time which will trigger the fade in
                        SecondsTillFadeIn = SecondsTillFadeInOnMouseOverIfFadedOut;
                    }
                    // else if no message was found that would have caused the growl to already be opaque or to fade out
                    // and if either no time was stored for the growl to finish fading in or if the fade in time would be different than the earliest recorded,
                    // then set or update the time to fade in by the earliest fade in time found
                    else if (firstFadeInCompletion == null
                        || ((DateTime)firstFadeInCompletion).CompareTo(earliestFadeIn) != 0)
                    {
                        // set all times to 0 to trigger stopping existing animations
                        SecondsTillFadeIn = 0d;
                        SecondsTillStartFadeOut = 0d;
                        SecondsTillCompleteFadeOut = 0d;

                        // set the fade in time with the earliest recorded time to fade in
                        SecondsTillFadeIn = earliestFadeIn.Subtract(currentTime).TotalSeconds;
                    }
                }
            }
        }

        // method to watch for the mouse leaving the provided growl FrameworkElement
        private static void ProcessGrowlCheckingMouse(object state)
        {
            // Cast the user state
            GrowlUserState castState = state as GrowlUserState;
            if (castState == null)
            {
                MessageBox.Show(System.Windows.Application.Current.MainWindow, "Unable to cast state as GrowlUserState in ProcessGrowlCheckingMouse");
            }
            // else if the userstate could be cast as the growl user state, then start a watching loop for when the mouse cursor leaves it
            else
            {
                // store the message receiver
                EventMessageReceiver thisReceiver = castState.Receiver;

                // loop indefinitely to check for the mouse leaving the growl FrameworkElement
                while (true)
                {
                    // lock on the message receiver to check disposal which will stop processing
                    lock (thisReceiver)
                    {
                        if (thisReceiver.isDisposed)
                        {
                            return;
                        }
                    }

                    // lock on whether the message receiver captured the mouse to check if it is no longer supposed to check which will stop processing
                    lock (thisReceiver.growlCapturedMouse)
                    {
                        if (!thisReceiver.growlCapturedMouse.Value)
                        {
                            return;
                        }
                    }

                    // define a holder for whether the mouse has left the growl FrameworkElement to stop processing
                    GenericHolder<bool> stopProcessing = new GenericHolder<bool>(false);

                    // lock on the stop processing holder to synchronize with a dispatcher invoke
                    lock (stopProcessing)
                    {
                        if (Application.Current == null)
                        {
                            return;
                        }

                        // dispatcher invoke on the UI thread to check if the mouse left the FrameworkElement
                        Application.Current.Dispatcher.BeginInvoke((Action<object>)(combinedState =>
                            {
                                // try to cast the dispatcher state as a nullable of the pair of the stop processing holder and the growl FrameworkElement
                                Nullable<KeyValuePair<GenericHolder<bool>, FrameworkElement>> castCombined = combinedState as Nullable<KeyValuePair<GenericHolder<bool>, FrameworkElement>>;
                                // if the dispatcher state was not successfully cast, then show a message with the error
                                if (castCombined == null)
                                {
                                    MessageBox.Show(System.Windows.Application.Current.MainWindow, "Unable to cast combinedState as KeyValuePair<GenericHolder<bool>, UIElement> in ProcessGrowlCheckingMouse");
                                }
                                // else if the dispatcher state was successfully cast, then check if the mouse left the growl FrameworkElement to stop processing
                                else
                                {
                                    // lock on capturing the mouse
                                    lock (thisReceiver.growlCapturedMouse)
                                    {
                                        // if the mouse left the growl FrameworkElement, then mark to stop processing and that the message receiver is not checking for the mouse and stop keeping the growl opaque
                                        if (thisReceiver.GrowlCheckIfMouseLeft(
                                            ((KeyValuePair<GenericHolder<bool>, FrameworkElement>)castCombined).Value)) // pass in the growl FrameworkElement
                                        {
                                            // mark to stop processing
                                            ((KeyValuePair<GenericHolder<bool>, FrameworkElement>)castCombined).Key.Value = true;
                                            // mark that the message receiver is not checking for the mouse
                                            thisReceiver.growlCapturedMouse.Value = false;
                                            // mark that the growl should not be kept opaque
                                            thisReceiver.keepGrowlOpaque = false;
                                        }
                                    }

                                    // lock on the stop processing holder to synchronize with the outer code which dispatched the current thread
                                    lock (((KeyValuePair<GenericHolder<bool>, FrameworkElement>)castCombined).Key)
                                    {
                                        // pulse back to the outer code which dispatched the current thread
                                        Monitor.Pulse(((KeyValuePair<GenericHolder<bool>, FrameworkElement>)castCombined).Key);
                                    }
                                }
                            }),
                            // pass in the stop processing holder and the growl FrameworkElement
                            new KeyValuePair<GenericHolder<bool>, FrameworkElement>(stopProcessing, castState.GrowlElement));

                        // wait for the dispatched thread to pulse back
                        Monitor.Wait(stopProcessing);
                    }

                    // if the stop processing holder was changed to indicate that the mouse left the growl FrameworkElement, then break out of the mouse-checking loop
                    if (stopProcessing.Value)
                    {
                        return;
                    }

                    // sleep before relooping to check the mouse location again using the specified delay
                    Thread.Sleep(GrowlProcessMouseCheckerMilliseconds);
                }
            }
        }

        // returns whether the mouse is outside the bounds of the provided growl FrameworkElement
        private bool GrowlCheckIfMouseLeft(FrameworkElement growlElement)
        {
            // use a helper call (which uses Win32) to accurately get the relative position of the mouse cursor relative to the growl FrameworkElement
            Point pointOnElement = Helpers.CorrectGetPosition(growlElement);

            // if the relative position of the mouse cursor is outside the bounds of the growl FrameworkElement rectangle, then process times on growl messages to fade out properly
            if (pointOnElement.X < 0 // mouse is to the left of the growl's left border
                || pointOnElement.Y < 0 // mouse is to the top of the growl's top border
                || pointOnElement.X >= growlElement.Width // mouse is to the right of the growl's right border
                || pointOnElement.Y >= growlElement.Height) // mouse is to the bottom of the growl's bottom border
            {
                bool needToStartTimer = false;

                // lock on growl messages for modification
                lock (_growlMessages)
                {
                    // if there are any growl messages to modify, then process them to fade out
                    if (_growlMessages.Count > 0)
                    {
                        // store the current time for the basis of all fading time modifications
                        DateTime currentTime = DateTime.UtcNow;
                        // define a time span for the longest time that any growl could stay opaque, defaulting to no time
                        TimeSpan latestStartFadeOut = TimeSpan.Zero;
                        // define a time span for the longest time that any growl could take to start and finish fading out, defaulting to no time
                        TimeSpan latestCompleteFadeOut = TimeSpan.Zero;

                        // loop though the growl messages
                        foreach (EventMessage currentGrowl in _growlMessages)
                        {
                            // declare a time span for how long it would take the current growl to finish fading in if it hasn't faded in already (which would 
                            TimeSpan timeLeftToFadeIn;
                            // if the current time is before when the current growl message would fade in, then use the difference as the time it would take for this growl message to fade in
                            if (currentTime.CompareTo(currentGrowl.FadeInCompletion) < 0)
                            {
                                timeLeftToFadeIn = currentGrowl.FadeInCompletion.Subtract(currentTime);
                            }
                            // else if the current growl message would have already faded in, then mark the fade in time as none
                            else
                            {
                                timeLeftToFadeIn = TimeSpan.Zero;
                            }

                            // define a time span for how long it would take the current growl to start fading out (including remaining time to fade in plus full time of being opaque)
                            TimeSpan currentStartFadeOut = currentGrowl.StartFadeOut.Subtract(currentGrowl.FadeInCompletion)
                                .Add(timeLeftToFadeIn);
                            // if the time span for how long to start fading out is the greatest so far then store it as the greatest
                            if (currentStartFadeOut.CompareTo(latestStartFadeOut) > 0)
                            {
                                latestStartFadeOut = currentStartFadeOut;
                            }

                            // define a time span for how long it would take the current growl to finish fading out (including remaining time to fade in plus full time of being opaque and full time to fade out from opaque)
                            TimeSpan currentCompleteFadeOut = currentGrowl.CompleteFadeOut.Subtract(currentGrowl.FadeInCompletion)
                                .Add(timeLeftToFadeIn);
                            // if the time span for how long to finish fading out is the greatest so far then store it as the greatest
                            if (currentCompleteFadeOut.CompareTo(latestCompleteFadeOut) > 0)
                            {
                                latestCompleteFadeOut = currentCompleteFadeOut;
                            }
                        }

                        // add the time to start fading out to the current time for the time when the growl will start fading out
                        DateTime newStartFadeOut = currentTime.Add(latestStartFadeOut);
                        // add the time to complete fading out to the current time for the time when the growl will finish fading out
                        DateTime newCompleteFadeOut = currentTime.Add(latestCompleteFadeOut);

                        // loop through the growl messages
                        foreach (EventMessage currentGrowl in _growlMessages)
                        {
                            // set the time when the current growl would finish fading in by the time it would starting fading out minus the time it would remain opaque
                            currentGrowl.FadeInCompletion = newStartFadeOut.Subtract(currentGrowl.StartFadeOut.Subtract(currentGrowl.FadeInCompletion));
                            // set the time when the current growl will start fading out from opaque
                            currentGrowl.StartFadeOut = newStartFadeOut;
                            // set the tiem when the current growl will finish fading out
                            currentGrowl.CompleteFadeOut = newCompleteFadeOut;
                        }

                        needToStartTimer = true;
                    }
                }

                if (needToStartTimer)
                {
                    // may need to start the processing thread which sets the bindable animation times for fading in and out
                    StartMessageTimerIfNeeded();
                }

                // return that the mouse left the growl FrameworkElement
                return true;
            }
            // return that the mouse did not leave the growl FrameworkElement
            return false;
        }
        #endregion

        // adds a new message to display in the message collection for the growl
        private void AddEventMessageToGrowl(EventMessage toAdd)
        {
            if (Application.Current == null)
            {
                return;
            }

            // requires UI thread so if this is the wrong thread then show the error
            if (Dispatcher.CurrentDispatcher != Application.Current.Dispatcher)
            {
                MessageBox.Show(System.Windows.Application.Current.MainWindow, "Cannot add growl message from any Dispatcher other than the Application's Dispatcher");
            }
            // else if this is the UI thread, then add the growl message
            else
            {
                // check for disposal and return if disposed
                lock (this)
                {
                    if (isDisposed)
                    {
                        return;
                    }
                }

                bool needToStartTimer = false;

                // lock on growl messages for modification
                lock (_growlMessages)
                {
                    // uses an extra lock method on top of ObservableCollection so it will collect all changes and fire a single reset when unlocked
                    _growlMessages.LockCollectionChanged();
                    // try/finally to add the message and finally unlock the collection
                    try
                    {
                        // add the current message
                        _growlMessages.Add(toAdd);

                        needToStartTimer = true;
                    }
                    finally
                    {
                        // uses an extra unlock method on top of ObservableCollection which matches the earlier lock method
                        _growlMessages.UnlockCollectionChanged();
                    }
                }

                if (needToStartTimer)
                {
                    // may need to start the processing thread which sets the bindable animation times for fading in and out
                    StartMessageTimerIfNeeded();
                }
            }
        }

        // starts the processing thread if it is not already started which sets the bindable animation times for fading in and out
        private void StartMessageTimerIfNeeded()
        {
            // lock for modifying the message processing thread
            lock (MessageTimerLocker)
            {
                // if the message processing thread is not already running, then run it
                if (!MessageTimerRunning)
                {
                    // mark that the message processing thread started
                    MessageTimerRunning = true;
                    // start the message processing thread
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessMessageTimer, this);
                }
            }
        }
        // define whether the message processing thread is running, defaulting to not running (false)
        private bool MessageTimerRunning = false;
        // define the lock for modifying the message processing thread
        private readonly object MessageTimerLocker = new object();

        // calculates the bindable animation times for fading in and out based on all growl messages
        private bool CalculateGrowlFadeTimes()
        {
            // lock on growl messages for checking
            lock (_growlMessages)
            {
                // store the current time as the base time for animation times
                DateTime toCompare = DateTime.UtcNow;
                // define the earliest time where a growl message should be finished fading in, defaulting to max
                DateTime earliestToFadeIn = DateTime.MaxValue;
                // define the latest time where a growl message should start fading out, defaulting to minimum
                DateTime latestToStartFadeOut = DateTime.MinValue;
                // define the latest time where a growl message should finish fading out, defaulting to minimum
                DateTime latestToFinishFadeOut = DateTime.MinValue;

                // define whether a growl message was found which has started fading out, defaulting to not found (false)
                bool foundFadeOut = false;
                // define whether a growl message was found that has completely faded in and is currently opaque, defaulting to not found (false)
                bool foundOpaque = false;
                // define whether a growl message was found that has started fading in, defaulting to not found (false)
                bool foundFadeIn = false;

                // create a list for the messages which will store messages which have elapsed in display time (but will only be removed if the whole growl should disappear)
                List<EventMessage> completedMessages = new List<EventMessage>();

                // loop through all growl messages
                foreach (EventMessage currentGrowl in _growlMessages)
                {
                    // if the current time is earlier than when the current message would finish fading in, then possibly mark that a fade in was found and update the earliest time to fade in and the latest time to start fading out
                    if (DateTime.Compare(toCompare,
                        currentGrowl.FadeInCompletion) < 0)
                    {
                        // if an opaque growl has not been found (which would make it pointless to look for fading in since the growl should be opaque),
                        // then mark that a fade in was found and possibly update the earliest time to fade in
                        if (!foundOpaque)
                        {
                            // a message fading in was found
                            foundFadeIn = true;

                            // if the current fading in message would fade in the earliest out of those checked so far, then store the new earliest
                            if (DateTime.Compare(earliestToFadeIn,
                                currentGrowl.FadeInCompletion) > 0)
                            {
                                earliestToFadeIn = currentGrowl.FadeInCompletion;
                            }
                        }

                        // if the current time to start fading out is latest out of those checked so far, then store the new latest
                        if (DateTime.Compare(latestToStartFadeOut,
                            currentGrowl.StartFadeOut) < 0)
                        {
                            latestToStartFadeOut = currentGrowl.StartFadeOut;
                        }
                    }
                    // else if the current message is not fading in and the current time is earlier than when the message would start fading out, then mark that an opaque message was found and possibly update the latest to start fading out
                    else if (DateTime.Compare(toCompare,
                        currentGrowl.StartFadeOut) < 0)
                    {
                        // an opaque message was found
                        foundOpaque = true;

                        // if the current opaque message would start fading out the latest out of those checked so far, then store the new latest
                        if (DateTime.Compare(latestToStartFadeOut,
                            currentGrowl.StartFadeOut) < 0)
                        {
                            latestToStartFadeOut = currentGrowl.StartFadeOut;
                        }
                    }
                    // else if the current message is neither fading in nor starting to fade out, and the current time is earlier than when the message would finish fading out, then possibly mark that a message is fading out and update the latest to finish fading out
                    else if (DateTime.Compare(toCompare,
                            currentGrowl.CompleteFadeOut) < 0)
                    {
                        // if no message has been found which was either fading in or remaining opaque, then mark that a fading out message was found and possibly update the latest time to fade out
                        if (!foundFadeIn
                            && !foundOpaque)
                        {
                            // a fading out message was found
                            foundFadeOut = true;

                            // if the current fading out message would finish fading out the latest out of those checked so far, then store the new latest
                            if (DateTime.Compare(latestToFinishFadeOut,
                                currentGrowl.CompleteFadeOut) < 0)
                            {
                                latestToFinishFadeOut = currentGrowl.CompleteFadeOut;
                            }
                        }
                    }
                    // else if the current message is neither fading in nor staying opaque nor fading out, then it should have completed faded out
                    // so confirm it should be removed and if so then add it to the completed list
                    else if (currentGrowl.ShouldRemove)
                    {
                        completedMessages.Add(currentGrowl);
                    }
                }

                // store the count of messages which will remain displayed in the growl after processing, defaulting to the count of existing growl messages
                int remainingMessageCount = _growlMessages.Count;

                // if any growl message was found that should be displayed at all (fading in, remaining opaque, or fading out), then mark that the growl should be visible and adjust the animation times for fading in or out
                if (foundFadeIn
                    || foundFadeOut
                    || foundOpaque)
                {
                    // growl should be visible
                    GrowlVisible = true;

                    // if a growl message was found which should be opaque,
                    // then clear out the last storage for fade in and fade out completion times and check for a different time to remain opaque if different than previously stored
                    if (foundOpaque)
                    {
                        // clear out the last storage for fade in time
                        firstFadeInCompletion = null;
                        // clear out the last storage for fade out completion time
                        lastFadeOutCompletion = null;

                        // if the time to remain opaque is different than the last time previously stored,
                        // then clear the fade in/fade out times to stop animations, set the time to remain opaque, and store the time to remain opaque for later comparisons
                        if (lastFadeOutStart == null
                            || ((DateTime)lastFadeOutStart).CompareTo(latestToStartFadeOut) != 0)
                        {
                            // set all times to 0 to trigger stopping existing animations
                            SecondsTillFadeIn = 0d;
                            SecondsTillStartFadeOut = 0d;
                            SecondsTillCompleteFadeOut = 0d;

                            // set the time to remain opaque to trigger making the growl completely opaque immediately
                            SecondsTillStartFadeOut = latestToStartFadeOut.Subtract(toCompare).TotalSeconds;

                            // store the time to remain opaque for later comparison
                            lastFadeOutStart = latestToStartFadeOut;
                        }
                    }
                    // else if a growl message was not found which should be opaque and if a growl message was found which should fade in,
                    // then clear out the last storage for remaining opaque and fade out completion times and check for a different time to fade in if different than previously stored
                    else if (foundFadeIn)
                    {
                        // clear out the last storage for remaining opaque time
                        lastFadeOutStart = null;
                        // clear out the last storage for fade out completion time
                        lastFadeOutCompletion = null;

                        // if the time to fade in is different than the last time previously stored,
                        // then clear the fade in/fade out times to stop animations, set the time to fade in, and store the time to fade in for later comparisons
                        if (firstFadeInCompletion == null
                            || ((DateTime)firstFadeInCompletion).CompareTo(earliestToFadeIn) != 0)
                        {
                            // set all times to 0 to trigger stopping existing animations
                            SecondsTillFadeIn = 0d;
                            SecondsTillStartFadeOut = 0d;
                            SecondsTillCompleteFadeOut = 0d;

                            // set the time to fade in to trigger animating the growl to become opaque over the specified time
                            SecondsTillFadeIn = earliestToFadeIn.Subtract(toCompare).TotalSeconds;

                            // store the time to fade in for later comparison
                            firstFadeInCompletion = earliestToFadeIn;
                        }
                    }
                    // else if a growl message was not found which should be opaque and if a growl message was not found which should fade in and if a growl message was found which should be fading out,
                    // then clear out the last storage for fade in and remaining opaque times and check for a different time to fade out if different than previously stored
                    else
                    {
                        // clear out the last storage for fade in time
                        firstFadeInCompletion = null;
                        // clear out the last storage for remaining opaque time
                        lastFadeOutStart = null;

                        // if the time to fade out is different than the last time previously stored,
                        // then clear the fade in/fade out times to stop animations, set the time to fade in, and store the time to fade out for later comparisons
                        if (lastFadeOutCompletion == null
                            || ((DateTime)lastFadeOutCompletion).CompareTo(latestToFinishFadeOut) != 0)
                        {
                            // set all times to 0 to trigger stopping existing animations
                            SecondsTillFadeIn = 0d;
                            SecondsTillStartFadeOut = 0d;
                            SecondsTillCompleteFadeOut = 0d;

                            // set the time to fade in to trigger animation the growl to fade out over the specified time
                            SecondsTillCompleteFadeOut = latestToFinishFadeOut.Subtract(toCompare).TotalSeconds;

                            // store the time to fade out for later comparison
                            lastFadeOutCompletion = latestToFinishFadeOut;
                        }
                    }
                }
                // else if the growl should not be displayed (no messages or no message is fading in, remaining opaque, or fading out),
                // then clear storage for last times to fade in/fade out, make the growl invisible, and decrement the remaining messages and trigger removing messages
                else
                {
                    // clear storage for last times to fade in/fade out
                    firstFadeInCompletion = null;
                    lastFadeOutStart = null;
                    lastFadeOutCompletion = null;

                    // set all times to 0 to trigger stopping existing animations
                    SecondsTillFadeIn = 0d;
                    SecondsTillStartFadeOut = 0d;
                    SecondsTillCompleteFadeOut = 0d;

                    // hide growl
                    GrowlVisible = false;

                    // decrement the remaining message count by the number of removed messages (should be all?)
                    remainingMessageCount -= completedMessages.Count;

                    // if any messages were to be removed, then dispatch them for removal
                    if (completedMessages.Count > 0)
                    {
                        if (Application.Current != null)
                        {
                            Application.Current.Dispatcher.BeginInvoke((Action<IEnumerable<EventMessage>>)RemoveEventMessagesFromGrowl, // the method which removes growl messages locks for modification
                                (IEnumerable<EventMessage>)completedMessages);
                        }
                    }
                }

                // return whether there are any messages left to display
                return remainingMessageCount != 0;
            }
        }

        // removes messages from display in the message collection for the growl
        private void RemoveEventMessagesFromGrowl(IEnumerable<EventMessage> toRemove)
        {
            if (Application.Current == null)
            {
                return;
            }

            // requires UI thread so if this is the wrong thread then show the error
            if (Dispatcher.CurrentDispatcher != Application.Current.Dispatcher)
            {
                MessageBox.Show(System.Windows.Application.Current.MainWindow, "Cannot remove growl messages from any Dispatcher other than the Application's Dispatcher");
            }
            // else if this is the UI thread, then remove the growl messages
            else
            {
                // check for disposal and return if disposed
                lock (this)
                {
                    if (isDisposed)
                    {
                        return;
                    }
                }

                // declare array for copying the input enumerable
                EventMessage[] toRemoveArray;
                // if the input enumerable exists and, upon copying it to an array, has at least one message, then remove the messages
                if (toRemove != null
                    && (toRemoveArray = toRemove.ToArray()).Length > 0)
                {
                    // lock on growl messages for modification
                    lock (_growlMessages)
                    {
                        // uses an extra lock method on top of ObservableCollection so it will collect all changes and fire a single reset when unlocked
                        _growlMessages.LockCollectionChanged();

                        // try/finally to remove the messages and finally unlock the collection
                        try
                        {
                            // loop through the indexes in the array of messages to remove
                            for (int removeIndex = 0; removeIndex < toRemoveArray.Length; removeIndex++)
                            {
                                // if the message at the current index is a downloading message, then clear its instance storage
                                if (toRemoveArray[removeIndex] is DownloadingMessage)
                                {
                                    downloading = null;
                                }
                                // else if the message at the current index is a downloaded message, then clear its instance storage
                                else if (toRemoveArray[removeIndex] is DownloadedMessage)
                                {
                                    downloaded = null;
                                }
                                // else if the message at the current index is an uploading message, then clear its instance storage
                                else if (toRemoveArray[removeIndex] is UploadingMessage)
                                {
                                    uploading = null;
                                }
                                // else if the message at the current index is an uploaded message, then clear its instance storage
                                else if (toRemoveArray[removeIndex] is UploadedMessage)
                                {
                                    uploaded = null;
                                }

                                // remove the message at the current index
                                _growlMessages.Remove(toRemoveArray[removeIndex]);
                            }
                        }
                        finally
                        {
                            // uses an extra unlock method on top of ObservableCollection which matches the earlier lock method
                            _growlMessages.UnlockCollectionChanged();
                        }
                    }
                }
            }
        }

        // loops indefinitely on a delay to process fade in/fade out times on growl messages
        private static void ProcessMessageTimer(object state)
        {
            // grab the message receiver
            EventMessageReceiver currentReceiver = state as EventMessageReceiver;
            // if the message receiver failed to retrieve, then show the error
            if (currentReceiver == null)
            {
                MessageBox.Show(System.Windows.Application.Current.MainWindow, "Unable to display growls: currentReceiver cannot be null");
            }
            // else if the message receiver succeeded in retrieval, then process fade in/fade out times on growl messages
            else
            {
                // define whether a message was found which should still continue to be displayed, default to true so that the logic processes at least once
                bool foundMessage = true;

                // loop until a message is not found which should still continue to be displayed
                while (foundMessage)
                {
                    // check if the receiver has been disposed to stop processing (return)
                    lock (currentReceiver)
                    {
                        if (currentReceiver.isDisposed)
                        {
                            return;
                        }
                    }

                    // declare whether the mouse is hovering over the growl since if so it will need to remain opaque regardless of messages
                    bool skipCalculate;
                    // lock on the mouse capturing to see if the mouse is hovering over the growl (meaning it is kept opaque) and set whether to skip calculation by the mouse hovering over
                    lock (currentReceiver.growlCapturedMouse)
                    {
                        skipCalculate = currentReceiver.keepGrowlOpaque;
                    }

                    // if the mouse is not hovering over the growl to keep it opaque, then run the method to calculate the fade in/fade out times and store whether any message was found which still needs to be displayed
                    if (!skipCalculate)
                    {
                        foundMessage = currentReceiver.CalculateGrowlFadeTimes();
                    }

                    // if a growl message was found which will still need to be displayed, then sleep on a specified delay before the processing loop repeats
                    if (foundMessage)
                    {
                        Thread.Sleep(MessageTimerDelayMilliseconds);
                    }
                }

                // lock for changing the message processing thread
                lock (currentReceiver.MessageTimerLocker)
                {
                    // declare a bool for whether the message processing should terminate
                    bool terminateProcessing;

                    // lock for checking the growl messages
                    lock (currentReceiver._growlMessages)
                    {
                        // set whether message processing should terminate by zero growl messages
                        terminateProcessing = currentReceiver._growlMessages.Count == 0;
                    }

                    // if message processing should terminate, then mark the thread no longer running (false)
                    if (terminateProcessing)
                    {
                        currentReceiver.MessageTimerRunning = false;
                    }
                    // else if message processing should not terminate, then start processing again
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