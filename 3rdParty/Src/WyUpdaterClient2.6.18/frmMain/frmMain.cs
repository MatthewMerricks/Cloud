using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using wyDay.Controls;
using wyUpdate.Common;
using wyUpdate.Downloader;

namespace wyUpdate
{
    public partial class frmMain : Form
    {
        #region Private variables

        public bool IsAdmin;

        public readonly ClientFile update = new ClientFile();
        ServerFile ServerFile;
        VersionChoice updateFrom;

        UpdateDetails updtDetails;

        FileDownloader downloader;
        InstallUpdate installUpdate;

        readonly ClientLanguage clientLang = new ClientLanguage();

        Frame frameOn = Frame.Checking;
        bool isCancelled;

        string error;
        string errorDetails;

        // full filename of the update & servers files
        string updateFilename;
        string serverFileLoc;

        //client file location
        string clientFileLoc;

        // are we using the -server commandline switch?
        string serverOverwrite;

        // custom update path (-updatepath commandline switch)
        string updatePathVar;

        // custom url args (-urlargs commandline switch)
        string customUrlArgs;

        // base directory: same path as the executable, unless specified
        string baseDirectory;
        //the extract directory
        string tempDirectory;

        readonly PanelDisplay panelDisplaying = new PanelDisplay(500, 320);

        // the first step wyUpdate should take
        UpdateStepOn startStep = UpdateStepOn.Nothing;

        // for self update
        public SelfUpdateState SelfUpdateState;

        /// <summary>Does the client need elevation?</summary>
        bool needElevation;

        //--Uninstalling
        bool uninstalling;

        //--Silent updating/uninstalling
        bool isSilent;
        public int ReturnCode;

        //Pre-RC2 compatability:
        ClientFileType clientFileType;

        // handle hidden form
        bool _isApplicationRun = true;

        // start hidden, close if no update, show if update
        bool QuickCheck;
        bool QuickCheckNoErr;
        bool QuickCheckJustCheck;
        bool SkipUpdateInfo;
        string OutputInfo;

        string StartOnErr;
        string StartOnErrArgs;

        /// <summary>This is only set for standalone service updating (not updating a service via the AutomaticUpdater)</summary>
        bool UpdatingFromService;

        Logger log;

        string forcedLanguageCulture;

        /// <summary>Custom proxy URL. Command line switch -proxy.</summary>
        string customProxyUrl;
        /// <summary>Custom proxy user name. Command line switch -proxyu.</summary>
        string customProxyUser;
        /// <summary>Custom proxy password. Command line switch -proxyp.</summary>
        string customProxyPassword;
        /// <summary>Custom proxy domain. Command line switch -proxyd.</summary>
        string customProxyDomain;

        #endregion Private variables

        public frmMain(string[] args)
        {
            //sets to SegoeUI on Vista
            Font = SystemFonts.MessageBoxFont;

            // check if user is an admin for windows 2000+
            IsAdmin = VistaTools.IsUserAnAdmin();

            InitializeComponent();

            //enable Lazy SSL for all downloads
            FileDownloader.EnableLazySSL();

            //resize the client so its client region = 500x360
            if (ClientRectangle.Width != 500)
                Width = (Width - ClientRectangle.Width) + 500;

            if (ClientRectangle.Height != 360)
                Height = (Height - ClientRectangle.Height) + 360;

            //add the panelDisplaying to form
            panelDisplaying.TabIndex = 0;
            Controls.Add(panelDisplaying);

            try
            {
                //process commandline argument
                Arguments commands = new Arguments(args);
                ProcessArguments(commands);

                // load the self update information
                if (!string.IsNullOrEmpty(selfUpdateFileLoc))
                {
                    //Note: always load the selfupdate data before the automatic update data
                    LoadSelfUpdateData(selfUpdateFileLoc);
                    ConfigureProxySettings();

                    //TODO: wyUp 3.0: excise this hack
                    //if the loaded file is from RC1, then update self and bail out
                    if (selfUpdateFromRC1)
                    {
                        //install the new client, and relaunch it to continue the update
                        if (needElevation && NeedElevationToUpdate())
                        {
                            //the user "elevated" as a non-admin user
                            //warn the user of their idiocy
                            error = clientLang.AdminError;

                            //set to false so new client won't be launched in frmMain_Load()
                            selfUpdateFromRC1 = false;

                            ShowFrame(Frame.Error);
                        }
                        else
                        {
                            needElevation = false;

                            FileAttributes atr = File.GetAttributes(oldSelfLocation);
                            bool resetAttributes = (atr & FileAttributes.Hidden) != 0 || (atr & FileAttributes.ReadOnly) != 0 || (atr & FileAttributes.System) != 0;

                            // remove the ReadOnly & Hidden atributes temporarily
                            if (resetAttributes)
                                File.SetAttributes(oldSelfLocation, FileAttributes.Normal);

                            //Install the new client
                            File.Copy(newSelfLocation, oldSelfLocation, true);

                            if (resetAttributes)
                                File.SetAttributes(oldSelfLocation, atr);

                            //Relaunch self in OnLoad()
                        }

                        //bail out
                        return;
                    }
                }
                else // not self-updating
                {
                    ConfigureProxySettings();
                }

                //Load the client information
                if (clientFileType == ClientFileType.PreRC2)
                    //TODO: wyUp 3.0: stop supporting old client files (barely anyone uses RC2).
                    update.OpenObsoleteClientFile(clientFileLoc);
                else
                    update.OpenClientFile(clientFileLoc, clientLang, forcedLanguageCulture, updatePathVar, customUrlArgs);

                clientLang.SetVariables(update.ProductName, update.InstalledVersion);
            }
            catch (Exception ex)
            {
                clientLang.SetVariables(update.ProductName, update.InstalledVersion);

                error = "Client file failed to load. The client.wyc file might be corrupt.";
                errorDetails = ex.Message;

                ShowFrame(Frame.Error);
                return;
            }

            //sets up Next & Cancel buttons
            SetButtonText();

            //set header alignment, etc.
            panelDisplaying.HeaderImageAlign = update.HeaderImageAlign;

            if (update.HeaderTextIndent >= 0)
                panelDisplaying.HeaderIndent = update.HeaderTextIndent;

            panelDisplaying.HideHeaderDivider = update.HideHeaderDivider;

            // set the 
            if (update.CustomWyUpdateTitle != null)
                Text = update.CustomWyUpdateTitle;

            try
            {
                if (!string.IsNullOrEmpty(update.HeaderTextColorName))
                    panelDisplaying.HeaderTextColor = Color.FromName(update.HeaderTextColorName);
            }
            catch { }

            //load the Side/Top images
            panelDisplaying.TopImage = update.TopImage;
            panelDisplaying.SideImage = update.SideImage;

            if (isAutoUpdateMode)
            {
                try
                {
                    // create the temp folder where we'll store the updates long term
                    if (tempDirectory == null)
                        tempDirectory = CreateAutoUpdateTempFolder();
                }
                catch (Exception ex)
                {
                    error = clientLang.GeneralUpdateError;
                    errorDetails = "Failed to create the automatic updater temp folder: " + ex.Message;

                    ShowFrame(Frame.Error);
                    return;
                }
                
                try
                {
                    // load the previous auto update state from "autoupdate"
                    LoadAutoUpdateData();
                    ConfigureProxySettings();
                }
                catch
                {
                    startStep = UpdateStepOn.Checking;
                }
            }
            else if (SelfUpdateState == SelfUpdateState.FullUpdate)
            {
                try
                {
                    // load the server file for MinClient needed details (i.e. failure case)
                    ServerFile = ServerFile.Load(serverFileLoc, updatePathVar, customUrlArgs);

                    //load the self-update server file
                    LoadClientServerFile();
                    clientLang.NewVersion = SelfServerFile.NewVersion;
                }
                catch (Exception ex)
                {
                    error = clientLang.ServerError;
                    errorDetails = ex.Message;

                    ShowFrame(Frame.Error);
                    return;
                }

                if (needElevation && NeedElevationToUpdate())
                {
                    //the user "elevated" as a non-admin user
                    //warn the user of their idiocy
                    error = clientLang.AdminError;
                    ShowFrame(Frame.Error);
                }
                else
                {
                    needElevation = false;

                    //begin updating the product
                    ShowFrame(Frame.InstallUpdates);
                }
            }
            //continuing from elevation or self update (or both)
            else if (SelfUpdateState == SelfUpdateState.ContinuingRegularUpdate)
            {
                try
                {
                    //load the server file (without filling the 'changes' box & without downloading the wyUpdate Server file)
                    LoadServerFile(false);
                }
                catch (Exception ex)
                {
                    error = clientLang.ServerError;
                    errorDetails = ex.Message;

                    ShowFrame(Frame.Error);
                    return;
                }

                if (needElevation && NeedElevationToUpdate())
                {
                    // the user "elevated" as a non-admin user
                    // warn the user of their idiocy
                    error = clientLang.AdminError;

                    ShowFrame(Frame.Error);
                }
                else
                {
                    needElevation = false;

                    //begin updating the product
                    ShowFrame(Frame.InstallUpdates);
                }
            }
            else if (!uninstalling)
                startStep = UpdateStepOn.Checking;
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);

            if (!_isApplicationRun)
                return;

            _isApplicationRun = false;

            if (isAutoUpdateMode)
            {
                /* SetupAutoupdateMode must happen after the handle is created
                     * (aka. in OnHandleCreated, or after base.SetVisibleCore() is called)
                     * because Control.Invoke() used in UpdateHelper
                     * requires the handle to be created.
                     *
                     * This solves the problem where the AutomaticUpdater control sends a message,
                     * it thinks the message was recieved successfully because there
                     * wasn't an error on the pipe stream, however in reality it never gets past
                     * the try-catch block in 'pipeServer_MessageReceived'. The exception is gobbled up
                     * and there's a stalemate: wyUpdate is waiting for its first message, AutomaticUpdater
                     * is waiting for a progress report.
                     */
                SetupAutoupdateMode();
            }


            // run the OnLoad code

            if (uninstalling)
            {
                ShowFrame(Frame.Uninstall);
            }
            else if (selfUpdateFromRC1)
            {
                //if the loaded file is from RC1, then update self and bail out

                //Relaunch self
                StartSelfElevated();
            }
            else if (startStep != UpdateStepOn.Nothing)
            {
                // either begin checking or load the step from the autoupdate file
                try
                {
                    PrepareStepOn(startStep);

                    // selfupdate & post-selfupdate installation
                    if (beginAutoUpdateInstallation)
                        UpdateHelper_RequestReceived(this, UpdateAction.UpdateStep, UpdateStep.Install);
                }
                catch (Exception ex)
                {
                    if (startStep != UpdateStepOn.Checking)
                        startStep = UpdateStepOn.Checking;
                    else
                    {
                        // show the error screen
                        error = "Automatic update state failed to load.";
                        errorDetails = ex.Message;

                        ShowFrame(Frame.Error);
                        return;
                    }

                    try
                    {
                        PrepareStepOn(startStep);
                    }
                    catch (Exception ex2)
                    {
                        // show the error screen
                        error = "Automatic update state failed to load.";
                        errorDetails = ex2.Message;

                        ShowFrame(Frame.Error);
                    }
                }
            }
        }


        void ProcessArguments(Arguments commands)
        {
            if (commands["supdf"] != null)
            {
                //the client is in self update mode
                selfUpdateFileLoc = commands["supdf"];

                // check if this instance is the "new self"
                if (commands["ns"] != null)
                    IsNewSelf = true;
            }
            else
            {
                forcedLanguageCulture = commands["forcelang"];

                // automatic update mode
                if (commands["autoupdate"] != null)
                {
                    // the actual pipe will be created when OnHandleCreated is called
                    isAutoUpdateMode = true;

                    // check if this instance is the "new self"
                    if (commands["ns"] != null)
                        IsNewSelf = true;
                }
                else if (commands["uninstall"] != null)
                {
                    // uninstall any newly created folders, files, or registry
                    uninstalling = true;
                }
                else // standalone updater mode
                {
                    if (commands["quickcheck"] != null)
                    {
                        WindowState = FormWindowState.Minimized;
                        ShowInTaskbar = false;

                        QuickCheck = true;

                        if (commands["noerr"] != null)
                            QuickCheckNoErr = true;

                        if (commands["justcheck"] != null)
                            QuickCheckJustCheck = true;

                        // for outputting errors & update information to
                        // STDOUT or to a file
                        if (QuickCheckNoErr || QuickCheckJustCheck)
                            OutputInfo = commands["outputinfo"];
                    }
                    else if (commands["fromservice"] != null)
                    {
                        SkipUpdateInfo = true;
                        UpdatingFromService = true;

                        if (!string.IsNullOrEmpty(commands["logfile"]))
                            log = new Logger(commands["logfile"]);
                    }

                    if (commands["skipinfo"] != null)
                        SkipUpdateInfo = true;

                    StartOnErr = commands["startonerr"];
                    StartOnErrArgs = commands["startonerra"];
                }

                // client data file
                if (commands["cdata"] != null)
                {
                    clientFileLoc = commands["cdata"];

                    if (clientFileLoc.EndsWith("iuc", StringComparison.OrdinalIgnoreCase))
                        clientFileType = ClientFileType.PreRC2;
                    else if (clientFileLoc.EndsWith("iucz", StringComparison.OrdinalIgnoreCase))
                        clientFileType = ClientFileType.RC2;
                    else
                        clientFileType = ClientFileType.Final;
                }
                else
                {
                    clientFileLoc = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client.wyc");
                    clientFileType = ClientFileType.Final;

                    //try the RC-2 filename
                    if (!File.Exists(clientFileLoc))
                    {
                        clientFileLoc = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "iuclient.iucz");
                        clientFileType = ClientFileType.RC2;
                    }
                    
                    //try Pre-RC2 filename
                    if (!File.Exists(clientFileLoc))
                    {
                        //if it doesn't exist, try without the 'z'
                        clientFileLoc = clientFileLoc.Substring(0, clientFileLoc.Length - 1);
                        clientFileType = ClientFileType.PreRC2;
                    }
                }

                //set basedirectory as the location of the executable
                baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (commands["basedir"] != null && Directory.Exists(commands["basedir"]))
                {
                    //if the specified directory exists, then set as directory
                    // also trim the trailing space
                    baseDirectory = commands["basedir"].TrimEnd();
                }

                // only generate a temp directory if we're not in AutoUpdate mode
                if (!isAutoUpdateMode)
                {
                    // create "random" temp dir.
                    tempDirectory = Path.Combine(Path.GetTempPath(), @"w" + DateTime.Now.ToString("sff"));
                    Directory.CreateDirectory(tempDirectory);
                }

                // load the passed server argument
                serverOverwrite = commands["server"];

                // load the custom updatepath directory
                updatePathVar = commands["updatepath"];

                // custom url arguments
                customUrlArgs = commands["urlargs"];

                customProxyUrl = commands["proxy"];
                customProxyUser = commands["proxyu"];
                customProxyPassword = commands["proxyp"];
                customProxyDomain = commands["proxyd"];

                // only allow silent uninstalls 
                if (uninstalling && commands["s"] != null)
                {
                    isSilent = true;

                    WindowState = FormWindowState.Minimized;
                    ShowInTaskbar = false;
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // only warn if after the welcome page and not self updating/elevating
            if (needElevation
                || SelfUpdateState == SelfUpdateState.WillUpdate
                || SelfUpdateState == SelfUpdateState.FullUpdate
                || isSilent
                || isAutoUpdateMode
                || isCancelled
                || panelDisplaying.TypeofFrame == FrameType.WelcomeFinish
                || panelDisplaying.TypeofFrame == FrameType.TextInfo)
            {
                //close the form
                e.Cancel = false;
            }
            else //currently updating
            {
                // stop closing
                e.Cancel = true;

                // prompt the user if they really want to cancel
                CancelUpdate();
            }

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            //if not self updating, then delete temp files.
            if (!(needElevation || SelfUpdateState == SelfUpdateState.WillUpdate || SelfUpdateState == SelfUpdateState.FullUpdate || isAutoUpdateMode))
            {
                RemoveTempDirectory();
            }

            if (isCancelled)
                ReturnCode = 3;

            base.OnClosed(e);
        }

        /// <summary>
        /// Remove the temporary directory if it exists.
        /// </summary>
        void RemoveTempDirectory()
        {
            if (!Directory.Exists(tempDirectory))
                return;

            try
            {
                Directory.Delete(tempDirectory, true);
            }
            catch { }
        }

        /// <summary>Configures the network access using the saved proxy settings.</summary>
        void ConfigureProxySettings()
        {
            if (!string.IsNullOrEmpty(customProxyUrl))
            {
                FileDownloader.CustomProxy = new WebProxy(customProxyUrl);

                if (!string.IsNullOrEmpty(customProxyUser) && !string.IsNullOrEmpty(customProxyPassword))
                {
                    FileDownloader.CustomProxy.Credentials = new NetworkCredential(
                        customProxyUser,
                        customProxyPassword,
                        // if the domain is null, use an empty string
                        customProxyDomain ?? string.Empty
                        );
                }
                else
                {
                    FileDownloader.CustomProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                }
            }
        }
    }
}
