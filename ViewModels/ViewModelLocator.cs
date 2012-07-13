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

            // Window pages
            SimpleIoc.Default.Register<CloudAlreadyRunningViewModel>();
            SimpleIoc.Default.Register<WindowInvisibleViewModel>();
            SimpleIoc.Default.Register<BadgeComInitializationErrorViewModel>();

            // Modal dialog support
            SimpleIoc.Default.Register<IModalDialogService, ModalDialogService>();
            SimpleIoc.Default.Register<IMessageBoxService, MessageBoxService>();

            // Modal dialogs
            SimpleIoc.Default.Register<IModalWindow>( () => new CloudMessageBoxView(), CLConstants.kDialogBox_CloudMessageBoxView, false);
            SimpleIoc.Default.Register<IModalWindow>( () => new FolderSelectionSimpleView(), CLConstants.kDialogBox_FolderSelectionSimpleView, false);

            // Modal dialog view models
            SimpleIoc.Default.Register<CloudMessageBoxViewModel>();
            SimpleIoc.Default.Register<FolderSelectionSimpleViewModel>();

            // Growls
            SimpleIoc.Default.Register<FancyBalloon>();

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
        /// Gets the CloudAlreadyRunningViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public CloudAlreadyRunningViewModel CloudAlreadyRunningViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<CloudAlreadyRunningViewModel>();
            }
        }

        /// <summary>
        /// Gets the WindowInvisibleViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public WindowInvisibleViewModel WindowInvisibleViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<WindowInvisibleViewModel>();
            }
        }

        /// <summary>
        /// Gets the BadgeComInitializationErrorViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public BadgeComInitializationErrorViewModel BadgeComInitializationErrorViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<BadgeComInitializationErrorViewModel>();
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
        /// Gets the CloudMessageBoxView property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public CloudMessageBoxView CloudMessageBoxView
        {
            get
            {
                return (CloudMessageBoxView)ServiceLocator.Current.GetInstance<IModalWindow>(CLConstants.kDialogBox_CloudMessageBoxView);
            }
        }

        /// <summary>
        /// Gets the FolderSelectionSimpleView property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public FolderSelectionSimpleView FolderSelectionSimpleView
        {
            get
            {
                return (FolderSelectionSimpleView)ServiceLocator.Current.GetInstance<FolderSelectionSimpleView>(CLConstants.kDialogBox_FolderSelectionSimpleView);
            }
        }

        /// <summary>
        /// Gets the CloudMessageBoxViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public CloudMessageBoxViewModel CloudMessageBoxViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<CloudMessageBoxViewModel>();
            }
        }

        /// <summary>
        /// Gets the FolderSelectionSimpleViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public FolderSelectionSimpleViewModel FolderSelectionSimpleViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FolderSelectionSimpleViewModel>();
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
        /// Cleans up all the resources.
        /// </summary>
        public static void Cleanup()
        {
            //TODO: Clean up all of the view models.
        }
    }
}