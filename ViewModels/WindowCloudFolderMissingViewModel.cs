//
//  WindowCloudFolderMissingViewModel.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using GalaSoft.MvvmLight;
using win_client.Model;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using MVVMProductsDemo.ViewModels;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;
using win_client.Common;
using System.Reflection;
using System.Linq;
using CloudApiPrivate.Model.Settings;
using System.IO;
using System.Resources;
using GalaSoft.MvvmLight.Ioc;
using Dialog.Abstractions.Wpf.Intefaces;
using System.Collections.Generic;
using win_client.Views;
using win_client.AppDelegate;
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using win_client.ViewModelHelpers;
using Ookii.Dialogs.Wpf;


namespace win_client.ViewModels
{
         
    /// <summary>
    /// Page to control the multiple pages of the tour.
    /// </summary>
    public class WindowCloudFolderMissingViewModel : ValidatingViewModelBase, ICleanup
    {

        #region Instance Variables

        private readonly IDataService _dataService;
        private ResourceManager _rm;

        #endregion
        

        #region Bound Properties

        /// <summary>
        /// The <see cref="BodyMessagee" /> property's name.
        /// </summary>
        public const string BodyMessagePropertyName = "BodyMessage";
        private string _bodyMessage = "";

        /// <summary>
        /// Sets and gets the BodyMessage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string BodyMessage
        {
            get
            {
                return _bodyMessage;
            }

            set
            {
                if (_bodyMessage == value)
                {
                    return;
                }

                _bodyMessage = value;
                RaisePropertyChanged(BodyMessagePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="OkButtonContent" /> property's name.
        /// </summary>
        public const string OkButtonContentPropertyName = "OkButtonContent";
        private string _okButtonContent = "";

        /// <summary>
        /// Sets and gets the OkButtonContent property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string OkButtonContent
        {
            get
            {
                return _okButtonContent;
            }

            set
            {
                if (_okButtonContent == value)
                {
                    return;
                }

                _okButtonContent = value;
                RaisePropertyChanged(OkButtonContentPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ViewGridContainer" /> property's name.
        /// </summary>
        public const string ViewGridContainerPropertyName = "ViewGridContainer";
        private Grid _viewGridContainer = null;
        /// <summary>
        /// Sets and gets the ViewGridContainer property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Grid ViewGridContainer
        {
            get
            {
                return _viewGridContainer;
            }

            set
            {
                if (_viewGridContainer == value)
                {
                    return;
                }

                _viewGridContainer = value;
                RaisePropertyChanged(ViewGridContainerPropertyName);
            }
        }
        
        #endregion

        #region Life Cycle
        /// <summary>
        /// Initializes a new instance of the PageHomeViewModel class.
        /// </summary>
        public WindowCloudFolderMissingViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _dataService.GetData(
                (item, error) =>
                {
                    if (error != null)
                    {
                        // Report error here
                        return;
                    }

                    //&&&&               WelcomeTitle = item.Title;
                });
            _rm = CLAppDelegate.Instance.ResourceManager;
            BodyMessage = _rm.GetString("windowCloudFolderBodyMesssage");
            OkButtonContent = CLAppDelegate.Instance.WindowCloudFolderMissingOkButtonContent;
        }

        /// <summary>
        /// Clean up all resources allocated, and save state as needed.
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
            _rm = null;
        }

        #endregion
     
        #region Commands

        /// <summary>
        /// The user clicked the OK button.
        /// The button will read "Restore" or "Locate...".  Restore occurs
        /// when the missing cloud folder is found in the recycle bin.  Locate...
        /// occurs when we don't find the renamed directory, and we don't find
        /// the missing folder in the recycle bin.  Locate... will put up the folder
        /// selection dialog to allow the user to select a folder in which to make
        /// a new Cloud folder.  The Cloud folder will be constructed in the folder
        /// selected by the user.  If the user cancels the folder selection dialog,
        /// he will be left on this WindowCloudFolderMissing dialog in the same state.
        /// </summary>
        private RelayCommand _windowCloudFolderMissingViewModel_OkCommand;
        public RelayCommand WindowCloudFolderMissingViewModel_OkCommand
        {
            get
            {
                return _windowCloudFolderMissingViewModel_OkCommand
                    ?? (_windowCloudFolderMissingViewModel_OkCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Process the OK button click.
                                                if (this.OkButtonContent.Equals(_rm.GetString("windowCloudFolderMissingOkButtonLocate"), StringComparison.InvariantCulture))
                                                {
                                                    // This is the Locate... case.  Display the Windows Forms folder selection
                                                    // dialog.
                                                    VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog();
                                                    folderBrowser.Description = _rm.GetString("windowCloudFolderMissingFolderBrowserDescription");
                                                    folderBrowser.RootFolder = Environment.SpecialFolder.MyDocuments;  // no way to get to the user's home directory.
                                                    folderBrowser.ShowNewFolderButton = true;
                                                    folderBrowser.ShowDialog(this);
                                                    // NOTE:  This must be a message to the view, and the view will set 

#if TRASH
                                                    ; this must be asynchronous processing
                                                    display the folder selection dialog, starting in the user's home directory.
                                                    if user clicked OK
                                                      get the path of the selected folder
                                                      create the Cloud directory in the selected folder
                                                      if error creating the cloud directory
                                                        display the error message in a modal dialog
                                                        leave the user on this window when the user clicks OK on the error message modal dialog
                                                      else no error creating the cloud directory
                                                        naviagate to WindowInvisible.xaml.  That will start the Cloud app.
                                                      endelse no error creating the cloud directory
                                                    else user clicked cancel
                                                      ; leave the user on this window dialog
                                                    endelse user clicked cancel
#endif  // TRASH
                                                }
                                                else if (this.OkButtonContent.Equals(_rm.GetString("windowCloudFolderMissingOkButtonRestore"), StringComparison.InvariantCulture))
                                                {
                                                    // This is the Restore case.  Restore the cloud folder from the recycle bin.
                                                    CLError error = null;
                                                    RestoreCloudFolderFromRecycleBin(out error);
                                                    if (error == null)
                                                    {
                                                        // Tell this window (view) to close.
                                                        CLAppMessages.Message_WindowCloudFolderMissingShoudClose.Send("");

                                                        // Navigate to WindowInvisible.  That window will start the core services.
                                                        WindowInvisibleView nextWindow = new WindowInvisibleView();
                                                        nextWindow.Show();
                                                    }
                                                    else
                                                    {
                                                        // Display the error message in a modal dialog
                                                        // Leave the user on this dialog when the user clicks OK on the error message modal dialog
                                                        CLModalErrorDialog.Instance.DisplayModalErrorMessage(
                                                                error.errorDescription, 
                                                                _rm.GetString("windowCloudFolderMissingErrorTitle"),
                                                                _rm.GetString("windowCloudFolderMissingErrorHeader"),
                                                                _rm.GetString("windowCloudFolderMissingErrorRightButtonContent"),
                                                                this.ViewGridContainer, returnedViewModelInstance =>
                                                                {
                                                                    // If the cloud folder actually exists at the new location, then we
                                                                    // were successful at moving it, even if an error was thrown.  In this
                                                                    // case, we will just use it and continue starting core services.
                                                                    // Otherwise, we will leave the user on this dialog, but change
                                                                    // the OK button to "Locate...".
                                                                    if (Directory.Exists(Settings.Instance.CloudFolderPath))
                                                                    {
                                                                        // Tell this window (view) to close.
                                                                        CLAppMessages.Message_WindowCloudFolderMissingShoudClose.Send("");

                                                                        // Navigate to WindowInvisible.  That window will start the core services.
                                                                        WindowInvisibleView nextWindow = new WindowInvisibleView();
                                                                        nextWindow.Show();
                                                                    }
                                                                    else
                                                                    {
                                                                        // Just leave the user on this same WindowCloudFolderMissing window,
                                                                        // but change the OK button to Locate... since we had trouble
                                                                        // restoring the folder from the recycle bin.
                                                                        this.OkButtonContent = _rm.GetString("windowCloudFolderMissingOkButtonLocate");
                                                                    }
                                                                });
                                                    }
                                                }
                                            }));                                              
            }
        }

        /// <summary>
        /// Move the cloud folder from the recycle bin back to its original location.
        /// </summary>
        private void RestoreCloudFolderFromRecycleBin(out CLError error)
        {
            try
            {
                // Move the directory and delete the recycle bin info file.
                Directory.Move(CLAppDelegate.Instance.FoundPathToDeletedCloudFolderRFile, Settings.Instance.CloudFolderPath);
                File.Delete(CLAppDelegate.Instance.FoundPathToDeletedCloudFolderIFile);
            }
            catch (Exception ex)
            {
                error = ex;
                return;
            }
            error = null;
        }

        /// <summary>
        /// The user clicked the OK button.
        /// </summary>
        private RelayCommand _windowCloudFolderMissingViewModel_UnlinkCommand;
        public RelayCommand WindowCloudFolderMissingViewModel_UnlinkCommand
        {
            get
            {
                return _windowCloudFolderMissingViewModel_UnlinkCommand
                    ?? (_windowCloudFolderMissingViewModel_UnlinkCommand = new RelayCommand(
                                            () =>
                                            {
                                                // Process the Remove button click.
                                                // We will unlink this device.  Stop all core services and exit the application
                                                CLError error = null;
                                                CLAppDelegate.Instance.UnlinkFromCloudDotCom(out error);
                                                if (error != null)
                                                {
                                                    CLModalErrorDialog.Instance.DisplayModalErrorMessage(
                                                           error.errorDescription, 
                                                           _rm.GetString("windowCloudFolderMissingErrorTitle"),
                                                           _rm.GetString("windowCloudFolderMissingErrorHeader"),
                                                           _rm.GetString("windowCloudFolderMissingErrorRightButtonContent"),
                                                           this.ViewGridContainer, returnedViewModelInstance =>
                                                           {
                                                               // Exit the app when the user clicks the OK button.
                                                               Application.Current.Shutdown();
                                                           });
                                                }
                                            }));                                              
            }
        }

        #endregion
    }
}