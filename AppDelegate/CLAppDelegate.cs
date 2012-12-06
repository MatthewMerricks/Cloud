//
//  CLAppDelegate.cs
//  Cloud Windows
//
//  Created by BobS on 5/9/12.
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using CloudApiPrivate.Model.Settings;
using System.Windows.Threading;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Principal;
using win_client.Common;
using System.Windows;
using System.Resources;
using System.Reflection;
using CloudApiPublic;
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using CloudApiPrivate.Static;
using System.Security.Permissions;
using System.Windows.Media.Imaging;
using win_client.ViewModels;
using win_client.Views;
using win_client.Resources;
using win_client.Services.ServicesManager;
using System.Data;
using System.Data.OleDb;
using CloudApiPublic.Static;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using CloudApiPrivate.Model;
using CloudApiPrivate.Common;
using System.Threading;
using System.Threading.Tasks;

namespace win_client.AppDelegate
{

    /// <summary>
    /// Singleton class to represent application delegate support.
    /// </summary>
    public sealed class CLAppDelegate
    {
        #region "Private instance fields"
        /// <summary>
        /// Allocate ourselves. We have a private constructor, so no one else can.
        /// </summary>
        private static CLAppDelegate _instance = null;
        private static object InstanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance; 
        private static bool _isWindowPlacedFirstTime = false;
        private static bool _isUnlinked = false;
        #endregion

        #region "Public Definitions"
        public delegate void UnlinkFromCloudDotComAsyncCallback(CLError error);
        public delegate void InstallCloudServicesAsyncCallback(CLError error);
        #endregion

        #region Public Properties

        public string StartupUrlRelative { get; set; }
        public string FoundOriginalCloudFolderPath { get; set; }
        public DateTime FoundDeletedCloudFolderDeletionTimeLocal { get; set; }
        public string FoundPathToDeletedCloudFolderRFile { get; set; }
        public string FoundPathToDeletedCloudFolderIFile { get; set; }


        public Window AppMainWindow { get; set; }
        public DialogCheckForUpdates CheckForUpdatesWindow { get; set; }

        private bool _isAlreadyRunning = false;
        public bool IsAlreadyRunning
        {
            get
            {
                return _isAlreadyRunning;
            }
        }

        private bool _isFirstTimeSetupNeeded = false;
        public bool IsFirstTimeSetupNeeded
        {
            get
            {
                return _isFirstTimeSetupNeeded;
            }
        }

        private Dispatcher _mainDispatcher = null;
        public Dispatcher MainDispatcher
        {
            get
            {
                return _mainDispatcher;
            }
        }

        #endregion

        private string _pageCloudFolderMissingOkButtonContent;
        public string PageCloudFolderMissingOkButtonContent
        {
            get { return _pageCloudFolderMissingOkButtonContent; }
            set { _pageCloudFolderMissingOkButtonContent = value; }
        }

        private string _pageCloudFolderMissingOkButtonTooltipContent;
        public string PageCloudFolderMissingOkButtonTooltipContent
        {
            get { return _pageCloudFolderMissingOkButtonTooltipContent; }
            set { _pageCloudFolderMissingOkButtonTooltipContent = value; }
        }
        

        #region "Life Cycle"
        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLAppDelegate Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLAppDelegate();
                        _instance.initAppDelegate();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLAppDelegate()
        {
            // Initialize members, etc. here.
            _mainDispatcher = Application.Current.Dispatcher;
        }

        private static bool IsVista()
        {
            string majorOSVersion = Environment.OSVersion.Version.Major.ToString();
            if (majorOSVersion.Equals(Convert.ToString(6)))
            {
                return true;
            }
            return false;
        }

        private static bool IsWin7()
        {
            string majorOSVersion = Environment.OSVersion.Version.Major.ToString();
            if (majorOSVersion.Equals(Convert.ToString(7)))
            {
                return true;
            }
            return false;
        }

        private class RecyclerDetails
        {
            public const int CreationDateIndex = 5;
            public const int FileNameIndex = 0;
            public const int FileParentDirectoryIndex = 1;

            public string CreationDate { get; set; }
            public string FileName { get; set; }
            public string FileParentDirectory { get; set; }
        }

        /// <summary>
        /// Lazy initialization
        /// </summary>
        [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        private void initAppDelegate()
        {
            //// TODO: Needed? registers application to listen to url handling events.
            //[[NSAppleEventManager sharedAppleEventManager] setEventHandler:self andSelector:@selector(handleURLFromEvent:) forEventClass:kInternetEventClass andEventID:kAEGetURL];

            // we only allow one instance of our app.
            StartupUrlRelative = Resources.Resources.startupUriPageInvisible;     // assume we will simply start running with no UI displayed
            if (isCloudAppAlreadyRunning())
            {
                // Tell the app.xaml.cs instance logic that we are already running.
                _isAlreadyRunning = true;
                StartupUrlRelative = Resources.Resources.startupUriAlreadyRunning;
                return;
            }

            // Start a single instance of the updater window.
            _trace.writeToLog(9, "CLAppDelegate: initAppDelegate: new DialogCheckForUpdates.");
            CheckForUpdatesWindow = new DialogCheckForUpdates();
            CheckForUpdatesWindow.Width = 0;
            CheckForUpdatesWindow.Height = 0;
            CheckForUpdatesWindow.MinWidth = 0;
            CheckForUpdatesWindow.MinHeight = 0;
            CheckForUpdatesWindow.Left = Int32.MaxValue;
            CheckForUpdatesWindow.Top = Int32.MaxValue;
            CheckForUpdatesWindow.ShowInTaskbar = false;
            CheckForUpdatesWindow.ShowActivated = false;
            CheckForUpdatesWindow.Visibility = System.Windows.Visibility.Hidden;
            CheckForUpdatesWindow.WindowStyle = WindowStyle.None;
            CheckForUpdatesWindow.Show();

            // Determine which window we will show on startup
            SetupCloudAppLaunchMode();
        }

        /// <summary>
        /// Call to release resources at application shutdown
        /// </summary>
        public void cleanUpAppDelegate()
        {
        }
 
        #endregion

        #region "Initialization and Start-up"

        /// <summary>
        /// Check to see if we are already running.
        /// </summary>
        private bool isCloudAppAlreadyRunning()
        {
            bool isCloudAppRunning = false;

            Process[] processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            string currentOwner = WindowsIdentity.GetCurrent().Name.ToString();
            var query = from p in processes
                            where currentOwner.ToLowerInvariant().
                            Contains(GetProcessOwner(p.Id).ToLowerInvariant())
                            select p;
            int instance = query.Count();
            if (instance > 1)
            {
                isCloudAppRunning = true;
            }

            return isCloudAppRunning;
        }

        /// <summary>
        /// Get the owner of this process.
        /// </summary>
        static string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    searcher.Dispose();
                    return argList[0];
                }
            }
            searcher.Dispose();
            return string.Empty;
        }

        /// <summary>
        /// Unlink this device from Cloud.com
        /// </summary>
        


        //- (BOOL)unlinkFromCloudDotCom
        public void UnlinkFromCloudDotComSync(out CLError error)
        {
            try
            {
                // Don't run this twice
                _trace.writeToLog(9, "CLAppDelegate: UnlinkFromCloudDotCom: Entry.");
                if (_isUnlinked)
                {
                    _trace.writeToLog(9, "CLAppDelegate: UnlinkFromCloudDotCom: Already unlinked.  Return.");
                    error = null;
                    return;
                }

                //// Moved cleaning settings to before unlink because there if unlink succeeds on the server but we go down,
                //// then we still have the AKey stored and it will be invalid; this risks leaving extra devices linked
                ////
                //// clean our settings
                //[[CLSettings sharedSettings] resetSettings];
                _trace.writeToLog(9, "CLAppDelegate: UnlinkFromCloudDotCom: Reset settings.");
                string copyAkey = Settings.Instance.Akey;
                Settings.Instance.resetSettings();

                //CLRegistration *regstration = [[CLRegistration alloc] init];
                //rc = [regstration unlinkDeviceWithAccessKey:[[CLSettings sharedSettings] aKey]];
                if (!String.IsNullOrEmpty(copyAkey))
                {
                    _trace.writeToLog(9, "CLAppDelegate: UnlinkFromCloudDotCom: Unlink from the server.");
                    CLRegistration registration = new CLRegistration();
                    registration.UnlinkDeviceWithAccessKey(copyAkey, out error);
                }
                else
                {
                    error = null;
                }

                // Clear database (will start again at SID "0")
                _trace.writeToLog(9, "CLAppDelegate: UnlinkFromCloudDotCom: Wipe the index.");
                win_client.Services.FileSystemMonitoring.CLFSMonitoringService.Instance.SyncBox.WipeIndex();

                //// stop services.
                //CLAppDelegate *delegate = [NSApp delegate];
                //[delegate stopCloudAppServicesAndUI];
                _trace.writeToLog(9, "CLAppDelegate: UnlinkFromCloudDotCom: StopCloudAppServicesAndUI.");
                StopCloudAppServicesAndUI();

                // Remove the Cloud folder if it contains only our Public and Pictures folder.
                _trace.writeToLog(9, "CLAppDelegate: UnlinkFromCloudDotCom: Remove a possible null cloud folder.");
                RemoveNullCloudFolder();

                // Remove all of the Cloud folder shortcuts
                //TODO: Should we remove the cloud folder shortcuts when we unlink?
                //CLShortcuts.RemoveCloudFolderShortcuts(Settings.Instance.CloudFolderPath);

                // Remove the autostart of Cloud.exe
                _trace.writeToLog(9, "CLAppDelegate: UnlinkFromCloudDotCom: Remove the autostart of Cloud.exe.");
                CLShortcuts.RemoveCloudAutostartShortcut();

                //[[CLCoreDataController defaultController] removeCoreDataStore];
                //TODO: Is SQLIndexer terminated properly?

                //// Stop the badging agent.  Do this synchronously because the system may be immediately exiting.
                //NSString *path = [[[NSBundle mainBundle] pathForResource:@"LICENSE" ofType:@""] stringByDeletingLastPathComponent];
                //NSString *cmd = [path stringByAppendingString:@"/CloudApp.bundle/CloudApp.app/Contents/MacOS/CloudApp -d"];
                //system([cmd UTF8String]);
                //TODO: Stop the badging service.

                //return rc;
                _isUnlinked = true;
            }
            catch (Exception ex)
            {
                error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(9, "CLAppDelegate: UnlinkFromCloudDotCom: ERROR.  Exception.  Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode);
            }
            _trace.writeToLog(9, "CLAppDelegate: UnlinkFromCloudDotCom: Exit.");
        }

        /// <summary>
        /// Public method that will asynchronously ask the server to unlink this device from the user's account.
        /// <param name="callback">The user's callback function which will execute when the asynchronous request is complete.</param>
        /// <param name="timeoutInSeconds">The maximum time that this request will remain active.  It will be cancelled if it is not complete within this time.  Specify Double.MaxValue for no timeout.</param>
        /// </summary>
        public void UnlinkFromCloudDotComAsync(UnlinkFromCloudDotComAsyncCallback callback, double timeoutInSeconds)
        {
            var tsMain = new CancellationTokenSource();
            CancellationToken ctMain = tsMain.Token;

            var tsTimeout = new CancellationTokenSource();
            CancellationToken ctTimeout = tsMain.Token;

            // Start the thread to be used to communicate with the server.
            CLError errorFromAsync = null;
            Task.Factory.StartNew(() => UnlinkFromCloudDotComSync(out errorFromAsync)).ContinueWith(task =>
            {
                bool bResult = false;
                CLError err = null;

                Exception ex = task.Exception;
                if (ex == null)
                {
                    bResult = true;
                }

                if (ex != null)
                {
                    err += ex;
                }
                else if (!bResult)
                {
                    err = errorFromAsync;
                }

                // The server communication is complete.  Kill the timeout thread.
                tsTimeout.Cancel();

                // Call the user's (of the API) callback.  This callback will execute on the main thread.
                // The user's callback function may crash.  Just let the application crash if that happens.
                // Exit this thread after the callback returns.
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    callback(err);
                }));
            }, ctMain);

            // Start timeout thread
            Task.Factory.StartNew(() =>
            {
                int ticksUntilTimeout = (int)(timeoutInSeconds / 0.100);
                for (int i = 0; i < ticksUntilTimeout; ++i)
                {
                    if (ctTimeout.IsCancellationRequested)
                    {
                        // We were cancelled because the HTTP request completed.  Exit the timeout thread.
                        return;
                    }
                    Thread.Sleep(100);
                }

                // We timed out.  Kill the main thread and exit ours.
                tsMain.Cancel();
            }, ctTimeout);
        }

        /// <summary>
        /// Remove the Cloud folder if it has only our Public and Pictures folders in it.
        /// </summary>
        private void RemoveNullCloudFolder()
        {
            try
            {
                // Only if the directory exists in the expected spot, and only if we are not restarting to move the cloud folder.
                bool otherFileExists = false;
                if (!Settings.Instance.IsMovingCloudFolder && Directory.Exists(Settings.Instance.CloudFolderPath))
                {
                    // Iterate through all of the files in the directory.  Stop if we get anything other than the
                    // Public and/or Pictures directory.
                    foreach (string entry in Directory.EnumerateFileSystemEntries(Settings.Instance.CloudFolderPath, "*.*", SearchOption.AllDirectories))
                    {
                        if (entry.Equals(Settings.Instance.CloudFolderPath + "\\" + Resources.Resources.CloudFolderPicturesFolder, StringComparison.InvariantCulture)
                               || entry.Equals(Settings.Instance.CloudFolderPath + "\\" + Resources.Resources.CloudFolderDocumentsFolder, StringComparison.InvariantCulture)
                               || entry.Equals(Settings.Instance.CloudFolderPath + "\\" + Resources.Resources.CloudFolderVideosFolder, StringComparison.InvariantCulture))
                        {
                            continue;
                        }

                        // Some file exists in the CLoud folder
                        otherFileExists = true;
                        break;
                    }

                    // Delete the Cloud folder if it doesn't have any files
                    if (!otherFileExists)
                    {
                        Directory.Delete(Settings.Instance.CloudFolderPath, recursive: true);
                    }
                }

            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                _trace.writeToLog(1, "CLAppDelegate: ExitApplication: ERROR: Exception: Msg: <{0}>. Code: {1}", error.errorDescription, error.errorCode);
            }
        }

        /// <summary>
        /// Perform one-time installation (cloud folder, and any OS support)
        /// </summary>
        //- (void)stopCloudAppServicesAndUI
        public void StopCloudAppServicesAndUI()
        {
            // Stop core services
            CLServicesManager.Instance.StopCoreServices();
        }

        /// <summary>
        /// Exit the application.
        /// </summary>
        public void ExitApplication()
        {
            // If the user has exited setup in the middle, or there was a restart, we may be partially set up.  Unlink.
            if (!Settings.Instance.CompletedSetup)
            {
                CLError error = null;
                try
                {
                    // Unlink.  Remove all of the settings and unlink from the server if we can.
                    this.UnlinkFromCloudDotComSync(out error);
                    if (error != null)
                    {
                        _trace.writeToLog(1, "CLAppDelegate: ExitApplication: ERROR: Exception: Msg: <{0}>. Code: {1}", error.errorDescription, error.errorCode);
                    }
                }
                catch (Exception ex)
                {
                    error += ex;
                    error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                    _trace.writeToLog(1, "CLAppDelegate: ExitApplication: ERROR: Exception(2): Msg: <{0}>. Code: {1}", error.errorDescription, error.errorCode);
                }
            }

            // Actually shut down the application now.
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Perform one-time installation (cloud folder, and any OS support)is th
        /// </summary>
        public void installCloudServices(out CLError error)
        {
            error = null;

            // TODO: Install any OS integration support (badging, etc.)
            // Set error to null or error information.

            // Create the cloud folder
            DateTime creationTime;
            CLCreateCloudFolder.CreateCloudFolder(Settings.Instance.CloudFolderPath, out creationTime, out error);
            if (error == null)
            {
                // Set setup process completed
                _trace.writeToLog(1, "Cloud folder created at <{0}>.", Settings.Instance.CloudFolderPath);

                // Set setup process completed
                Settings.Instance.CloudFolderCreationTimeUtc = creationTime;
                Settings.Instance.updateCloudFolderPath(Settings.Instance.CloudFolderPath, creationTime);
                Settings.Instance.setCloudAppSetupCompleted(true);

                // NOTE: PageInvisible will be shown which will start the core servicees.
            }
        }

        /// <summary>
        /// Public method that will asynchronously install the cloud services.
        /// <param name="callback">The user's callback function which will execute when the asynchronous request is complete.</param>
        /// <param name="timeoutInSeconds">The maximum time that this request will remain active.  It will be cancelled if it is not complete within this time.  Specify Double.MaxValue for no timeout.</param>
        /// </summary>
        public void InstallCloudServicesAsync(InstallCloudServicesAsyncCallback callback, double timeoutInSeconds)
        {
            var tsMain = new CancellationTokenSource();
            CancellationToken ctMain = tsMain.Token;

            var tsTimeout = new CancellationTokenSource();
            CancellationToken ctTimeout = tsMain.Token;

            // Start the thread to be used to communicate with the server.
            CLError errorFromAsync = null;
            Task.Factory.StartNew(() => installCloudServices(out errorFromAsync)).ContinueWith(task =>
            {
                bool bResult = false;
                CLError err = null;

                Exception ex = task.Exception;
                if (ex == null)
                {
                    bResult = true;
                }

                if (ex != null)
                {
                    err += ex;
                }
                else if (!bResult)
                {
                    err = errorFromAsync;
                }

                // The server communication is complete.  Kill the timeout thread.
                tsTimeout.Cancel();

                // Call the user's (of the API) callback.  This callback will execute on the main thread.
                // The user's callback function may crash.  Just let the application crash if that happens.
                // Exit this thread after the callback returns.
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    callback(err);
                }));
            }, ctMain);

            // Start timeout thread
            Task.Factory.StartNew(() =>
            {
                int ticksUntilTimeout = (int)(timeoutInSeconds / 0.100);
                for (int i = 0; i < ticksUntilTimeout; ++i)
                {
                    if (ctTimeout.IsCancellationRequested)
                    {
                        // We were cancelled because the HTTP request completed.  Exit the timeout thread.
                        return;
                    }
                    Thread.Sleep(100);
                }

                // We timed out.  Kill the main thread and exit ours.
                tsMain.Cancel();
            }, ctTimeout);
        }

        /// <summary>
        /// We will go onto the system tray.  Start the app services and UI
        /// </summary>
        public void startCloudAppServicesAndUI()
        {
            // Start core services
            CLServicesManager.Instance.StartCoreServices();
        }

        /// <summary>
        /// Get the application trace instance
        /// </summary>
        public CLTrace GetTrace()
        {
            return _trace;
        }

        //- (void)setupCloudAppLaunchMode
            //if (isFirstTimeSetupNeeded())
            //{
            //    // Tell the app.xaml.cs instance logic that we need the welcome dialog.
            //    _isFirstTimeSetupNeeded = true;

            //}
            //else {
            //    // DO NOTHING HERE.  The PageInvisible will be loaded and the loaded event will invoke the
            //    // startCloudAppServicesAndUI() method on the UI thread after a small delay.  This insures
            //    // that the system tray icon support is in place before we initialize the services.
            //    //startCloudAppServicesAndUI();
            //}

        //- (void)cloudFolderHasBeenDeleted:(NSString *)path
        void CloudFolderHasBeenDeleted(string path)
        {
            // Here we have to halt. this means the folder has been deleted.
            StopCloudAppServicesAndUI();

            //TODO: // show the repair window
            // self.folderMissingController = [[CLFolderMissingController alloc] initWithWindowNibName:@"CLFolderMissingController"];
            // [self.folderMissingController setCloudFolderPath:path];
            // [self.folderMissingController showWindow:self.folderMissingController.window];
            // [self.folderMissingController.window orderFrontRegardless];
            // Note: The window will be shown by App.xaml.cs using the StartupUri determined in this module.
        }

        void SetupCloudAppLaunchMode()
        {
            // // todo: here we should go back to our cloud and verify if
            // // device is still valid to help make the correct determination if we need to
            // // setup or not.

            // BOOL setupNeeded = YES;
            // BOOL shouldStartCoreServices = YES; 
            bool setupNeeded = true;
            bool shouldStartCoreServices = true;

            // if ([[CLSettings sharedSettings] completedSetup]) {
            if (Settings.Instance.CompletedSetup)
            {
                // setupNeeded = NO;
                setupNeeded = false;

                // if ([[NSFileManager defaultManager] fileExistsAtPath:[[CLSettings sharedSettings] cloudFolderPath]] == NO) {
                if (!Directory.Exists(Settings.Instance.CloudFolderPath))
                {
                    // NSData *bookmark = [[CLSettings sharedSettings] bookmarkDataForCloudFolder];
                    // NSURL *cloudFolderURL = [NSURL URLByResolvingBookmarkData:bookmark
                    //                                                   options:NSURLBookmarkResolutionWithoutUI
                    //                                             relativeToURL:NULL
                    //                                       bookmarkDataIsStale:NO
                    //                                                     error:NULL];

                    // NSString *cloudFolderPath = [cloudFolderURL path];

                    // if (cloudFolderPath != nil) {

                    //     if ([cloudFolderPath rangeOfString:@".Trash"].location != NSNotFound) { // folder got moved to trash, ask user to restore.

                    //         shouldStartCoreServices = NO;
                    //         [self cloudFolderHasBeenDeleted:cloudFolderPath];
                    //     }
                    //     else {

                    //         [[CLSettings sharedSettings] updateCloudFolderPath:cloudFolderPath]; // automatically pick up the new folder location.
                    //     }
                    // }
                    // else {

                    //     shouldStartCoreServices = NO;
                    //     [self cloudFolderHasBeenDeleted:nil]; // we couldn't find the folder, ask the user to locate it. 
                    // }

                    // The cloud folder was not found.  Try to locate the folder if it was moved or renamed.
                    string movedCloudFolderPath = LocateMovedCloudFolder();
                    if (movedCloudFolderPath != null)
                    {
                        // Update the location of the cloud folder path.  We will continue normally and start core services.
                        Settings.Instance.updateCloudFolderPath(movedCloudFolderPath, Settings.Instance.CloudFolderCreationTimeUtc);
                    }
                    else
                    {
                        // We didn't locate a moved cloud folder.  Some UI will be displayed, so don't start core services.
                        shouldStartCoreServices = false;

                        // Try to locate a deleted cloud folder
                        string foundOriginalPath;
                        DateTime foundDeletionTimeLocal;
                        string foundPathToDeletedCloudFolderRFile;
                        string foundPathToDeletedCloudFolderIFile;
                        bool isDeletedFolderFound = LocateCloudFolderInRecycleBin(Settings.Instance.CloudFolderPath, Settings.Instance.CloudFolderCreationTimeUtc,
                                                                            out foundOriginalPath, out foundDeletionTimeLocal, 
                                                                            out foundPathToDeletedCloudFolderRFile, out foundPathToDeletedCloudFolderIFile);
                        if (isDeletedFolderFound)
                        {
                            // We will put up a window in App.xaml.cs to allow the user to recover the deleted cloud folder.
                            StartupUrlRelative = Resources.Resources.startupUriCloudFolderMissing;
                            FoundOriginalCloudFolderPath = foundOriginalPath;
                            FoundDeletedCloudFolderDeletionTimeLocal = foundDeletionTimeLocal;
                            FoundPathToDeletedCloudFolderRFile = foundPathToDeletedCloudFolderRFile;
                            FoundPathToDeletedCloudFolderIFile = foundPathToDeletedCloudFolderIFile;
                            PageCloudFolderMissingOkButtonContent = Resources.Resources.pageCloudFolderMissingOkButtonRestore;
                            PageCloudFolderMissingOkButtonTooltipContent = Resources.Resources.pageCloudFolderMissingOkButtonRestoreTooltip;
                        }
                        else
                        {
                            // We will put up a window in App.xaml.cs to allow the user to make a new cloud folder or unlink.
                            StartupUrlRelative = Resources.Resources.startupUriCloudFolderMissing;
                            PageCloudFolderMissingOkButtonContent = Resources.Resources.pageCloudFolderMissingOkButtonLocate;
                            PageCloudFolderMissingOkButtonTooltipContent = Resources.Resources.pageCloudFolderMissingOkButtonLocateTooltip;
                        }
                    }
                }
            }

            // if (setupNeeded == YES) {
            if (setupNeeded)
            {
                // // can't start our core services if we're not setup
                // shouldStartCoreServices = NO;
                shouldStartCoreServices = false;

                // // welcome our new user
                // self.welcomeController  = [[CLWelcomeWindowController alloc] initWithWindowNibName:@"CLWelcomeWindowController"];
                // [self.welcomeController showWindow:self.welcomeController.window];
                // [[self.welcomeController window] orderFrontRegardless];
                StartupUrlRelative = Resources.Resources.startupUriFirstTimeSetup;
            }

            // if (shouldStartCoreServices == YES) {
            if (shouldStartCoreServices)
            {
                //     // business as usual, let's take this sync for a spin!
                //     [self startCloudAppServicesAndUI];
                // Note: PageInvisible will fire this soon. 
                //startCloudAppServicesAndUI();
            }
        }

        /// <summary>
        /// Locate the cloud folder if it was moved, but not deleted.
        /// </summary>
        /// <param name="void"></param>
        /// <returns>string: The moved cloud folder, or null.</returns>
        private static string LocateMovedCloudFolder()
        {
            DateTime cloudFolderCreationTimeUtc = Settings.Instance.CloudFolderCreationTimeUtc;
            DateTime cloudFolderCreationTimeUtcPlusOneSecond = cloudFolderCreationTimeUtc + TimeSpan.FromSeconds(1);
            string sCloudFolderCreationTimeUtc = cloudFolderCreationTimeUtc.ToString("yyyy-MM-dd HH:mm:ss");
            string sCloudFolderCreationTimeUtcPlusOneSecond = cloudFolderCreationTimeUtcPlusOneSecond.ToString("yyyy-MM-dd HH:mm:ss");
            string sSql = String.Format("SELECT System.ItemPathDisplay FROM SYSTEMINDEX WHERE " +
                                        "System.ItemType='Directory' AND System.DateCreated >= '{0}' AND System.DateCreated < '{1}'", sCloudFolderCreationTimeUtc, sCloudFolderCreationTimeUtcPlusOneSecond);

            List<string> resultingPaths = new List<string>();
            using (OleDbConnection objConnection = new OleDbConnection("Provider=Search.CollatorDSO;Extended Properties='Application=Windows';"))
            {
                OleDbCommand cmd = new OleDbCommand(sSql, objConnection);
                try
                {
                    objConnection.Open();
                    OleDbDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        resultingPaths.Add((string)reader[0]);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    CLError error = ex;
                    error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                    _trace.writeToLog(1, "CLAppDelegate: LocateMovedCloudFolder: ERROR: Exception.  Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                }
            }

            resultingPaths.RemoveAll(x => 
                { return (x.LastPathComponent().Equals(Resources.Resources.CloudFolderPicturesFolder, StringComparison.InvariantCulture) 
                        || x.LastPathComponent().Equals(Resources.Resources.CloudFolderDocumentsFolder, StringComparison.InvariantCulture) 
                        || x.LastPathComponent().Equals(Resources.Resources.CloudFolderVideosFolder, StringComparison.InvariantCulture)); 
                });
            string foundPath = null;
            if (resultingPaths.Count > 0)
            {
                foundPath = resultingPaths[0];
            }
            return foundPath;
        }

        /// <summary>
        /// Locate the cloud folder in the recycle bin via it's creation time.
        /// </summary>
        /// <param name="cloudFolderPath">  The original cloud folder location</param>
        /// <param name="cloudFolderCreationTime">  The original cloud folder creation time (UTC).</param>
        /// <param name="foundOriginalPath"> If found, the original path of the folder (where it was deleted from).</param>
        /// <param name="foundDeletionTimeLocal"> If found, the deletion time (local).</param>
        /// <param name="foundPathToDeletedCloudFolderRFile"> If found, the full path of the deleted folder $R* file (the actual folder).</param>
        /// <param name="foundPathToDeletedCloudFolderIFile"> If found, the full path of the deleted folder $I* file (the deleted folder information file).</param>
        /// <returns>bool: True: Cloud folder found.</returns>
        bool LocateCloudFolderInRecycleBin(string cloudFolderLocation, DateTime cloudFolderCreationTimeUtc,
                                            out string foundOriginalPath, out DateTime foundDeletionTimeLocal, out string foundPathToDeletedCloudFolderRFile, out string foundPathToDeletedCloudFolderIFile)
        {
            try
            {
                string recycleLocation = String.Empty;
                string strKeyPath = "SOFTWARE\\Microsoft\\Protected Storage System Provider";
                RegistryKey regKey = Registry.CurrentUser.OpenSubKey(strKeyPath);
                string[] arrSubKeys = regKey.GetSubKeyNames();
                if (IsVista() || IsWin7())
                {
                    recycleLocation = "$Recycle.bin";
                }
                else
                {
                    recycleLocation = "RECYCLER";
                }
                ObjectQuery query = new ObjectQuery("Select * from Win32_LogicalDisk Where DriveType = 3");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                ManagementObjectCollection queryCollection = searcher.Get();
                foreach (ManagementObject mgtObject in queryCollection)
                {
                    // Get the drive letter of this drive.  We will use it if it matches the original cloud folder path.
                    string strTmpDrive = mgtObject["Name"].ToString();
                    if (cloudFolderLocation.Substring(0, 2).ToLower().Equals(strTmpDrive.ToLower(), StringComparison.InvariantCulture))
                    {
                        // Loop through the recycle bins on this drive (usually just one).
                        foreach (string strSubKey in arrSubKeys)
                        {
                            // Form the full path of the recycle bin and process it if it exists.
                            string regKeySID = strSubKey;
                            string recycleBinLocation = (strTmpDrive + "\\" + recycleLocation + "\\" + regKeySID + "\\");
                            if (recycleBinLocation != "" && Directory.Exists(recycleBinLocation))
                            {
                                // Process this directory.  Get the list of folders contained in the recycle bin.  Loop through the folders.
                                DirectoryInfo recycleBin = new DirectoryInfo(recycleBinLocation);
                                DirectoryInfo[] recycleBinFolders = recycleBin.GetDirectories();
                                foreach (DirectoryInfo folder in recycleBinFolders)
                                {
                                    // See if this is the original cloud folder.  It is if the folder creation time is equal.
                                    if (folder.CreationTime.ToUniversalTime() == cloudFolderCreationTimeUtc)
                                    {
                                        // This is our cloud folder.  Find the original path.  That is in the corresponding
                                        // $I file.  e.g., if the name of this $R file is $RABCDEF, then locate the file
                                        // with name $IABCDEF.  The deletion time is stored in 8 bytes at offset 0x10 in the
                                        /// $I file.  The Unicode original path string is stored at offset 0x18.  The
                                        // path string is a maximum of 260 characters (or 520 bytes), including the double
                                        // null termination.
                                        string iFilePath = recycleBinLocation + "\\$I" + folder.Name.Substring(2);
                                        byte[] fileBytes = File.ReadAllBytes(iFilePath);

                                        // Parse out the DateTime ticks.
                                        const int offsetDateTimeTicks = 0x10;
                                        const int offsetPath = 0x18;
                                        const int lengthPath = 520;
                                        UInt64 ulTicks = 0;

                                        for (int index = offsetDateTimeTicks; index < offsetPath; ++index)
                                        {
                                            ulTicks |= ((UInt64)fileBytes[index]) << ((index - offsetDateTimeTicks) * 8);
                                        }

                                        // Parse out the original path
                                        string originalPath = String.Empty;
                                        UInt16 uChar = 0;
                                        for (int index = offsetPath; index < (offsetPath + lengthPath); ++index)
                                        {
                                            int tempShift = fileBytes[index] << ((index % 2) * 8);
                                            uChar |= (UInt16)tempShift;

                                            if (index != offsetPath && (index % 2) != 0)
                                            {
                                                if (uChar == 0)
                                                {
                                                    break;
                                                }

                                                originalPath += (char)uChar;
                                                uChar = 0;
                                            }
                                        }


                                        foundOriginalPath = originalPath;
                                        foundDeletionTimeLocal = DateTime.FromFileTime((long)ulTicks);
                                        foundPathToDeletedCloudFolderRFile = folder.FullName;
                                        foundPathToDeletedCloudFolderIFile = iFilePath;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);

                foundOriginalPath = null;
                foundDeletionTimeLocal = (DateTime)Helpers.DefaultForType(typeof(DateTime));
                foundPathToDeletedCloudFolderRFile = null;
                foundPathToDeletedCloudFolderIFile = null;
                return false;
            }

            foundOriginalPath = null;
            foundDeletionTimeLocal = (DateTime)Helpers.DefaultForType(typeof(DateTime));
            foundPathToDeletedCloudFolderRFile = null;
            foundPathToDeletedCloudFolderIFile = null;
            return false;
        }

        #endregion
        #region Support Functions

        /// <summary>
        /// Show the main window.  Call this whenever a page loads in the NavigationWindow.
        /// </summary>
        /// <param name="void"></param>
        /// <returns>void</returns>
        public static void ShowMainWindow(Window window)
        {
            // Set the containing window to be invisible
            if (window != null)
            {
                window.MinWidth = 645;
                window.MinHeight = 485;
                window.MaxWidth = 650;
                window.MaxHeight = 485;
                window.WindowStyle = WindowStyle.ThreeDBorderWindow;
                //window.Visibility = System.Windows.Visibility.Visible;
                window.ShowInTaskbar = true;
                window.ShowActivated = true;

                // Show the window, and set the placement if we should.
                if (!_isWindowPlacedFirstTime || window.Visibility != Visibility.Visible)
                {
                    _isWindowPlacedFirstTime = true;
                    window.SetPlacement(Settings.Instance.MainWindowPlacement);
                }

                window.Show();
            }
        }

        /// <summary>
        /// Hide the main window.  Call this whenever navigating to PageInvisible.
        /// </summary>
        /// <param name="void"></param>
        /// <returns>void</returns>
        public static void HideMainWindow(Window window)
        {
            // Set the containing window to be invisible
            if (window != null)
            {
                // Save the current position of the window.
                if (window.WindowStyle != WindowStyle.None && window.Visibility == Visibility.Visible)
                {
                    _trace.writeToLog(1, "CLAppDelegate: HideMainWindow: Set MainWindowPlacement. Coords: {0},{1},{2},{3}(LRWH). Title: {4}.", window.Left, window.Top, window.Width, window.Height, window.Title);
                    Settings.Instance.MainWindowPlacement = window.GetPlacement();
                }

                // Make sure the window is truely gone, way off screen...
                window.Width = 0;
                window.Height = 0;
                window.MinWidth = 0;
                window.MinHeight = 0;
                window.Left = Int32.MaxValue;
                window.Top = Int32.MaxValue;
                window.ShowInTaskbar = false;
                window.ShowActivated = false;
                window.Visibility = System.Windows.Visibility.Hidden;
                window.WindowStyle = WindowStyle.None;
            }
        }
        #endregion  
    }
} 

