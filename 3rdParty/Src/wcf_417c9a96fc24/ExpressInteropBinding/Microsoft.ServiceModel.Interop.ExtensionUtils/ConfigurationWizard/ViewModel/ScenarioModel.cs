// <copyright file="ScenarioModel.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.Configuration;

    /// <summary>
    /// View model for representing an scenario in the configuration wizard
    /// </summary>
    public class ScenarioModel : ObservableModel
    {
        private bool isSelected = false;
        private Action<IEnumerable<PageViewModelBase>> addPageCallback;

        /// <summary>
        /// Initializes a new instance of the ScenarioModel class
        /// </summary>
        /// <param name="wizardScenario">Scenario settings</param>
        /// <param name="addPageCallback">Callback for adding pages dynamically</param>
        public ScenarioModel(interoperabilityWizardScenario wizardScenario, Action<IEnumerable<PageViewModelBase>> addPageCallback)
        {
            this.WizardScenario = wizardScenario;
            this.addPageCallback = addPageCallback;
        }

        /// <summary>
        /// Gets the scenario settings
        /// </summary>
        public interoperabilityWizardScenario WizardScenario
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the scenario name
        /// </summary>
        public string Name
        {
            get 
            { 
                return this.WizardScenario.name; 
            }

            set
            {
                this.WizardScenario.name = value;
                OnPropertyChanged(() => this.Name);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the scenario is selected
        /// </summary>
        public bool IsSelected
        {
            get { return this.isSelected; }
            set { SetValue(ref this.isSelected, value, this.IsSelectedChanged); }
        }

        private void IsSelectedChanged()
        {
            OnPropertyChanged(() => this.IsSelected);

            if (this.IsSelected)
            {
                this.addPageCallback(this.WizardScenario.pages.Select(p => (PageViewModelBase)new GenericWizardPageViewModel(this.WizardScenario.name, p)));
            }
        }
    }
}

