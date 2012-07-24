// <copyright file="WizardViewModel.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.Command;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.Configuration;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.Services;

    /// <summary>
    /// The main ViewModel class for the wizard.
    /// This class contains the various pages shown
    /// in the workflow and provides navigation
    /// between the pages.
    /// </summary>
    public class WizardViewModel : ObservableModel
    {
        private RelayCommand cancelCommand;
        private PageViewModelBase currentPage;
        private RelayCommand moveNextCommand;
        private RelayCommand movePreviousCommand;
        private ObservableCollection<PageViewModelBase> pages;
        private IFileSelectionService fileSelection;
        private ICertificateStoreService certificateStore;
        private interoperabilityWizard wizardModel;

        /// <summary>
        /// Initializes a new instance of the WizardViewModel class
        /// </summary>
        /// <param name="fileSelection">Service for selecting files</param>
        /// <param name="certificateStore">Service for selecting certificates</param>
        /// <param name="wizardModel">Wizard model read from the configuration file</param>
        /// <param name="bindingName">Binding name</param>
        /// <param name="fileName">Path to the configuration file</param>
        public WizardViewModel(
            IFileSelectionService fileSelection, 
            ICertificateStoreService certificateStore,
            interoperabilityWizard wizardModel, 
            string bindingName, 
            string fileName)
        {
            this.fileSelection = fileSelection;
            this.certificateStore = certificateStore;
            this.wizardModel = wizardModel;
            this.FileName = fileName;
            this.BindingName = bindingName;
            this.CurrentPage = this.Pages[0];
        }

        /// <summary>
        /// Raised when the wizard should be removed from the UI.
        /// </summary>
        public event EventHandler RequestClose;

        /// <summary>
        /// Gets the command which, when executed, cancels the order 
        /// and causes the Wizard to be removed from the user interface.
        /// </summary>
        public ICommand CancelCommand
        {
            get
            {
                if (this.cancelCommand == null)
                {
                    this.cancelCommand = new RelayCommand(() => this.CancelWizard());
                }

                return this.cancelCommand;
            }
        }

        /// <summary>
        /// Gets the path to the configuration file
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Gets the binding for the generated binding
        /// </summary>
        public string BindingName { get; private set; }

        /// <summary>
        /// Gets the command which, when executed, causes the CurrentPage 
        /// property to reference the previous page in the workflow.
        /// </summary>
        public ICommand MovePreviousCommand
        {
            get
            {
                if (this.movePreviousCommand == null)
                {
                    this.movePreviousCommand = new RelayCommand(
                        () => this.MoveToPreviousPage(),
                        () => this.CanMoveToPreviousPage);
                }

                return this.movePreviousCommand;
            }
        }

        /// <summary>
        /// Gets the command which, when executed, causes the CurrentPage 
        /// property to reference the next page in the workflow.  If the user
        /// is viewing the last page in the workflow, this causes the Wizard
        /// to finish and be removed from the user interface.
        /// </summary>
        public ICommand MoveNextCommand
        {
            get
            {
                if (this.moveNextCommand == null)
                {
                    this.moveNextCommand = new RelayCommand(
                        () => this.MoveToNextPage(),
                        () => this.CanMoveToNextPage);
                }

                return this.moveNextCommand;
            }
        }

        /// <summary>
        /// Gets the page ViewModel that the user is currently viewing.
        /// </summary>
        public PageViewModelBase CurrentPage
        {
            get
            {
                return this.currentPage;
            }

            private set
            {
                if (value == this.currentPage)
                {
                    return;
                }

                if (this.currentPage != null)
                {
                    this.currentPage.IsCurrentPage = false;
                }

                this.currentPage = value;

                if (this.currentPage != null)
                {
                    this.currentPage.IsCurrentPage = true;
                }

                this.OnPropertyChanged(() => this.CurrentPage);
                this.OnPropertyChanged(() => this.IsOnLastPage);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the user is currently viewing the last page 
        /// in the workflow.
        /// </summary>
        public bool IsOnLastPage
        {
            get { return this.CurrentPageIndex == this.Pages.Count - 1; }
        }

        /// <summary>
        /// Gets a read-only collection of all page ViewModels.
        /// </summary>
        public ObservableCollection<PageViewModelBase> Pages
        {
            get
            {
                if (this.pages == null)
                {
                    this.CreatePages();
                }

                return this.pages;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the user has clicked ok in the dialog
        /// </summary>
        public bool DialogResult { get; private set; }

        private bool CanMoveToPreviousPage
        {
            get { return 0 < this.CurrentPageIndex; }
        }

        private bool CanMoveToNextPage
        {
            get { return this.CurrentPage != null && this.CurrentPage.IsValid(); }
        }

        private int CurrentPageIndex
        {
            get
            {
                if (this.CurrentPage == null)
                {
                    return -1;
                }

                return this.Pages.IndexOf(this.CurrentPage);
            }
        }

        private void CancelWizard()
        {
            this.DialogResult = false;

            this.OnRequestClose();
        }

        private void MoveToNextPage()
        {
            if (this.CanMoveToNextPage)
            {
                if (this.CurrentPageIndex < this.Pages.Count - 1)
                {
                    this.CurrentPage = this.Pages[this.CurrentPageIndex + 1];
                }
                else
                {
                    this.DialogResult = true;
                    this.OnRequestClose();
                }
            }
        }

        private void MoveToPreviousPage()
        {
            if (this.CanMoveToPreviousPage)
            {
                this.CurrentPage = this.Pages[this.CurrentPageIndex - 1];
            }
        }

        private void CreatePages()
        {
            var tempPages = new List<PageViewModelBase>();

            if (string.IsNullOrEmpty(this.BindingName))
            {
                var basicSettings = new BasicSettingsPageViewModel(this.fileSelection);
                basicSettings.ConfigurationFilePath = this.FileName;

                tempPages.Add(basicSettings);
            }

            tempPages.Add(new ScenarioPageViewModel(
                this.wizardModel.scenario,
                (pagesToAdd) =>
                {
                    var pagesToRemove = this.pages.Where(p => p is GenericWizardPageViewModel ||
                        p is CertificatePageViewModel).ToList();

                    foreach (var pageToRemove in pagesToRemove)
                    {
                        this.pages.Remove(pageToRemove);
                    }

                    foreach (var pageToAdd in pagesToAdd)
                    {
                        this.pages.Add(pageToAdd);
                    }

                    this.pages.Add(new CertificatePageViewModel(this.certificateStore));

                    OnPropertyChanged(() => IsOnLastPage);
                }));

            this.pages = new ObservableCollection<PageViewModelBase>(tempPages);
        }

        private void OnRequestClose()
        {
            EventHandler handler = this.RequestClose;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
