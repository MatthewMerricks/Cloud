// <copyright file="ScenarioPageViewModel.cs" company="Microsoft Corporation">
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
    /// View model for the scenario page in the configuration wizard
    /// </summary>
    public class ScenarioPageViewModel : PageViewModelBase
    {
        private ObservableCollection<ScenarioModel> scenarios;

        /// <summary>
        /// Initializes a new instance of the ScenarioPageViewModel class
        /// </summary>
        /// <param name="scenarios">List of available scenarios</param>
        /// <param name="addPageCallback">Callback for adding pages dynamically</param>
        public ScenarioPageViewModel(IEnumerable<interoperabilityWizardScenario> scenarios, Action<IEnumerable<PageViewModelBase>> addPageCallback)
        {
            this.scenarios = new ObservableCollection<ScenarioModel>(
                scenarios.Select(s => new ScenarioModel(s, addPageCallback)));
        }

        /// <summary>
        /// Display name for view
        /// </summary>
        public override string DisplayName
        {
            get { return "Interoperability Platform"; }
        }

        /// <summary>
        /// Gets a list of available scenarios
        /// </summary>
        public IEnumerable<ScenarioModel> Scenarios
        {
            get { return this.scenarios; }
        }

        internal override bool IsValid()
        {
            return this.scenarios.Any(s => s.IsSelected);
        }
    }
}

