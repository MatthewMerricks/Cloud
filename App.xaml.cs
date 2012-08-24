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

namespace win_client
{
    public sealed partial class App : Application
    {

        static App()
        {

            DispatcherHelper.Initialize();

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

            MyNavigationWindow window = new MyNavigationWindow();
            window.ShowsNavigationUI = false;

            //    // Set the window to display, selected in CLAppDelegate.initAppDelegate.  
            //    ((App)Application.Current).StartupUri = new Uri(CLAppDelegate.Instance.StartupUrlRelative, UriKind.Relative);
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
        }
    }
}
