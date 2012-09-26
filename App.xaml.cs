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
using BadgeNET;
using CloudApiPublic.Model;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Static;
using CloudApiPublic.Support;
using CloudApiPublic.Static;
using System.Windows.Media.Imaging;

namespace win_client
{
    public sealed partial class App : Application
    {

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
                        TraceType setType = TraceType.NotEnabled;

                        if ((loggingLevel & (int)TraceType.AddAuthorization) == (int)TraceType.AddAuthorization)
                        {
                            setType &= ~TraceType.Communication;
                            setType &= ~TraceType.AddAuthorization;
                        }
                        else if ((loggingLevel & (int)TraceType.Communication) == (int)TraceType.Communication)
                        {
                            setType &= ~TraceType.Communication;
                        }

                        if ((loggingLevel & (int)TraceType.FileChangeFlow) == (int)TraceType.FileChangeFlow)
                        {
                            setType &= ~TraceType.FileChangeFlow;
                        }

                        Settings.Instance.TraceType = setType;

                        Settings.Instance.LogErrors = (setType != TraceType.NotEnabled);

                        this.Shutdown(0);
                    }
                }
            }

            base.OnStartup(e);
        }

        public App()
        { }

        private ViewModelLocator _vm = new ViewModelLocator();

        private void Application_Startup(object sender, StartupEventArgs e) 
        {
            // Break into the assembler
            //System.Diagnostics.Debugger.Launch();
            // throw (new System.Exception()); 

            CLAppDelegate app = CLAppDelegate.Instance;                 // fire up the singleton

            // Instantiate a new window
            MyNavigationWindow window = new MyNavigationWindow();

            // Set the window's icon
            window.Icon = BitmapFrame.Create(GetResourceStream(new Uri("/Cloud;component/Artwork/Cloud.ico", UriKind.Relative)).Stream);
            window.ShowsNavigationUI = false;

            // Set the window to display, selected in CLAppDelegate.initAppDelegate.  
            // ((App)Application.Current).StartupUri = new Uri(CLAppDelegate.Instance.StartupUrlRelative, UriKind.Relative);
            window.Source = new Uri(CLAppDelegate.Instance.StartupUrlRelative, UriKind.Relative);

            // If we are running PageInvisible as the first page in this NavigationWindow, set a flag
            // to prevent the animation of the window into the system tray.
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
        }

        private void Application_Exit(object sender, ExitEventArgs e) 
        { 
            //TODO: Clean up...
            IconOverlay.Shutdown();
            DelayProcessable<FileChange>.TerminateAllProcessing();
            Sync.Sync.Shutdown();
        }
    }
}
