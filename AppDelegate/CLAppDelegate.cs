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
using win_client.DataModels.Settings;
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
using System.Security.Permissions;
using System.Windows.Media.Imaging;
using win_client.SystemTray.TrayIcon;
using win_client.ViewModels;
using win_client.Views;
using win_client.Services.ServicesManager;

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
            createCloudFolder(Settings.Instance.CloudFolderPath, out error);
            if (error == null)
            {
                // Set setup process completed
                _trace.writeToLog(1, "Cloud folder created at <{0}>.", Settings.Instance.CloudFolderPath);

                // Set setup process completed
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
        private void createCloudFolder(string cloudFolderPath, out CLError error)
        {
            error = null;

            try
            {
                if (!Directory.Exists(cloudFolderPath))
                {
                    Directory.CreateDirectory(cloudFolderPath);
                    Directory.CreateDirectory(cloudFolderPath + @"\Public");
                    Directory.CreateDirectory(cloudFolderPath + @"\Pictures");
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
                return;
            }
        }
        #endregion
    }
} 

