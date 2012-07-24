// <copyright file="PageViewModelBase.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.ViewModel
{
    /// <summary>
    /// Abstract base class for all pages shown in the wizard.
    /// </summary>
    public abstract class PageViewModelBase : ObservableModel
    {
        private bool isCurrentPage;

        /// <summary>
        /// Initializes a new instance of the PageViewModelBase class
        /// </summary>
        protected PageViewModelBase()
        {
        }

        /// <summary>
        /// Gets the display name for the view
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Gets or sets the subtitle to show in the view
        /// </summary>
        public string Subtitle { get; set; }

        /// <summary>
        /// Gets or sets the description to show in the view
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model is the current page in the wizard
        /// </summary>
        public bool IsCurrentPage
        {
            get { return this.isCurrentPage; }
            set { SetValue(ref this.isCurrentPage, value, () => this.IsCurrentPage); }
        }

        /// <summary>
        /// Returns true if the user has filled in this page properly
        /// and the wizard should allow the user to progress to the 
        /// next page in the workflow.
        /// </summary>
        /// <returns>A value indicating whether the model is valid</returns>
        internal abstract bool IsValid();
    }
}

