//
//  App.xaml.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Windows;
using GalaSoft.MvvmLight.Threading;
using win_client.ViewModels;
using win_client.AppDelegate;
using win_client.Common;
using Cloud.BadgeNET;
using Cloud.Model;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Static;
using Cloud.Support;
using Cloud.Static;
using System.Windows.Media.Imaging;
using System.IO;
using win_client.Services.ServicesManager;
using win_client.Services.FileSystemMonitoring;
using System.Diagnostics;
using System.Threading;

namespace win_client
{
    public sealed partial class App : Application
    {
        private CLTrace _trace = CLTrace.Instance;
        static App()
        {
            DispatcherHelper.Initialize();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args != null && e.Args.Length == 2)
            {
                if (e.Args[0] == "SetCloudLogging")
                {
                    int loggingLevel;
                    if (int.TryParse(e.Args[1], out loggingLevel))
                    {
                        TraceType toSet = IntToTraceType(loggingLevel);

                        Settings.Instance.TraceType = toSet;

                        Settings.Instance.LogErrors = (toSet != TraceType.NotEnabled);

                        this.Shutdown(0);
                    }
                }
            }
            else
            {
                try
                {
                    string enableTraceIni = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.None) + "\\Cloud\\EnableTrace.ini";

                    if (File.Exists(enableTraceIni))
                    {
                        string readIni = File.ReadAllText(enableTraceIni);
                        int readValue;
                        if (!string.IsNullOrWhiteSpace(readIni)
                            && int.TryParse(readIni, out readValue))
                        {
                            TraceType toSet = IntToTraceType(readValue);

                            Settings.Instance.TraceType = toSet;

                            Settings.Instance.LogErrors = (toSet != TraceType.NotEnabled);
                        }
                    }
                }
                catch
                {
                }
            }

            // Read the trace level for the Cloud trace.
            try
            {
                string traceLevelFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.None) + "\\Cloud\\CloudTraceLevel.ini";

                if (File.Exists(traceLevelFilePath))
                {
                    string readIni = File.ReadAllText(traceLevelFilePath);
                    int readValue;
                    if (!string.IsNullOrWhiteSpace(readIni)
                        && int.TryParse(readIni, out readValue))
                    {
                        Settings.Instance.TraceLevel = readValue;
                    }
                }
            }
            catch
            {
            }

            // Initialize the Cloud tracing.
            CLTrace.Initialize(TraceLocation: Settings.Instance.TraceType != TraceType.NotEnabled ? Settings.Instance.TraceLocation : null, 
                TraceCategory: "Cloud", FileExtensionWithoutPeriod: "log", TraceLevel: Settings.Instance.TraceLevel, LogErrors: Settings.Instance.LogErrors);

            // Change the Cloud folder location if we have just been restarted from the CloudMoveCloudFolder.vbs VBScript.
            lock (Settings.Instance.MovingCloudFolderTargetPath)
            {
                if (Settings.Instance.IsMovingCloudFolder)
                {
                    try
                    {
                        // Clear the flag so we do this only once
                        _trace.writeToLog(9, "App.xaml: OnStartup: Moving the cloud folder on restart.");
                        Settings.Instance.IsMovingCloudFolder = false;

                        // Get the directory creation time of the new cloud folder.
                        DateTime creationTime = Directory.GetCreationTimeUtc(Settings.Instance.MovingCloudFolderTargetPath);

                        // Update the cloud folder location.
                        Settings.Instance.updateCloudFolderPath(Settings.Instance.MovingCloudFolderTargetPath, creationTime);

                        // Clear the target cloud folder location too.
                        Settings.Instance.MovingCloudFolderTargetPath = String.Empty;

                        // Wipe the index to cause a re-index.
                        _trace.writeToLog(9, "App.xaml: OnStartup: Wipe the index to cause a re-index.");
                        long syncCounter;
                        CLFSMonitoringService.Instance.Syncbox.RecordCompletedSync(null, new long[0], out syncCounter);
                    }
                    catch (Exception ex)
                    {
                        CLError error = ex;
                        error.LogErrors(Settings.Instance.TraceLocation, Settings.Instance.LogErrors);
                        _trace.writeToLog(1, "App.xaml: OnStartup: ERROR: Exception.  Msg: <{0}>.", ex.Message);
                        MessageBox.Show(String.Format("Error starting the Cloud application. Startup exception: <{0}>.", ex.Message), "Oh Snap!", MessageBoxButton.OK);
                        this.Shutdown(0);
                    }
                }
            }

            base.OnStartup(e);
        }

        private static TraceType IntToTraceType(int readValue)
        {
            TraceType setType = TraceType.NotEnabled;

            if ((readValue & (int)TraceType.AddAuthorization) == (int)TraceType.AddAuthorization)
            {
                setType |= TraceType.Communication;
                setType |= TraceType.AddAuthorization;
            }
            else if ((readValue & (int)TraceType.Communication) == (int)TraceType.Communication)
            {
                setType |= TraceType.Communication;
            }

            if ((readValue & (int)TraceType.FileChangeFlow) == (int)TraceType.FileChangeFlow)
            {
                setType |= TraceType.FileChangeFlow;
            }

            return setType;
        }

        public App()
        { }

        private void Application_Startup(object sender, StartupEventArgs e) 
        {
            // Break into the assembler
            //System.Diagnostics.Debugger.Launch();
            // throw (new System.Exception()); 

            _trace.writeToLog(1, "App.xaml: Application_Startup: Starting...");
            CLAppDelegate app = CLAppDelegate.Instance;                 // fire up the singleton

            // Instantiate a new window
            MyNavigationWindow window = new MyNavigationWindow();

            // Set the window's icon
            _trace.writeToLog(1, "App.xaml: Application_Startup: Set the main window icon.");
            window.Icon = BitmapFrame.Create(GetResourceStream(new Uri("/Cloud;component/Artwork/Cloud.ico", UriKind.Relative)).Stream);
            window.ShowsNavigationUI = false;

            // Set the window to display, selected in CLAppDelegate.initAppDelegate.  
            // ((App)Application.Current).StartupUri = new Uri(CLAppDelegate.Instance.StartupUrlRelative, UriKind.Relative);
            _trace.writeToLog(1, "App.xaml: Application_Startup: Set the window to show.");
            window.Source = new Uri(CLAppDelegate.Instance.StartupUrlRelative, UriKind.Relative);

            // If we are running PageInvisible as the first page in this NavigationWindow, set a flag
            // to prevent the animation of the window into the system tray.
            _trace.writeToLog(1, "App.xaml: Application_Startup: Start window {0}.", window.Source.OriginalString);
            if (window.Source.OriginalString.Contains("PageInvisible"))
            {
                window.firstMinimize = true;
            }

            // Show the window
            this.MainWindow = window;
            CLAppDelegate.Instance.AppMainWindow = window;
            window.Topmost = true;
            window.Show();
            window.Topmost = false;
            _trace.writeToLog(1, "App.xaml: Application_Startup: Exit.");
        }

        private void Application_Exit(object sender, ExitEventArgs e) 
        { 
            //TODO: More cleanup??
            _trace.writeToLog(1, "App.xaml: Application_Exit: Entry.");
            CLServicesManager.Instance.StopCoreServices();
            _trace.writeToLog(1, "App.xaml: Application_Exit: Exit.");
        }

        /// <summary>
        /// Attempt to catch and trace all application unhandled exceptions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                e.Handled = true;
                _trace.writeToLog(1, "App.xaml: Application_DispatcherUnhandledException: ERROR: Exception: Msg: <{0}>.", e.Exception.Message);
                ((CLError)e.Exception).LogErrors(_trace.TraceLocation, _trace.LogErrors);
            }
            catch
            {
                try
                {
                    _trace.writeToLog(1, "App.xaml: Application_DispatcherUnhandledException: ERROR: Exception within the exception.");
                }
                catch
                {
                }
            }
        }
    }
}