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
//&&&&
using System.Data;
using System.Data.OleDb;
using CloudApiPublic.Static;
using CloudApiPrivate.Static;
using Microsoft.Win32;
using Shell32;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

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
        private static CLTrace _trace;
        private bool _isAlreadyRunning = false;
        private bool _isFirstTimeSetupNeeded = false;
        private ResourceManager _resourceManager = null;
        //&&&&private TrayIcon _trayIcon;
        #endregion
        #region Public Properties
        public bool IsAlreadyRunning
        {
            get
            {
                return _isAlreadyRunning;
            }
        }

        public bool IsFirstTimeSetupNeeded
        {
            get
            {
                return _isFirstTimeSetupNeeded;
            }
        }

        public ResourceManager ResourceManager
        {
            get
            {
                return _resourceManager;
            }
        }

        #endregion

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
            _trace = CLTrace.Instance;
            Assembly assembly = Assembly.GetExecutingAssembly();
            _resourceManager = new ResourceManager(CLConstants.kResourcesName, assembly);
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

        private string LocateCloudFolderInRecycleBin(DateTime cloudPathCreationTime)
        {
            Shell Shl = new Shell();
            Folder Recycler = Shl.NameSpace(10);
            //int countItems =

            List<RecyclerDetails> recyclerItems = new List<RecyclerDetails>();

            foreach (object currentItem in Recycler.Items())
            {
                FolderItem castItem = currentItem as FolderItem;
                if (castItem != null)
                {
                    recyclerItems.Add(new RecyclerDetails()
                    {
                        CreationDate = Recycler.GetDetailsOf(castItem,
                            RecyclerDetails.CreationDateIndex),
                        FileName = Recycler.GetDetailsOf(castItem,
                            RecyclerDetails.FileNameIndex),
                        FileParentDirectory = Recycler.GetDetailsOf(castItem,
                            RecyclerDetails.FileParentDirectoryIndex)
                    });
                }
            }

            DateTime cloudPathCreationTimePlusOneSecond = cloudPathCreationTime + TimeSpan.FromSeconds(1);

            foreach (RecyclerDetails folder in recyclerItems)
            {
                if (folder.FileName == "CloudX")
                {
                    int i = 0;
                    i++;
                }
            }

            return recyclerItems.Where(currentDetails => 
                {   
                    DateTime timeFromRecycleBin = DateTime.Parse(Helpers.CleanDateTimeString(currentDetails.CreationDate));
                    return timeFromRecycleBin >= cloudPathCreationTime && timeFromRecycleBin < cloudPathCreationTimePlusOneSecond;
                })
                .Select(currentDetails => Path.Combine(currentDetails.FileParentDirectory, currentDetails.FileName))
                .FirstOrDefault();
        }

        private static void EmptyRecycleBinX()
        {
            string recycleLocation = String.Empty;
            string strKeyPath = "SOFTWARE\\Microsoft\\Protected Storage System Provider";
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(strKeyPath);
            string[] arrSubKeys = regKey.GetSubKeyNames();
            if (IsVista() || IsWin7())    //Methods are described below
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
                string strTmpDrive = mgtObject["Name"].ToString();
                //if (bSIDExists == true)  // default is true
                foreach (string strSubKey in arrSubKeys)
                {
                    string regKeySID = strSubKey;
                    string recycleBinLocation = (strTmpDrive + "\\" + recycleLocation + "\\" + regKeySID + "\\");
                    if (recycleBinLocation != "" && Directory.Exists(recycleBinLocation))
                    {
                        DirectoryInfo recycleBin = new DirectoryInfo(recycleBinLocation);
                        // Clean Files
                        FileInfo[] recycleBinFiles = recycleBin.GetFiles();
                        foreach (FileInfo fileToClean in recycleBinFiles)
                        {
                            try
                            {
                                Console.WriteLine("File to delete: <{0}>.", fileToClean.ToString());
                                //fileToClean.Delete();
                            }
                            catch (Exception)
                            {
                                // Ignore exceptions and try to move next file
                            }
                        }
                        // Clean Folders
                        DirectoryInfo[] recycleBinFolders = recycleBin.GetDirectories();
                        foreach (DirectoryInfo folderToClean in recycleBinFolders)
                        {
                            try
                            {
                                Console.WriteLine("Folder to delete: <{0}>.", folderToClean.ToString());
                                folderToClean.Delete(true);
                            }
                            catch (Exception)
                            {
                                // Ignore exceptions and try to move next file
                            }
                        }
                        Console.WriteLine("Cleaned up location: {0}", recycleBinLocation);
                    }
                }
            }
        }

        /// <summary>
        /// Locate the cloud folder in the recycle bin via it's creation time.
        /// </summary>
        /// <param name="cloudFolderCreationTime">  The original cloud folder creation time</param>
        /// <param name="foundOriginalPath"> If found, the original path.</param>
        /// <<param name="foundDeletionTime"> If found, the deletion time.</param>
        /// <returns>bool: True: Cloud folder found.</returns>
        bool LocateCloudFolderInRecycleBin(DateTime cloudFolderCreationTime, out string foundOriginalPath, out DateTime foundDeletionTime)
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
                    string strTmpDrive = mgtObject["Name"].ToString();
                    //if (bSIDExists == true)  // default is true
                    foreach (string strSubKey in arrSubKeys)
                    {
                        string regKeySID = strSubKey;
                        string recycleBinLocation = (strTmpDrive + "\\" + recycleLocation + "\\" + regKeySID + "\\");
                        if (recycleBinLocation != "" && Directory.Exists(recycleBinLocation))
                        {
                            DirectoryInfo recycleBin = new DirectoryInfo(recycleBinLocation);

                            // Get the list of folders
                            DirectoryInfo[] recycleBinFolders = recycleBin.GetDirectories();
                            foreach (DirectoryInfo folder in recycleBinFolders)
                            {
                                if (folder.CreationTime == cloudFolderCreationTime)
                                {
                                    // This is our cloud folder.  Find the original path.  That is in the corresponding
                                    // $I file.  e.g., if the name of this $R file is $RABCDEF, then locate the file
                                    // with name $IABCDEF.  The deletion time is stored in 8 bytes at offset 0x10 in the
                                    /// $I file.  The Unicode original path string is stored at offset 0x18.  The
                                    // path string is a maximum of 260 characters (or 520 bytes), including the double
                                    // null termination.
                                    byte[] fileBytes = File.ReadAllBytes(recycleBinLocation + "\\$I" + folder.Name.Substring(2));

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

                                    DateTime deletionTime = new DateTime((long)ulTicks);

                                    foundOriginalPath = originalPath;
                                    foundDeletionTime = deletionTime;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                foundOriginalPath = null;
                foundDeletionTime = (DateTime)Helpers.DefaultForType(typeof(DateTime));
                return false;
            }

            foundOriginalPath = null;
            foundDeletionTime = (DateTime)Helpers.DefaultForType(typeof(DateTime));
            return false;
        }
        
        /// <summary>
        /// Lazy initialization
        /// </summary>
        [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        private void initAppDelegate()
        {
            //&&&&& TEST ONLY
            DateTime currentTime = DateTime.Now;
            //DateTime pastTime = currentTime - TimeSpan.FromDays(60);
            DateTime pastTime = Settings.Instance.CloudFolderCreationTime;
            DateTime pastTimePlusOneSecond = pastTime + TimeSpan.FromSeconds(1);
            string sPastTime = pastTime.ToString("yyyy-MM-dd HH:mm:ss");
            string sPastTimePlusOneSecond = pastTimePlusOneSecond.ToString("yyyy-MM-dd HH:mm:ss");
            string sSql = String.Format("SELECT System.ItemPathDisplay, System.ItemType, System.DateCreated FROM SYSTEMINDEX WHERE " + 
                                        "System.ItemType='Directory' AND System.DateCreated>='{0}' AND System.DateCreated<'{1}'", sPastTime, sPastTimePlusOneSecond);
            //AND System.DateCreated = '{0}'
            //AND System.ItemPathDisplay LIKE '%Cloud%' 

            List<string> resultingPaths = new List<string>();
            using (OleDbConnection objConnection = new OleDbConnection("Provider=Search.CollatorDSO;Extended Properties='Application=Windows';"))
            {
                OleDbCommand cmd = new OleDbCommand(sSql, objConnection);
                try
                {
                    objConnection.Open();
                    OleDbDataReader reader = cmd.ExecuteReader();
                    Console.WriteLine("Starting.......");
                    while (reader.Read())
                    {
                        Console.WriteLine(reader[0] + "," + reader[1] + "," + reader[2]);
                        resultingPaths.Add((string)reader[0]);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }

            resultingPaths.RemoveAll(x => { return (x.LastPathComponent().Equals("Pictures", StringComparison.InvariantCulture) || x.LastPathComponent().Equals("Public", StringComparison.InvariantCulture)); });

            // Now search the recycle bin.
            string foundOriginalPath;
            DateTime foundDeletionTime;
            bool foundDeletedCloudFolder = LocateCloudFolderInRecycleBin(pastTime, out foundOriginalPath, out foundDeletionTime);

            if (foundDeletedCloudFolder)
            {
                Console.WriteLine(String.Format("Found folder: Original path: <{0}>, DeletionTime: {1}.", foundOriginalPath, foundDeletionTime));
            }



            //// TODO: Needed? registers application to listen to url handling events.
            //[[NSAppleEventManager sharedAppleEventManager] setEventHandler:self andSelector:@selector(handleURLFromEvent:) forEventClass:kInternetEventClass andEventID:kAEGetURL];

            // we only allow one instance of our app.
            if (isCloudAppAlreadyRunning())
            {
                // Tell the app.xaml.cs instance logic that we are already running.
                _isAlreadyRunning = true;
                return;
            }

            if (isFirstTimeSetupNeeded())
            {
                // Tell the app.xaml.cs instance logic that we need the welcome dialog.
                _isFirstTimeSetupNeeded = true;

            }
            else {
                // DO NOTHING HERE.  The WindowInvisible will be loaded and the loaded event will invoke the
                // startCloudAppServicesAndUI() method on the UI thread after a small delay.  This insures
                // that the system tray icon support is in place before we initialize the services.
                //startCloudAppServicesAndUI();
            }
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
        /// Check to see whether we should put up the UI, or just run.
        /// </summary>
        private bool isFirstTimeSetupNeeded()
        {
            bool needed = true;
            
            // TODO: here we should go back to our cloud and verify if
            // device is still valid to help make the correct determination if we need to
            // setup or not.

            if (Settings.Instance.CompletedSetup)
            {
            
                if (Directory.Exists(Settings.Instance.CloudFolderPath))
                {
                    needed = false;
                }
                else
                {
                    // Oh Snap! - The user has deleted the cloud folder while the app wasn't running.
                    // Try to remove this device from Cloud.com, and reset all of the settings.
                    // TODO: The Mac client uses the old descriptor and asks osX for the associated
                    // path.  If it is found, that should be the new path of the same folder.  The
                    // user just moved it.  I don't think this is possible on Windows, but is there
                    // some way we could find the moved folder; moved while this program was not running?
                    needed = true;
                    unlinkFromCloudDotCom();
                }
            }
    
            return needed;
        }

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
        
        private bool unlinkFromCloudDotCom()
        {
            bool rc = true;
    
            // TODO: tell cloud service to untrust this device and wait for confirmation.
            //&&&&CLRegistration *registration = new CLRegistration();
            //&&&&rc = registration.unlinkDeviceWithAccessKey(Settings.Instance.Akey);
    
            // stop services.
            stopCloudAppServicesAndUI();
    
            // clean our settings
            Settings.Instance.resetSettings();
    
            return rc;
        
        }

        /// <summary>
        /// Perform one-time installation (cloud folder, and any OS support)
        /// </summary>
        public void stopCloudAppServicesAndUI()
        {
            // TODO: Remove all of the OS integration

            // Stop core services
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
        /// Perform one-time installation (cloud folder, and any OS support)
        /// </summary>
        public void installCloudServices(out CLError error)
        {
            error = null;

            // TODO: Install any OS integration support (badging, etc.)
            // Set error to null or error information.

            // Create the cloud folder
            DateTime creationTime;
            createCloudFolder(Settings.Instance.CloudFolderPath, out creationTime, out error);
            if (error == null)
            {
                // Set setup process completed
                _trace.writeToLog(1, "Cloud folder created at <{0}>.", Settings.Instance.CloudFolderPath);

                // Set setup process completed
                Settings.Instance.CloudFolderCreationTime = creationTime;
                Settings.Instance.setCloudAppSetupCompleted(true);

                // Start services, added a small delay to allow the OS to create folder.
#if SILVERLIGHT 
                var dispatcher = Deployment.Current.Dispatcher; 
#else 
                var dispatcher = Dispatcher.CurrentDispatcher; 
#endif              
                dispatcher.DelayedInvoke(TimeSpan.FromSeconds(2), () => { startCloudAppServicesAndUI(); });
            }
        }

        /// <summary>
        /// We will go onto the system tray.  Start the app services and UI
        /// </summary>
        public void startCloudAppServicesAndUI()
        {
            // We might not have an application main window.  However, we need one for the
            // system tray support.
            //&&&&&Window mainWindow = Application.Current.MainWindow;
            //&&&&&if (mainWindow == null)
            //&&&&&{
            //&&&&&    mainWindow = new WindowInvisibleView();
            //&&&&&    mainWindow.Show();
            //&&&&&}

            // Create the system tray icon
            //&&&&_trayIcon = new TrayIcon(mainWindow);
            //&&&&_trayIcon.Show(global::win_client.Resources.Resources.SystemTrayIcon, "Cloud");

            // Start core services
            CLServicesManager.Instance.StartCoreServices();
        }

        /// <summary>
        /// Perform one-time installation (cloud folder, and any OS support)
        /// </summary>
        private void createCloudFolder(string cloudFolderPath, out DateTime creationTime, out CLError error)
        {
            error = null;

            try
            {
                if (!Directory.Exists(cloudFolderPath))
                {
                    Directory.CreateDirectory(cloudFolderPath);
                    Directory.CreateDirectory(cloudFolderPath + @"\Public");
                    Directory.CreateDirectory(cloudFolderPath + @"\Pictures");
                    creationTime = Directory.GetCreationTime(cloudFolderPath);
                }
                else
                {
                    creationTime = (DateTime)Helpers.DefaultForType(typeof(DateTime));
                }

                // TODO: Assign our own icon to the newly created Cloud folder
                // TODO: Assign our own icon to the newly created Cloud\Public folder
                // TODO: Assign our own icon to the newly created Cloud\Pictures folder
                // TODO: Set a shortcut to the Cloud folder into Explorer toolbar
                // TODO: Set a shortcut to the Cloud folder onto the Desktop.
                // TODO: Add our Cloud app menu and icon to the System Tray.  Set it to be always visible.
            }
            catch (Exception e)
            {
                CLError err = new CLError();
                err.errorDomain = CLError.ErrorDomain_Application;
                err.errorDescription = CLSptResourceManager.Instance.ResMgr.GetString("appDelegateExceptionCreatingFolder");
                err.errorCode = (int)CLError.ErrorCodes.Exception;
                err.errorInfo = new Dictionary<string,object>();
                err.errorInfo.Add(CLError.ErrorInfo_Exception, e);
                error = err;
                creationTime = (DateTime)Helpers.DefaultForType(typeof(DateTime));
                return;
            }
        }

        /// <summary>
        /// Get the application trace instance
        /// </summary>
        public CLTrace GetTrace()
        {
            return _trace;
        }

        //- (void)setupCloudAppLaunchMode
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
                }
            }

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

        }

        #endregion
    }
} 

