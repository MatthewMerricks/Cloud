﻿//
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
using BadgeNET;
using CloudApiPublic.Model;

namespace win_client
{
    public sealed partial class App : Application
    {
#if SILVERLIGHT 
        public App()
        {
            Startup += Application_Startup;
            Exit += Application_Exit;
            UnhandledException += Application_UnhandledException;

            InitializeComponent();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var trace = CLTrace.Instance;
            try
            {
                trace.writeToLog(0, "Cloud is starting...");

                LocalMessageReceiver receiver = new LocalMessageReceiver("singleinstance");
                receiver.Listen();
                RootVisual = new MainPage();
            }
            catch (ListenFailedException)
            {
                trace.writeToLog(0, "Cloud is already running.  Exiting.");
                return;
            }

            RootVisual = new MainPage();
            DispatcherHelper.Initialize();

            // Load and initilaize singleton classes
            var appDelegate = CLAppDelegate.Instance;
        }

        private void Application_Exit(object sender, EventArgs e)
        {
            var trace = CLTrace.Instance;
            trace.writeToLog(0, "Cloud is exiting...");
            trace.flush();

            ViewModelLocator.Cleanup();
            var appDelegate = CLAppDelegate.Instance;
            appDelegate.cleanUpAppDelegate();

        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // the browser's exception mechanism. On IE this will display it a yellow alert 
            // icon in the status bar and Firefox will display a script error.
            if(!System.Diagnostics.Debugger.IsAttached)
            {

                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled. 
                // For production applications this error handling should be replaced with something that will 
                // report the error to the website and stop the application.
                e.Handled = true;
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    ReportErrorToDOM(e);
                });
            }
        }
        private void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
        {
            try
            {
                string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
                errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", "\n");

                System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight Application " + errorMsg + "\");");
            }
            catch(Exception)
            {
            }
        }
#else  // WPF

        static App()
        {

            DispatcherHelper.Initialize();

        }

        public App()
        { }

        private void Application_Startup(object sender, StartupEventArgs e) 
        {
            CLAppDelegate app = CLAppDelegate.Instance;                 // fire up the singleton

            // Allows icon overlays to start receiving calls to SetOrRemoveBadge,
            // before the initial list is passed (via InitializeOrReplace)
            CLError error = IconOverlay.Initialize();
            if (error != null)
            {
                MessageBox.Show(String.Format("Error initializing Cloud shell integration. Message: {0}, Code: {1}.", error.errorDescription, error.errorCode), "Error.");
            }

            // Todo: add call to IconOverlay.Shutdown() when the application terminates/closes/receives unhandled exception

            // Choose the window to display.
            if (CLAppDelegate.Instance.IsAlreadyRunning)
            {
                ((App)Application.Current).StartupUri = new Uri("/Views/WindowCloudAlreadyRunning.xaml", UriKind.Relative);
            }
            else if (CLAppDelegate.Instance.IsFirstTimeSetupNeeded)
            {
                ((App)Application.Current).StartupUri = new Uri("NavigationWindow.xaml", UriKind.Relative);
            }
            else
            {
                ((App)Application.Current).StartupUri = new Uri("/Views/WindowInvisible.xaml", UriKind.Relative);
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e) 
        { 
            //TODO: Clean up...
        }

#endif  // end WPF
    }
}
