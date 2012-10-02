using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using wyUpdate.Common;

namespace wyDay.Controls
{
    /// <summary>
    /// Backend for the AutomaticUpdater control.
    /// </summary>
    public class AutomaticUpdaterBackend : IDisposable
    {
        AutoUpdaterInfo AutoUpdaterInfo;

        UpdateHelper updateHelper = new UpdateHelper();

        UpdateStepOn m_UpdateStepOn;

        UpdateType internalUpdateType = UpdateType.Automatic;
        UpdateType m_UpdateType = UpdateType.Automatic;


        // changes
        string version, changes;
        bool changesAreRTF;

        #region Events

        /// <summary>
        /// Event is raised before the update checking begins.
        /// </summary>
        public event BeforeHandler BeforeChecking;

        /// <summary>
        /// Event is raised before the downloading of the update begins.
        /// </summary>
        public event BeforeHandler BeforeDownloading;

        /// <summary>
        /// Event is raised before the extracting of the update begins.
        /// </summary>
        public event BeforeHandler BeforeExtracting;

        /// <summary>
        /// Event is raised when checking or updating is cancelled.
        /// </summary>
        public event EventHandler Cancelled;

        /// <summary>
        /// Event is raised when the checking for updates fails.
        /// </summary>
        public event FailHandler CheckingFailed;

        /// <summary>
        /// Event is raised after you or your user invoked InstallNow(). You should close your app as quickly as possible (because wyUpdate will be waiting).
        /// </summary>
        public event EventHandler CloseAppNow;

        /// <summary>
        /// Event is raised when an update can't be installed and the closing is aborted.
        /// </summary>
        public event EventHandler ClosingAborted;

        /// <summary>
        /// Event is raised when the update fails to download.
        /// </summary>
        public event FailHandler DownloadingFailed;

        /// <summary>
        /// Event is raised when the update fails to extract.
        /// </summary>
        public event FailHandler ExtractingFailed;

        /// <summary>
        /// Event is raised when the current update step progress changes.
        /// </summary>
        public event UpdateProgressChanged ProgressChanged;

        /// <summary>
        /// Event is raised when the update is ready to be installed.
        /// </summary>
        public event EventHandler ReadyToBeInstalled;

        /// <summary>
        /// Event is raised when a new update is found.
        /// </summary>
        public event EventHandler UpdateAvailable;

        /// <summary>
        /// Event is raised when an update fails to install.
        /// </summary>
        public event FailHandler UpdateFailed;

        /// <summary>
        /// Event is raised when an update installs successfully.
        /// </summary>
        public event SuccessHandler UpdateSuccessful;

        /// <summary>
        /// Event is raised when the latest version is already installed.
        /// </summary>
        public event SuccessHandler UpToDate;

        /// <summary>
        /// Event is raised when the update step that is supposed to be processed isn't the step that's processed.
        /// </summary>
        public event EventHandler UpdateStepMismatch;

        #endregion Events

        #region Properties

        /// <summary>
        /// Gets or sets the arguments to pass to your app when it's being restarted after an update.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Gets the changes for the new update.
        /// </summary>
        public string Changes
        {
            get
            {
                if (!changesAreRTF)
                    return changes;

                // convert the RTF text to plaintext
                using (RichTextBox r = new RichTextBox {Rtf = changes})
                {
                    return r.Text;
                }
            }
        }

        /// <summary>
        /// Gets the changes for the new update. It may be in RTF or TXT format. Use AreChangesRTF.
        /// </summary>
        public string RawChanges { get { return changes; } }

        /// <summary>
        /// Is the RawChanges RTF or text.
        /// </summary>
        public bool AreChangesRTF { get { return changesAreRTF; } }

        /// <summary>
        /// Gets if this AutomaticUpdater has hidden this form and preparing to install an update.
        /// </summary>
        public bool ClosingForInstall { get; private set; }

        string m_GUID;

        /// <summary>
        /// Gets the GUID (Globally Unique ID) of the automatic updater. It is recommended you set this value (especially if there is more than one exe for your product).
        /// </summary>
        /// <exception cref="System.Exception">Thrown when trying to set the GUID at runtime.</exception>
        public string GUID
        {
            get { return m_GUID; }
            set
            {
                // disallow setting after AutoUpdaterInfo is not null
                if (AutoUpdaterInfo != null)
                    throw new Exception("You must set the GUID at Design time (or before you call the Initialize() function).");

                // disallow bad filename characters
                if (value != null && value.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                    throw new Exception("The GUID cannot contain invalid filename characters.");

                m_GUID = value;
            }
        }

        /// <summary>
        /// Gets the date the updates were last checked for.
        /// </summary>
        public DateTime LastCheckDate
        {
            get { return AutoUpdaterInfo.LastCheckedForUpdate; }
        }


        /// <summary>
        /// Gets the update step the AutomaticUpdater is currently on.
        /// </summary>
        public UpdateStepOn UpdateStepOn
        {
            get
            {
                return m_UpdateStepOn;
            }
            private set
            {
                m_UpdateStepOn = value;
                Trace.WriteLine(String.Format("AutomaticUpdater: BEUpdateStepOn: Entry. Set UpdateStepOn: {0}.", value));

                // set the AutoUpdaterInfo property
                if (value != UpdateStepOn.Checking
                    && value != UpdateStepOn.DownloadingUpdate
                    && value != UpdateStepOn.ExtractingUpdate)
                {
                    if (value == UpdateStepOn.Nothing)
                        AutoUpdaterInfo.ClearSuccessError();

                    AutoUpdaterInfo.UpdateStepOn = value;
                    AutoUpdaterInfo.Save();
                }
            }
        }

        /// <summary>
        /// Gets or sets how much this AutomaticUpdater control should do without user interaction.
        /// </summary>
        public UpdateType UpdateType
        {
            get { return m_UpdateType; }
            set
            {
                m_UpdateType = value;
                internalUpdateType = value;
            }
        }

        /// <summary>
        /// Gets the version of the new update.
        /// </summary>
        public string Version
        {
            get
            {
                return version;
            }
        }

        /// <summary>
        /// Gets or sets the arguments to pass to wyUpdate when it is started to check for updates.
        /// </summary>
        public string CloudUpdaterCommandline
        {
            get { return updateHelper.ExtraArguments; }
            set { updateHelper.ExtraArguments = value; }
        }

        /// <summary>
        /// Gets or sets the relative path to the wyUpdate (e.g. wyUpdate.exe  or  SubDir\\wyUpdate.exe)
        /// </summary>
        public string CloudUpdaterLocation
        {
            get { return updateHelper.CloudUpdaterLocation; }
            set { updateHelper.CloudUpdaterLocation = value; }
        }

        /// <summary>
        /// Gets or sets the service to start after the update.
        /// </summary>
        public string ServiceName { get; set; }

        #endregion

        #region Dispose

        /// <summary>
        /// Indicates whether this instance is disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Finalizes an instance of the <see cref="AutomaticUpdaterBackend"/> class.
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="AutomaticUpdaterBackend"/> is reclaimed by garbage collection.
        /// </summary>
        ~AutomaticUpdaterBackend()
        {
            Trace.WriteLine("AutomaticUpdater: BE~AutomaticUpdaterBackend: Entry.");
            Dispose(false);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">Result: <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                // Not already disposed ?
                if (disposing)
                {
                    // dispose managed resources
                    // Not already disposed ?
                    if (updateHelper != null)
                    {
                        updateHelper.Dispose(); // Dispose it
                        updateHelper = null; // Its now inaccessible
                    }
                }

                // free unmanaged resources
                // Set large fields to null.

                // Instance is disposed
                isDisposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Represents an AutomaticUpdater control.
        /// </summary>
        public AutomaticUpdaterBackend()
        {
            updateHelper.ProgressChanged += updateHelper_ProgressChanged;
            updateHelper.PipeServerDisconnected += updateHelper_PipeServerDisconnected;
            updateHelper.UpdateStepMismatch += updateHelper_UpdateStepMismatch;
        }

        internal AutomaticUpdaterBackend(bool bufferResponse) : this()
        {
            updateHelper.BufferResponse = bufferResponse;
        }

        internal void FlushResponses()
        {
            updateHelper.FlushResponses();
        }

        /// <summary>
        /// Proceed with the download and installation of pending updates.
        /// </summary>
        public void InstallNow()
        {
            Trace.WriteLine("AutomaticUpdater: BEInstallNow: Entry.");
            if (AutoUpdaterInfo == null)
            {
                Trace.WriteLine("AutomaticUpdater: BEInstallNow: ERROR.  AutoUpdater null.");
                throw new FailedToInitializeException();
            }

            // throw an exception when trying to Install when no update is ready

            if (UpdateStepOn == UpdateStepOn.Nothing)
                throw new Exception("There must be an update available before you can install it.");
            
            if (UpdateStepOn == UpdateStepOn.Checking)
                throw new Exception("The AutomaticUpdater must finish checking for updates before they can be installed.");

            if (UpdateStepOn == UpdateStepOn.DownloadingUpdate)
                throw new Exception("The update must be downloaded before you can install it.");

            if (UpdateStepOn == UpdateStepOn.ExtractingUpdate)
                throw new Exception("The update must finish extracting before you can install it.");

            // set the internal update type to automatic so the user won't be prompted anymore
            internalUpdateType = UpdateType.Automatic;

            if (UpdateStepOn == UpdateStepOn.UpdateAvailable)
            {
                // begin downloading the update
                Trace.WriteLine("AutomaticUpdater: BEInstallNow: Download the update.");
                DownloadUpdate();
            }
            else if (UpdateStepOn == UpdateStepOn.UpdateDownloaded)
            {
                Trace.WriteLine("AutomaticUpdater: BEInstallNow: Extract the update.");
                ExtractUpdate();
            }
            else // UpdateReadyToInstall
            {
                // begin installing the update
                Trace.WriteLine("AutomaticUpdater: BEInstallNow: InstallPendingUpdate.");
                InstallPendingUpdate();
            }
        }

        /// <summary>
        /// Cancel the checking, downloading, or extracting currently in progress.
        /// </summary>
        public void Cancel()
        {
            Trace.WriteLine("AutomaticUpdater: BECancel: entry.");
            if (AutoUpdaterInfo == null)
                throw new FailedToInitializeException();

            updateHelper.Cancel();

            SetLastSuccessfulStep();

            if (Cancelled != null)
                Cancelled(this, EventArgs.Empty);
        }

        void SetLastSuccessfulStep()
        {
            if (UpdateStepOn == UpdateStepOn.Checking)
                UpdateStepOn = UpdateStepOn.Nothing;
            else
                UpdateStepOn = UpdateStepOn.UpdateAvailable;
        }

        /// <summary>
        /// Check for updates forcefully -- returns true if the updating has begun. Use the "CheckingFailed", "UpdateAvailable", or "UpToDate" events for the result.
        /// </summary>
        /// <param name="recheck">Recheck with the servers regardless of cached updates, etc.</param>
        /// <returns>Returns true if checking has begun, false otherwise.</returns>
        public bool ForceCheckForUpdate(bool recheck)
        {
            Trace.WriteLine(String.Format("AutomaticUpdater: BEForceCheckForUpdate: entry. recheck: {0}.", recheck));
            if (AutoUpdaterInfo == null)
                throw new FailedToInitializeException();

            // if not already checking for updates then begin checking.
            if (UpdateStepOn == UpdateStepOn.Nothing || (recheck && UpdateStepOn == UpdateStepOn.UpdateAvailable))
            {
                Trace.WriteLine("AutomaticUpdater: BEForceCheckForUpdate: Begin checking.");
                BeforeArgs bArgs = new BeforeArgs();

                if (BeforeChecking != null)
                    BeforeChecking(this, bArgs);

                if (bArgs.Cancel)
                {
                    // close wyUpdate
                    updateHelper.Cancel();
                    return false;
                }

                // show the working animation
                UpdateStepOn = UpdateStepOn.Checking;

                if (recheck)
                {
                    Trace.WriteLine("AutomaticUpdater: BEForceCheckForUpdate: Call updateHelper.ForceRecheckForUpdate.");
                    updateHelper.ForceRecheckForUpdate();
                }
                else
                {
                    Trace.WriteLine("AutomaticUpdater: BEForceCheckForUpdate: Call updateHelper.CheckForUpdate.");
                    updateHelper.CheckForUpdate();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Check for updates forcefully -- returns true if the updating has begun. Use the "CheckingFailed", "UpdateAvailable", or "UpToDate" events for the result.
        /// </summary>
        /// <returns>Returns true if checking has begun, false otherwise.</returns>
        public bool ForceCheckForUpdate()
        {
            Trace.WriteLine("AutomaticUpdater: BEForceCheckForUpdate(2): Begin checking.");
            return ForceCheckForUpdate(false);
        }

        void InstallPendingUpdate()
        {
            // send the client the arguments that need to run on success and failure
            Trace.WriteLine("AutomaticUpdater: BEInstallPendingUpdate: Entry.");
            if (ServiceName != null)
                updateHelper.RestartInfo(ServiceName, AutoUpdaterInfo.AutoUpdateID, Arguments, true);
            else
                updateHelper.RestartInfo(Application.ExecutablePath, AutoUpdaterInfo.AutoUpdateID, Arguments, false);
        }

        void DownloadUpdate()
        {
            Trace.WriteLine("AutomaticUpdater: BEDownloadUpdate: Entry.");
            BeforeArgs bArgs = new BeforeArgs();

            if (BeforeDownloading != null)
                BeforeDownloading(this, bArgs);

            if (bArgs.Cancel)
            {
                // close wyUpdate
                updateHelper.Cancel();
                return;
            }

            // if the control is hidden show it now (so the user can cancel the downloading if they want)
            // show the 'working' animation
            UpdateStepOn = UpdateStepOn.DownloadingUpdate;

            Trace.WriteLine("AutomaticUpdater: BEDownloadUpdate: Begin the download.");
            updateHelper.DownloadUpdate();
        }

        void ExtractUpdate()
        {
            Trace.WriteLine("AutomaticUpdater: BEExtractUpdate: Entry.");
            BeforeArgs bArgs = new BeforeArgs();

            if (BeforeExtracting != null)
                BeforeExtracting(this, bArgs);

            if (bArgs.Cancel)
            {
                // close wyUpdate
                updateHelper.Cancel();
                return;
            }

            UpdateStepOn = UpdateStepOn.ExtractingUpdate;

            // extract the update
            Trace.WriteLine("AutomaticUpdater: BEExtractUpdate: Begin the extraction.");
            updateHelper.BeginExtraction();
        }

        void updateHelper_UpdateStepMismatch(object sender, Response respType, UpdateStep previousStep)
        {
            // we can't install right now
            Trace.WriteLine("AutomaticUpdater: BEupdateHelper_UpdateStepMismatch: Entry.");
            if (previousStep == UpdateStep.RestartInfo)
            {
                if (ClosingAborted != null)
                    ClosingAborted(this, EventArgs.Empty);
            }

            if (respType == Response.Progress)
            {
                switch (updateHelper.UpdateStep)
                {
                    case UpdateStep.CheckForUpdate:
                        Trace.WriteLine("AutomaticUpdater: BEupdateHelper_UpdateStepMismatch: Set UpdateStepOn.Checking.");
                        UpdateStepOn = UpdateStepOn.Checking;
                        break;
                    case UpdateStep.DownloadUpdate:
                        Trace.WriteLine("AutomaticUpdater: BEupdateHelper_UpdateStepMismatch: Set UpdateStepOn.DownloadingUpdate.");
                        UpdateStepOn = UpdateStepOn.DownloadingUpdate;
                        break;
                    case UpdateStep.BeginExtraction:
                        Trace.WriteLine("AutomaticUpdater: BEupdateHelper_UpdateStepMismatch: Set UpdateStepOn.BeginExtraction.");
                        UpdateStepOn = UpdateStepOn.ExtractingUpdate;
                        break;
                }
            }

            if (UpdateStepMismatch != null)
                UpdateStepMismatch(this, EventArgs.Empty);
        }

        void updateHelper_PipeServerDisconnected(object sender, UpdateHelperData e)
        {
            // wyUpdate should only ever exit after success or failure
            // otherwise it is a premature exit (and needs to be treated as an error)
            Trace.WriteLine("AutomaticUpdater: BEupdateHelper_PipeServerDisconnected: Entry.");
            if (UpdateStepOn == UpdateStepOn.Checking
                || UpdateStepOn == UpdateStepOn.DownloadingUpdate
                || UpdateStepOn == UpdateStepOn.ExtractingUpdate
                || e.UpdateStep == UpdateStep.RestartInfo)
            {
                if (e.UpdateStep == UpdateStep.RestartInfo && ClosingAborted != null)
                    ClosingAborted(this, EventArgs.Empty);

                // wyUpdate premature exit error
                UpdateStepFailed(UpdateStepOn, new FailArgs { CloudUpdaterPrematureExit = true, ErrorTitle = e.ExtraData[0], ErrorMessage = e.ExtraData[1] });
            }
        }

        void updateHelper_ProgressChanged(object sender, UpdateHelperData e)
        {
            Trace.WriteLine(String.Format("AutomaticUpdater: updateHelper_ProgressChanged: Entry. Response: {0}.", e.ResponseType.ToString()));
            switch (e.ResponseType)
            {
                case Response.Failed:

                    // show the error icon & menu
                    // and set last successful step
                    UpdateStepFailed(UpdateStepToUpdateStepOn(e.UpdateStep), new FailArgs { ErrorTitle = e.ExtraData[0], ErrorMessage = e.ExtraData[1] });

                    break;
                case Response.Succeeded:

                    switch (e.UpdateStep)
                    {
                        case UpdateStep.CheckForUpdate:

                            AutoUpdaterInfo.LastCheckedForUpdate = DateTime.Now;

                            // there's an update available
                            if (e.ExtraData.Count != 0)
                            {
                                Trace.WriteLine("AutomaticUpdater: updateHelper_ProgressChanged: There is an update available.");
                                version = e.ExtraData[0];

                                // if there are changes, save them
                                if (e.ExtraData.Count > 1)
                                {
                                    Trace.WriteLine("AutomaticUpdater: updateHelper_ProgressChanged: Save the changes.");
                                    changes = e.ExtraData[1];
                                    changesAreRTF = e.ExtraDataIsRTF[1];
                                }

                                // save the changes to the AutoUpdateInfo file
                                AutoUpdaterInfo.UpdateVersion = version;
                                AutoUpdaterInfo.ChangesInLatestVersion = changes;
                                AutoUpdaterInfo.ChangesIsRTF = changesAreRTF;
                            }
                            else
                            {
                                // Clear saved version details for cases where we're
                                // continuing an update (the version details filled
                                // in from the AutoUpdaterInfo file) however,
                                // wyUpdate reports your app has since been updated.
                                // Thus we need to clear the saved info.
                                Trace.WriteLine("AutomaticUpdater: updateHelper_ProgressChanged: Clear version details.");
                                version = null;
                                changes = null;
                                changesAreRTF = false;

                                AutoUpdaterInfo.ClearSuccessError();
                            }

                            break;
                        case UpdateStep.DownloadUpdate:

                            Trace.WriteLine("AutomaticUpdater: updateHelper_ProgressChanged: Set UpdateStepOn.UpdateDownloaded.");
                            UpdateStepOn = UpdateStepOn.UpdateDownloaded;

                            break;
                        case UpdateStep.RestartInfo:

                            // show client & send the "begin update" message
                            Trace.WriteLine("AutomaticUpdater: updateHelper_ProgressChanged: Show the begin update message.");
                            updateHelper.InstallNow();

                            // close this application so it can be updated
                            // use either the custom handler or Environment.Exit();
                            if (CloseAppNow != null)
                                CloseAppNow(this, EventArgs.Empty);
                            else
                                Environment.Exit(0);

                            return;
                    }

                    StartNextStep(e.UpdateStep);

                    break;
                case Response.Progress:

                    // call the progress changed event
                    if (ProgressChanged != null)
                        ProgressChanged(this, e.Progress);

                    break;
            }
        }

        void StartNextStep(UpdateStep updateStepOn)
        {
            // begin the next step
            Trace.WriteLine(String.Format("AutomaticUpdater: BEStartNextStep: Entry.  UpdateStepOn: {0}.", updateStepOn.ToString()));
            switch (updateStepOn)
            {
                case UpdateStep.CheckForUpdate:

                    if (!string.IsNullOrEmpty(version))
                    {
                        // there's an update available

                        if (internalUpdateType == UpdateType.CheckAndDownload
                            || internalUpdateType == UpdateType.Automatic)
                        {
                            UpdateStepOn = UpdateStepOn.UpdateAvailable;

                            // begin downloading the update
                            Trace.WriteLine("AutomaticUpdater: BEStartNextStep: Begin downloading.");
                            DownloadUpdate();
                        }
                        else
                        {
                            // show the update ready mark
                            Trace.WriteLine("AutomaticUpdater: BEStartNextStep: Show update ready.");
                            UpdateReady();
                        }
                    }
                    else //no update
                    {
                        // tell the user they're using the latest version
                        Trace.WriteLine("AutomaticUpdater: BEStartNextStep: Show already on latest version.");
                        AlreadyUpToDate();
                    }

                    break;
                case UpdateStep.DownloadUpdate:

                    // begin extraction
                    Trace.WriteLine("AutomaticUpdater: BEStartNextStep: Begin extraction.");
                    if (internalUpdateType == UpdateType.Automatic)
                        ExtractUpdate();
                    else
                        UpdateReadyToExtract();

                    break;
                case UpdateStep.BeginExtraction:

                    // inform the user that the update is ready to be installed
                    Trace.WriteLine("AutomaticUpdater: BEStartNextStep: Show ready to install.");
                    UpdateReadyToInstall();

                    break;
            }
        }

        void UpdateReady()
        {
            UpdateStepOn = UpdateStepOn.UpdateAvailable;

            if (UpdateAvailable != null)
                UpdateAvailable(this, EventArgs.Empty);
        }

        void UpdateReadyToExtract()
        {
            UpdateStepOn = UpdateStepOn.UpdateDownloaded;

            if (ReadyToBeInstalled != null)
                ReadyToBeInstalled(this, EventArgs.Empty);
        }

        void UpdateReadyToInstall()
        {
            UpdateStepOn = UpdateStepOn.UpdateReadyToInstall;

            if (ReadyToBeInstalled != null)
                ReadyToBeInstalled(this, EventArgs.Empty);
        }

        void AlreadyUpToDate()
        {
            UpdateStepOn = UpdateStepOn.Nothing;

            if (UpToDate != null)
                UpToDate(this, new SuccessArgs { Version = version });
        }

        void UpdateStepFailed(UpdateStepOn us, FailArgs args)
        {
            SetLastSuccessfulStep();

            switch (us)
            {
                case UpdateStepOn.Checking:

                    if (CheckingFailed != null)
                        CheckingFailed(this, args);

                    break;
                case UpdateStepOn.DownloadingUpdate:

                    if (DownloadingFailed != null)
                        DownloadingFailed(this, args);

                    break;
                case UpdateStepOn.ExtractingUpdate:

                    if (ExtractingFailed != null)
                        ExtractingFailed(this, args);

                    break;
                default:

                    if (UpdateFailed != null)
                        UpdateFailed(this, args);
                    break;
            }
        }

        static UpdateStepOn UpdateStepToUpdateStepOn(UpdateStep us)
        {
            switch(us)
            {
                case UpdateStep.BeginExtraction:
                    return UpdateStepOn.ExtractingUpdate;
                case UpdateStep.CheckForUpdate:
                    return UpdateStepOn.Checking;
                case UpdateStep.DownloadUpdate:
                    return UpdateStepOn.DownloadingUpdate;
                default:
                    throw new Exception("UpdateStep not supported");
            }
        }


        /// <summary>
        /// The intialize function must be called before you can use any other functions.
        /// </summary>
        public void Initialize()
        {
            // read settings file for last check time
            Trace.WriteLine("AutomaticUpdater: BEInitialize: Entry.");
            AutoUpdaterInfo = new AutoUpdaterInfo(m_GUID, null);

            // see if update is pending, if so force install
            if (AutoUpdaterInfo.UpdateStepOn == UpdateStepOn.UpdateReadyToInstall)
            {
                //TODO: test funky non-compliant state file

                // then KillSelf&StartUpdater
                Trace.WriteLine("AutomaticUpdater: BEInitialize: KillSelf&StartUpdater.");
                ClosingForInstall = true;

                // start the updater
                InstallPendingUpdate();
            }
        }

        /// <summary>
        /// The function that must be called when your app has loaded.
        /// </summary>
        public void AppLoaded()
        {
            Trace.WriteLine("AutomaticUpdater: BEAppLoaded: Entry.");
            if (AutoUpdaterInfo == null)
                throw new FailedToInitializeException();

            // if we want to kill ourself, then don't bother checking for updates
            if (ClosingForInstall)
                return;

            // get the current update step from the info file
            m_UpdateStepOn = AutoUpdaterInfo.UpdateStepOn;

            if (UpdateStepOn != UpdateStepOn.Nothing)
            {
                version = AutoUpdaterInfo.UpdateVersion;
                changes = AutoUpdaterInfo.ChangesInLatestVersion;
                changesAreRTF = AutoUpdaterInfo.ChangesIsRTF;

                Trace.WriteLine(String.Format("AutomaticUpdater: BEAppLoaded: UpdateStepOn: {0}.", UpdateStepOn.ToString()));
                switch (UpdateStepOn)
                {
                    case UpdateStepOn.UpdateAvailable:
                        Trace.WriteLine("AutomaticUpdater: BEAppLoaded: UpdateAvailable.");               
                        if (internalUpdateType == UpdateType.CheckAndDownload || internalUpdateType == UpdateType.Automatic)
                            DownloadUpdate(); // begin downloading the update
                        else
                            UpdateReady();

                        break;

                    case UpdateStepOn.UpdateReadyToInstall:
                        Trace.WriteLine("AutomaticUpdater: BEAppLoaded: UpdateReadyToInstall.");               
                        UpdateReadyToInstall();
                        break;

                    case UpdateStepOn.UpdateDownloaded:
                        Trace.WriteLine("AutomaticUpdater: BEAppLoaded: UpdateDownloaded.");               

                        if (internalUpdateType == UpdateType.Automatic)
                            ExtractUpdate(); // begin extraction
                        else
                            UpdateReadyToExtract();

                        break;
                }
            }
            else // UpdateStepOn == UpdateStepOn.Nothing
            {
                Trace.WriteLine("AutomaticUpdater: BEAppLoaded: UpdateStepOn: Nothing.");
                switch (AutoUpdaterInfo.AutoUpdaterStatus)
                {
                    case AutoUpdaterStatus.UpdateSucceeded:

                        // set the version & changes
                        Trace.WriteLine("AutomaticUpdater: BEAppLoaded: UpdateSucceeded.");
                        version = AutoUpdaterInfo.UpdateVersion;
                        changes = AutoUpdaterInfo.ChangesInLatestVersion;
                        changesAreRTF = AutoUpdaterInfo.ChangesIsRTF;

                        if (UpdateSuccessful != null)
                            UpdateSuccessful(this, new SuccessArgs { Version = version });

                        break;
                    case AutoUpdaterStatus.UpdateFailed:
                        Trace.WriteLine("AutomaticUpdater: BEAppLoaded: UpdateFailed.");

                        if (UpdateFailed != null)
                            UpdateFailed(this, new FailArgs { ErrorTitle = AutoUpdaterInfo.ErrorTitle, ErrorMessage = AutoUpdaterInfo.ErrorMessage });

                        break;
                }

                // clear the changes and resave
                AutoUpdaterInfo.ClearSuccessError();
                AutoUpdaterInfo.Save();
            }
        }
    }

    /// <summary>
    /// The fail to Initialize exception.
    /// </summary>
    public class FailedToInitializeException : Exception
    {
        /// <summary>
        /// The fail to Initialize exception.
        /// </summary>
        public FailedToInitializeException()
            : base("You must call the Initialize() function before you can use any other functions.")
        {
        }
    }
}