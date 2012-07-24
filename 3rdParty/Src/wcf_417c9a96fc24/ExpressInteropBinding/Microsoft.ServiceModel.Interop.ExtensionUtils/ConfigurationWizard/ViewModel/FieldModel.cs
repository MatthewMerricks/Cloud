// <copyright file="FieldModel.cs" company="Microsoft Corporation">
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
    /// View model that carries information about a field in a wizard screen
    /// </summary>
    public class FieldModel : ObservableModel
    {
        private ObservableCollection<FieldOptionModel> options;
        private FieldOptionModel currentOption;
        private string fieldValue;
        private bool optionSelected;

        /// <summary>
        /// Initializes a new instance of the FieldModel class
        /// </summary>
        /// <param name="wizardField">Wizard field settings</param>
        public FieldModel(interoperabilityWizardScenarioPageField wizardField)
        {
            if (wizardField == null)
            {
                throw new ArgumentNullException("wizardField");
            }

            this.WizardField = wizardField;

            if (wizardField.options != null)
            {
                this.options = new ObservableCollection<FieldOptionModel>(
                    wizardField.options.Select(o => new FieldOptionModel(o)));

                if (wizardField.isRequiredSpecified && !wizardField.isRequired)
                {
                    this.options.Insert(0, new FieldOptionModel(null));
                }

                if (this.options.Count > 1)
                {
                    this.CurrentOption = this.options[0];
                }
            }
            else
            {
                this.options = new ObservableCollection<FieldOptionModel>();
            }
        }

        /// <summary>
        /// Gets the wizard field settings
        /// </summary>
        public interoperabilityWizardScenarioPageField WizardField
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the field caption
        /// </summary>
        public string Caption
        {
            get { return this.WizardField.caption; }
        }

        /// <summary>
        /// Gets the field tooltip
        /// </summary>
        public string ToolTip
        {
            get { return this.WizardField.tooltip; }
        }

        /// <summary>
        /// Gets a value indicating whether the field is required
        /// </summary>
        public bool IsRequired
        {
            get { return this.WizardField.isRequired; }
        }

        /// <summary>
        /// Gets a list of available options for the field
        /// </summary>
        public IEnumerable<FieldOptionModel> Options
        {
            get { return this.options; }
        }

        /// <summary>
        /// Gets or sets the selected option for the field
        /// </summary>
        public FieldOptionModel CurrentOption
        {
            get { return this.currentOption; }
            set { SetValue(ref this.currentOption, value, () => this.CurrentOption); }
        }

        /// <summary>
        /// Gets or sets an plain string value for the field when there is not options associated
        /// </summary>
        public string FieldValue
        {
            get { return this.fieldValue; }
            set { SetValue(ref this.fieldValue, value, () => this.FieldValue); }
        }

        /// <summary>
        /// Gets a value indicating whether the field has associated options
        /// </summary>
        public bool HasOptions
        {
            get
            {
                return this.options.Count > 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the field has a single associated option
        /// </summary>
        public bool HasSingleOption
        {
            get
            {
                return this.options.Count == 1;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the field has multiple associated option
        /// </summary>
        public bool HasMultipleOptions
        {
            get
            {
                return this.HasOptions && !this.HasSingleOption;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an option has been selected
        /// </summary>
        public bool OptionSelected
        {
            get { return this.optionSelected; }
            set { SetValue(ref this.optionSelected, value, this.OptionSelectedChanged); }
        }

        private void OptionSelectedChanged()
        {
            OnPropertyChanged(() => this.OptionSelected);

            this.CurrentOption = this.Options.First();
        }
    }
}

