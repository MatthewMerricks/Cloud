// <copyright file="BasicSettingsPageViewModel.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.ViewModel
{
    using System.Windows.Input;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.Command;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.Services;

    /// <summary>
    /// View model for the basic setting view page
    /// </summary>
    public class BasicSettingsPageViewModel : PageViewModelBase
    {
        private string bindingName;
        private string configurationFilePath;

        private IFileSelectionService fileSelection;

        /// <summary>
        /// Initializes a new instance of the BasicSettingsPageViewModel class
        /// </summary>
        /// <param name="fileSelection">Service for selecting a file from the filesystem</param>
        public BasicSettingsPageViewModel(IFileSelectionService fileSelection)
        {
            this.fileSelection = fileSelection;
            this.SelectFileCommand = new RelayCommand(this.OnSelectFile);
        }

        /// <summary>
        /// Gets the view display name
        /// </summary>
        public override string DisplayName
        {
            get { return "Basic Settings"; }
        }

        /// <summary>
        /// Gets or sets the binding name
        /// </summary>
        public string BindingName
        {
            get { return this.bindingName; }
            set { SetValue(ref this.bindingName, value, () => this.BindingName); }
        }

        /// <summary>
        /// Gets or sets the configuration file path
        /// </summary>
        public string ConfigurationFilePath
        {
            get { return this.configurationFilePath; }
            set { SetValue(ref this.configurationFilePath, value, () => this.ConfigurationFilePath); }
        }

        /// <summary>
        /// Gets the command for selecting a file
        /// </summary>
        public ICommand SelectFileCommand
        {
            get;
            private set;
        }

        internal override bool IsValid()
        {
            return !string.IsNullOrEmpty(this.BindingName)
                && !string.IsNullOrEmpty(this.configurationFilePath);
        }

        private void OnSelectFile()
        {
            var file = this.fileSelection.SelectFile("Configuration Files (*.config)|*.config");
            this.ConfigurationFilePath = file;
        }
    }
}

