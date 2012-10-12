//
//  ViewModelLocator.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocatorTemplate xmlns:vm="clr-namespace:win_client.ViewModels"
                                   x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"
*/

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using win_client.Model;
using win_client.Views;
using win_client.ViewModels;
using win_client.Common;
using win_client.Services.Notification;
using GalaSoft.MvvmLight.Messaging;
using Dialog.Abstractions.Wpf.Intefaces;
using Dialog.Implementors.Wpf.MVVM.Services;
using System.Windows;


namespace win_client.ViewModels
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// <para>
    /// Use the <strong>mvvmlocatorproperty</strong> snippet to add ViewModels
    /// to this locator.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            if(ViewModelBase.IsInDesignModeStatic)
            {
                SimpleIoc.Default.Register<IDataService, Design.DesignDataService>();
            }
            else
            {
                SimpleIoc.Default.Register<IDataService, DataService>();
            }

            // Navigation pages
            SimpleIoc.Default.Register<PageHomeViewModel>();
            SimpleIoc.Default.Register<PageCreateNewAccountViewModel>();
            SimpleIoc.Default.Register<PageSelectStorageSizeViewModel>();
            SimpleIoc.Default.Register<PageSetupSelectorViewModel>();
            SimpleIoc.Default.Register<PageTourViewModel>();
            SimpleIoc.Default.Register<PageBadgeComInitializationErrorViewModel>();
            SimpleIoc.Default.Register<PageCloudAlreadyRunningViewModel>();
            SimpleIoc.Default.Register<PageInvisibleViewModel>();
            SimpleIoc.Default.Register<PagePreferencesViewModel>();
            SimpleIoc.Default.Register<PageFolderSelectionViewModel>();
            SimpleIoc.Default.Register<PageTourAdvancedEndViewModel>();

            // Navigation frames
            SimpleIoc.Default.Register<FramePreferencesGeneralViewModel>();
            SimpleIoc.Default.Register<FramePreferencesShortcutsViewModel>();
            SimpleIoc.Default.Register<FramePreferencesAccountViewModel>();
            SimpleIoc.Default.Register<FramePreferencesNetworkViewModel>();
            SimpleIoc.Default.Register<FramePreferencesAdvancedViewModel>();
            SimpleIoc.Default.Register<FramePreferencesAboutViewModel>();

            // Window pages
            SimpleIoc.Default.Register<PageCloudFolderMissingViewModel>();
            SimpleIoc.Default.Register<WindowSyncStatusViewModel>();

            // Modal dialog support
            SimpleIoc.Default.Register<IModalDialogService, ModalDialogService>();
            SimpleIoc.Default.Register<IMessageBoxService, MessageBoxService>();

            // Modal dialogs
            SimpleIoc.Default.Register<IModalWindow>(() => new DialogCloudMessageBoxView(), CLConstants.kDialogBox_CloudMessageBoxView, false);
            SimpleIoc.Default.Register<IModalWindow>(() => new DialogPreferencesNetworkProxies(), CLConstants.kDialogBox_PreferencesNetworkProxies, false);
            SimpleIoc.Default.Register<IModalWindow>(() => new DialogPreferencesNetworkBandwidth(), CLConstants.kDialogBox_PreferencesNetworkBandwidth, false);

            // Modal dialog view models
            SimpleIoc.Default.Register<DialogCloudMessageBoxViewModel>();
            SimpleIoc.Default.Register<DialogPreferencesNetworkProxiesViewModel>();
            SimpleIoc.Default.Register<DialogPreferencesNetworkBandwidthViewModel>();
            SimpleIoc.Default.Register<DialogCheckForUpdatesViewModel>();

            // Growls
            SimpleIoc.Default.Register<FancyBalloon>();

            // EventMessageReceiver
            SimpleIoc.Default.Register<CloudApiPrivate.EventMessageReceiver.EventMessageReceiver>(() => CloudApiPrivate.EventMessageReceiver.EventMessageReceiver.Instance);
        }

        /// <summary>
        /// Gets the PageHomeViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PageHomeViewModel PageHomeViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PageHomeViewModel>();
            }
        }

        /// <summary>
        /// Gets the PageCreateNewAccountViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PageCreateNewAccountViewModel PageCreateNewAccountViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PageCreateNewAccountViewModel>();
            }
        }

        /// <summary>
        /// Gets the PageSelectStorageSizeViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PageSelectStorageSizeViewModel PageSelectStorageSizeViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PageSelectStorageSizeViewModel>();
            }
        }

        /// <summary>
        /// Gets the PageSetupSelectorViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PageSetupSelectorViewModel PageSetupSelectorViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PageSetupSelectorViewModel>();
            }
        }

        /// <summary>
        /// Gets the PageTourViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PageTourViewModel PageTourViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PageTourViewModel>();
            }
        }

        /// <summary>
        /// Gets the PageCloudAlreadyRunningViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PageCloudAlreadyRunningViewModel PageCloudAlreadyRunningViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PageCloudAlreadyRunningViewModel>();
            }
        }

        /// <summary>
        /// Gets the PageInvisibleViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PageInvisibleViewModel PageInvisibleViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PageInvisibleViewModel>();
            }
        }

        /// <summary>
        /// Gets the PagePreferencesViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PagePreferencesViewModel PagePreferencesViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PagePreferencesViewModel>();
            }
        }

        /// <summary>
        /// Gets the PageFolderSelectionViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PageFolderSelectionViewModel PageFolderSelectionViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PageFolderSelectionViewModel>();
            }
        }

        /// <summary>
        /// Gets the PageTourAdvancedEndViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PageTourAdvancedEndViewModel PageTourAdvancedEndViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PageTourAdvancedEndViewModel>();
            }
        }

        /// <summary>
        /// Gets the PageCloudFolderMissingViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PageCloudFolderMissingViewModel PageCloudFolderMissingViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PageCloudFolderMissingViewModel>();
            }
        }

        /// <summary>
        /// Gets the WindowSyncStatusViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public WindowSyncStatusViewModel WindowSyncStatusViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<WindowSyncStatusViewModel>();
            }
        }



        /// <summary>
        /// Gets the PageBadgeComInitializationErrorViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PageBadgeComInitializationErrorViewModel PageBadgeComInitializationErrorViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<PageBadgeComInitializationErrorViewModel>();
            }
        }

        /// <summary>
        /// Gets the FramePreferencesGeneralViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public FramePreferencesGeneralViewModel FramePreferencesGeneralViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FramePreferencesGeneralViewModel>();
            }
        }

        /// <summary>
        /// Gets the FramePreferencesShortcutsViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public FramePreferencesShortcutsViewModel FramePreferencesShortcutsViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FramePreferencesShortcutsViewModel>();
            }
        }

        /// <summary>
        /// Gets the FramePreferencesAccountViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public FramePreferencesAccountViewModel FramePreferencesAccountViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FramePreferencesAccountViewModel>();
            }
        }

        /// <summary>
        /// Gets the FramePreferencesNetworkViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public FramePreferencesNetworkViewModel FramePreferencesNetworkViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FramePreferencesNetworkViewModel>();
            }
        }

        /// <summary>
        /// Gets the FramePreferencesAdvancedViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public FramePreferencesAdvancedViewModel FramePreferencesAdvancedViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FramePreferencesAdvancedViewModel>();
            }
        }

        /// <summary>
        /// Gets the FramePreferencesAboutViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public FramePreferencesAboutViewModel FramePreferencesAboutViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FramePreferencesAboutViewModel>();
            }
        }

        /// <summary>
        /// Gets the ModalDialogService property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public ModalDialogService ModalDialogService
        {
            get
            {
                return ServiceLocator.Current.GetInstance<ModalDialogService>();
            }
        }

        /// <summary>
        /// Gets the MessageBoxService property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public MessageBoxService MessageBoxService
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MessageBoxService>();
            }
        }

        /// <summary>
        /// Gets the DialogCloudMessageBoxView property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public DialogCloudMessageBoxView DialogCloudMessageBoxView
        {
            get
            {
                return (DialogCloudMessageBoxView)ServiceLocator.Current.GetInstance<IModalWindow>(CLConstants.kDialogBox_CloudMessageBoxView);
            }
        }

        /// <summary>
        /// Gets the DialogPreferencesNetworkProxies property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public DialogPreferencesNetworkProxies DialogPreferencesNetworkProxies
        {
            get
            {
                return (DialogPreferencesNetworkProxies)ServiceLocator.Current.GetInstance<DialogPreferencesNetworkProxies>(CLConstants.kDialogBox_PreferencesNetworkProxies);
            }
        }

        /// <summary>
        /// Gets the DialogPreferencesNetworkBandwidth property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public DialogPreferencesNetworkBandwidth DialogPreferencesNetworkBandwidth
        {
            get
            {
                return (DialogPreferencesNetworkBandwidth)ServiceLocator.Current.GetInstance<DialogPreferencesNetworkBandwidth>(CLConstants.kDialogBox_PreferencesNetworkBandwidth);
            }
        }

        /// <summary>
        /// Gets the DialogCloudMessageBoxViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public DialogCloudMessageBoxViewModel DialogCloudMessageBoxViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<DialogCloudMessageBoxViewModel>();
            }
        }

        /// <summary>
        /// Gets the DialogPreferencesNetworkProxiesViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public DialogPreferencesNetworkProxiesViewModel DialogPreferencesNetworkProxiesViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<DialogPreferencesNetworkProxiesViewModel>();
            }
        }

        /// <summary>
        /// Gets the DialogPreferencesNetworkBandwidthViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public DialogPreferencesNetworkBandwidthViewModel DialogPreferencesNetworkBandwidthViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<DialogPreferencesNetworkBandwidthViewModel>();
            }
        }

        /// <summary>
        /// Gets the DialogCheckForUpdatesViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public DialogCheckForUpdatesViewModel DialogCheckForUpdatesViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<DialogCheckForUpdatesViewModel>();
            }
        }

        /// <summary>
        /// Gets the FancyBalloon property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public FancyBalloon FancyBalloon
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FancyBalloon>();
            }
        }

        /// <summary>
        /// Gets the EventMessageReceiver property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public CloudApiPrivate.EventMessageReceiver.EventMessageReceiver EventMessageReceiver
        {
            get
            {
                return ServiceLocator.Current.GetInstance<CloudApiPrivate.EventMessageReceiver.EventMessageReceiver>();
            }
        }

        /// <summary>
        /// Cleans up all the resources.
        /// </summary>
        public static void Cleanup()
        {
            //TODO: Clean up all of the view models.
        }
    }
}