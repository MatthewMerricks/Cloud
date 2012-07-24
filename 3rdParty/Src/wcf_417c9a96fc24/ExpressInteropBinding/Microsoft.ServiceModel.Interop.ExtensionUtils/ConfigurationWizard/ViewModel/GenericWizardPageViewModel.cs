// <copyright file="GenericWizardPageViewModel.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.Configuration;

    /// <summary>
    /// View model for a generic wizard page that gathers input information from the user
    /// </summary>
    public class GenericWizardPageViewModel : PageViewModelBase
    {
        private ObservableCollection<FieldModel> fields;

        /// <summary>
        /// Initializes a new instance of the GenericWizardPageViewModel class
        /// </summary>
        /// <param name="scenario">Scenario name</param>
        /// <param name="wizardPage">Wizard page settings</param>
        public GenericWizardPageViewModel(string scenario, interoperabilityWizardScenarioPage wizardPage)
        {
            if (wizardPage == null)
            {
                throw new ArgumentNullException("wizardPage");
            }

            if (string.IsNullOrEmpty(scenario))
            {
                throw new ArgumentNullException("scenario");
            }

            this.WizardPage = wizardPage;
            this.Subtitle = scenario;

            if (wizardPage.field != null)
            {
                this.fields = new ObservableCollection<FieldModel>(wizardPage.field
                    .Select(f => new FieldModel(f)));
            }
            else
            {
                this.fields = new ObservableCollection<FieldModel>();
            }

            this.Description = wizardPage.description;
        }

        /// <summary>
        /// Gets the page settings used for initializing the model
        /// </summary>
        public interoperabilityWizardScenarioPage WizardPage
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets all the available fields for the page
        /// </summary>
        public IEnumerable<FieldModel> Fields
        {
            get { return this.fields; }
        }

        /// <summary>
        /// Gets the page title
        /// </summary>
        public override string DisplayName
        {
            get { return this.WizardPage.title; }
        }

        internal override bool IsValid()
        {
            return this.fields.All(f => f.CurrentOption != null
                || (!f.IsRequired || (f.IsRequired && !string.IsNullOrEmpty(f.FieldValue)))
                || (f.CurrentOption == null && f.HasSingleOption));
        }
    }
}

