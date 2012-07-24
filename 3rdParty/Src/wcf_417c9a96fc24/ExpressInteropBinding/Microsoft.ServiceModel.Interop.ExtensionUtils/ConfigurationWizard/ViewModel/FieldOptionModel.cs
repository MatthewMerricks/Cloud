// <copyright file="FieldOptionModel.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.ViewModel
{
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.Configuration;

    /// <summary>
    /// View model that represents a valid option for a wizard field
    /// </summary>
    public class FieldOptionModel : ObservableModel
    {
        /// <summary>
        /// Initializes a new instance of the FieldOptionModel class
        /// </summary>
        /// <param name="wizardOption">Options settings</param>
        public FieldOptionModel(interoperabilityWizardScenarioPageFieldOption wizardOption)
        {
            this.WizardOption = wizardOption;
        }

        /// <summary>
        /// Gets the option settings used to initialize the model
        /// </summary>
        public interoperabilityWizardScenarioPageFieldOption WizardOption
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the option text
        /// </summary>
        public string Name
        {
            get { return (this.WizardOption != null) ? this.WizardOption.name : "(Default)"; }
        }
    }
}