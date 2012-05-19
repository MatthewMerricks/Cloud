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
using win_client.Common;
using System.Windows;
using System.Resources;
using System.Reflection;

namespace win_client.AppDelegate
{

    /// <summary>
    /// Singleton class to represent application delegate support.
    /// </summary>
    public sealed class CLAppDelegate
    {
        #region "Life Cycle"
        /// <summary>
        /// Allocate ourselves. We have a private constructor, so no one else can.
        /// </summary>
        static readonly CLAppDelegate _instance = new CLAppDelegate();
        private static Boolean _isLoaded = false;
        private static CLTrace trace;
        private ResourceManager _resourceManager;

        public ResourceManager ResourceManager
        {
            get
            {
                return _resourceManager;
            }
        }

        /// <summary>
        /// Access SiteStructure.Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLAppDelegate Instance
        {
            get
            {
                if (!_isLoaded)
                {
                    _isLoaded = true;
                    _instance.initAppDelegate();
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
            trace = CLTrace.Instance;
        }
        
        /// <summary>
        /// Lazy initialization
        /// </summary>
        public void initAppDelegate()
        {
            Assembly assembly = GetType().Assembly;
            _resourceManager = new ResourceManager(CLConstants.kResourcesName, assembly);

            //// TODO: Needed? registers application to listen to url handling events.
            //[[NSAppleEventManager sharedAppleEventManager] setEventHandler:self andSelector:@selector(handleURLFromEvent:) forEventClass:kInternetEventClass andEventID:kAEGetURL];

            //// we only allow one instance of our app.
            //if ([self isCloudAppAlreadyRunning]) {

            //    NSLog(@"%s - Cloud.com app is already running.", __FUNCTION__);
            //    // show menu for currently running app
            //    [[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:@"cloud://ShowMenu"]]; 
            //    // exit this instance
            //    exit(0);
            //}

            //if ([self isFirstTimeSetupNeeded]) {

            //    // welcome our new user
            //    self.welcomeController  = [[CLWelcomeWindowController alloc] initWithWindowNibName:@"CLWelcomeWindowController"];
            //    [self.welcomeController showWindow:self.welcomeController.window];
            //    [[self.welcomeController window] orderFrontRegardless];

            //} else {

            //    [self startCloudAppServicesAndUI];
            //}

            //[self registerApplicationAsStatupItem];
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
        public bool isFirstTimeSetupNeeded()
        {
            bool needed = true;
            
            // TODO: here we should go back to our cloud and verify if
            // device is still valid to help make the correct determination if we need to
            // setup or not.

            if (Settings.Instance.CompletedSetup)
            {
            
                if (!Directory.Exists(Settings.Instance.CloudFolderPath))
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
        /// Unlink this device from Cloud.com
        /// </summary>
        
        bool unlinkFromCloudDotCom()
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

            // TODO: Stop core services
            //&&&&CLServicesManager.Instance.stopCoreServices();

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
                trace.writeToLog(1, "Cloud folder created at <{0}>.", Settings.Instance.CloudFolderPath);

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
        /// Start the app services and UI
        /// </summary>
        public void startCloudAppServicesAndUI()
        {
            bool debug = false;
            if (debug)
            {
                throw new NotImplementedException(@"TODO: Not implemented yet.");
            }
        }

        /// <summary>
        /// Perform one-time installation (cloud folder, and any OS support)
        /// </summary>
        public void createCloudFolder(string cloudFolderPath, out CLError error)
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
                err.errorDescriptionStringResourceKey = @"appDelegateExceptionCreatingFolder";
                err.errorCode = (int)CLError.ErrorCodes.Exception;
                err.errorInfo.Add(CLError.ErrorInfo_Exception, e);
                error = err;
                return;
            }
        }
        #endregion
    }
} 

