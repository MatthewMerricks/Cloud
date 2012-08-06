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
using win_client.SystemTray.TrayIcon;
using win_client.ViewModels;
using win_client.Views;
using win_client.Services.ServicesManager;
using System.Data;
using System.Data.OleDb;
using CloudApiPublic.Static;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using CloudApiPrivate.Model;

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
        //&&&&private TrayIcon _trayIcon;
        #endregion
        #region Public Properties

        public string StartupUrlRelative { get; set; }
        public string FoundOriginalCloudFolderPath { get; set; }
        public DateTime FoundDeletedCloudFolderDeletionTimeLocal { get; set; }
        public string FoundPathToDeletedCloudFolderRFile { get; set; }
        public string FoundPathToDeletedCloudFolderIFile { get; set; }


        public Window AppMainWindow { get; set; }

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

        private ResourceManager _resourceManager = null;
        public ResourceManager ResourceManager
        {
            get
            {
                return _resourceManager;
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
            Assembly assembly = Assembly.GetExecutingAssembly();
            _resourceManager = new ResourceManager(CLConstants.kResourcesName, assembly);
            _mainDispatcher = Dispatcher.CurrentDispatcher;
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
            StartupUrlRelative = _resourceManager.GetString("startupUriPageInvisible");     // assume we will simply start running with no UI displayed
            if (isCloudAppAlreadyRunning())
            {
                // Tell the app.xaml.cs instance logic that we are already running.
                _isAlreadyRunning = true;
                StartupUrlRelative = _resourceManager.GetString("startupUriAlreadyRunning");
                return;
            }

            // Determine which window we will show on startup
            SetupCloudAppLaunchMode();
        }

        /// <summary>
        /// Call to release resources at application shutdown
        /// </summary>
        public void cleanUpAppDelegate()
        {
            _resourceManager = null;
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
        public bool UnlinkFromCloudDotCom(out CLError error)
        {
            // Merged 7/26/12
            //BOOL rc = YES;

            //// todo: tell cloud service to untrust this device and wait for confirmation.
            //CLRegistration *regstration = [[CLRegistration alloc] init];
            //rc = [regstration unlinkDeviceWithAccessKey:[[CLSettings sharedSettings] aKey]];
            

            //// stop services.
            //CLAppDelegate *delegate = [NSApp delegate];
            //[delegate stopCloudAppServicesAndUI];

            //// clean our settings
            //[[CLSettings sharedSettings] resetSettings];

            //[[CLCoreDataController defaultController] removeCoreDataStore];

            //// Stop the badging agent.  Do this synchronously because the system may be immediately exiting.
            //NSString *path = [[[NSBundle mainBundle] pathForResource:@"LICENSE" ofType:@""] stringByDeletingLastPathComponent];
            //NSString *cmd = [path stringByAppendingString:@"/CloudApp.bundle/CloudApp.app/Contents/MacOS/CloudApp -d"];
            //system([cmd UTF8String]);

            //return rc;
            //&&&& 

            //BOOL rc = YES;
            // Note: allocated below.

            //// todo: tell cloud service to untrust this device and wait for confirmation.
            //CLRegistration *regstration = [[CLRegistration alloc] init];
            //rc = [regstration unlinkDeviceWithAccessKey:[[CLSettings sharedSettings] aKey]];
            CLRegistration registration = new CLRegistration();
            bool rc = registration.UnlinkDeviceWithAccessKey(Settings.Instance.Akey, out error);

            //// stop services.
            //CLAppDelegate *delegate = [NSApp delegate];
            //[delegate stopCloudAppServicesAndUI];
            StopCloudAppServicesAndUI();

            //// clean our settings
            //[[CLSettings sharedSettings] resetSettings];
            Settings.Instance.resetSettings();

            //[[CLCoreDataController defaultController] removeCoreDataStore];
            //TODO: Is SQLIndexer terminated properly?

            //// Stop the badging agent.  Do this synchronously because the system may be immediately exiting.
            //NSString *path = [[[NSBundle mainBundle] pathForResource:@"LICENSE" ofType:@""] stringByDeletingLastPathComponent];
            //NSString *cmd = [path stringByAppendingString:@"/CloudApp.bundle/CloudApp.app/Contents/MacOS/CloudApp -d"];
            //system([cmd UTF8String]);
            //TODO: Stop the badging service.

            //return rc;
            return rc;
        }

        /// <summary>
        /// Perform one-time installation (cloud folder, and any OS support)
        /// </summary>
        //- (void)stopCloudAppServicesAndUI
        public void StopCloudAppServicesAndUI()
        {
            // Merged 7/20/12
            // // Remove the status item from the menu bar.
            // [[NSStatusBar systemStatusBar] removeStatusItem:[self.appController statusItem]];
            // [self.appController setStatusItem:nil];

            // // Stop core services
            // [[CLServicesManager sharedService] stopCoreServices];
            //&&&&

            // // Remove the status item from the menu bar.
            // [[NSStatusBar systemStatusBar] removeStatusItem:[self.appController statusItem]];
            // [self.appController setStatusItem:nil];
            // TODO: Remove the system tray icon?

            // // Stop core services
            // [[CLServicesManager sharedService] stopCoreServices];
            CLServicesManager.Instance.StopCoreServices();
        }

        /// <summary>
        /// Exit the application.
        /// </summary>
        public void ExitApplication()
        {
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

                // Start services, added a small delay to allow the OS to create folder.
                //TODO: We don't start the core services here because the user is still viewing the user
                // interface, and the PageInvisible page that has the system tray icon support is not
                // in place yet.  The user will view the tour (or skip it), and then PageInvisible
                // will be shown, and that will install the system tray support.  Is this the proper
                // design???
                //var dispatcher = CLAppDelegate.Instance.MainDispatcher; 
                //dispatcher.DelayedInvoke(TimeSpan.FromSeconds(2), () => { startCloudAppServicesAndUI(); });
            }
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
            // Merged 7/20//12
            // // here we have to halt. this means the folder has been deleted.
            // [self stopCloudAppServicesAndUI];
    
            // // show the repair window
            // self.folderMissingController = [[CLFolderMissingController alloc] initWithWindowNibName:@"CLFolderMissingController"];
            // [self.folderMissingController setCloudFolderPath:path];
            // [self.folderMissingController showWindow:self.folderMissingController.window];
            // [self.folderMissingController.window orderFrontRegardless];
            ///&&&&
            ///
            // // here we have to halt. this means the folder has been deleted.
            // [self stopCloudAppServicesAndUI];
            StopCloudAppServicesAndUI();

            // // show the repair window
            // self.folderMissingController = [[CLFolderMissingController alloc] initWithWindowNibName:@"CLFolderMissingController"];
            // [self.folderMissingController setCloudFolderPath:path];
            // [self.folderMissingController showWindow:self.folderMissingController.window];
            // [self.folderMissingController.window orderFrontRegardless];
            // Note: The window will be shown by App.xaml.cs using the StartupUri determined in this module.
        }

        void SetupCloudAppLaunchMode()
        {
            // Merged 7/19/12
            // // todo: here we should go back to our cloud and verify if
            // // device is still valid to help make the correct determination if we need to
            // // setup or not.

            // BOOL setupNeeded = YES;
            // BOOL shouldStartCoreServices = YES;

            // if ([[CLSettings sharedSettings] completedSetup]) {

            //     setupNeeded = NO;

            //     if ([[NSFileManager defaultManager] fileExistsAtPath:[[CLSettings sharedSettings] cloudFolderPath]] == NO) {

            //         NSData *bookmark = [[CLSettings sharedSettings] bookmarkDataForCloudFolder];
            //         NSURL *cloudFolderURL = [NSURL URLByResolvingBookmarkData:bookmark
            //                                                           options:NSURLBookmarkResolutionWithoutUI
            //                                                     relativeToURL:NULL
            //                                               bookmarkDataIsStale:NO
            //                                                             error:NULL];

            //         NSString *cloudFolderPath = [cloudFolderURL path];

            //         if (cloudFolderPath != nil) {

            //             if ([cloudFolderPath rangeOfString:@".Trash"].location != NSNotFound) { // folder got moved to trash, ask user to restore.

            //                 shouldStartCoreServices = NO;
            //                 [self cloudFolderHasBeenDeleted:cloudFolderPath];
            //             }
            //             else {

            //                 [[CLSettings sharedSettings] updateCloudFolderPath:cloudFolderPath]; // automatically pick up the new folder location.
            //             }
            //         }
            //         else {

            //             shouldStartCoreServices = NO;
            //             [self cloudFolderHasBeenDeleted:nil]; // we couldn't find the folder, ask the user to locate it. 
            //         }
            //     }
            // }

            // if (setupNeeded == YES) {

            //     // can't start our core services if we're not setup
            //     shouldStartCoreServices = NO;

            //     // welcome our new user
            //     self.welcomeController  = [[CLWelcomeWindowController alloc] initWithWindowNibName:@"CLWelcomeWindowController"];
            //     [self.welcomeController showWindow:self.welcomeController.window];
            //     [[self.welcomeController window] orderFrontRegardless];
            // }

            // if (shouldStartCoreServices == YES) {

            //     // business as usual, let's take this sync for a spin!
            //     [self startCloudAppServicesAndUI];
            // }
            //&&&&

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
                            StartupUrlRelative = _resourceManager.GetString("startupUriCloudFolderMissing");
                            FoundOriginalCloudFolderPath = foundOriginalPath;
                            FoundDeletedCloudFolderDeletionTimeLocal = foundDeletionTimeLocal;
                            FoundPathToDeletedCloudFolderRFile = foundPathToDeletedCloudFolderRFile;
                            FoundPathToDeletedCloudFolderIFile = foundPathToDeletedCloudFolderIFile;
                            PageCloudFolderMissingOkButtonContent = _resourceManager.GetString("pageCloudFolderMissingOkButtonRestore");
                        }
                        else
                        {
                            // We will put up a window in App.xaml.cs to allow the user to make a new cloud folder or unlink.
                            StartupUrlRelative = _resourceManager.GetString("startupUriCloudFolderMissing");
                            PageCloudFolderMissingOkButtonContent = _resourceManager.GetString("pageCloudFolderMissingOkButtonLocate");
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
                StartupUrlRelative = _resourceManager.GetString("startupUriFirstTimeSetup");
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
            //using (OleDbConnection conn = new OleDbConnection(
            //"Provider=Search.CollatorDSO;Extended Properties='Application=Windows';"))
            //{
            //    conn.Open();
            //    OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 System.ItemPathDisplay FROM SYSTEMINDEX WHERE " +
            //        "System.ItemType = 'Directory' AND System.DateCreated >= '2012-01-01 12:00:00' AND System.DateCreated < '2012-07-21 12:00:00'", conn);

            //    using (OleDbDataReader reader = cmd.ExecuteReader())
            //    {
            //        while (reader.Read())
            //        {
            //            List<object> row = new List<object>();

            //            for (int i = 0; i < reader.FieldCount; i++)
            //            {
            //                row.Add(reader[i]);
            //            }

            //        }
            //    }
            //}
            //&&&&
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
                    _trace.writeToLog(1, "CLAppDelegate: LocateMovedCloudFolder: ERROR: Exception.  Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                }
            }

            resultingPaths.RemoveAll(x => { return (x.LastPathComponent().Equals("Pictures", StringComparison.InvariantCulture) || x.LastPathComponent().Equals("Public", StringComparison.InvariantCulture)); });
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
            catch (Exception)
            {
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
                //window.Width = 640;
                //window.Height = 480;
                window.MinWidth = 640;
                window.MinHeight = 480;
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

