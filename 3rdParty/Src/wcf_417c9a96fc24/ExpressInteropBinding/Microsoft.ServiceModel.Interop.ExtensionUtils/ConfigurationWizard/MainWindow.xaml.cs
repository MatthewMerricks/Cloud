// <copyright file="MainWindow.xaml.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.Configuration;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.Services;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.ViewModel;
    using Microsoft.VisualStudio.PlatformUI;

    /// <summary>
    /// Model view class for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DialogWindow
    {
        private WizardViewModel wizardViewModel;
        private IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        /// <param name="bindingName">WCF binding name to be used by default</param>
        /// <param name="fileName">Path to the application configuration file</param>
        /// <param name="serviceProvider">Visual studio service provider instance</param>
        public MainWindow(string bindingName, string fileName, IServiceProvider serviceProvider)
        {
            InitializeComponent();

            this.serviceProvider = serviceProvider;

            var configModel = this.LoadConfigurationModel();

            this.wizardViewModel = new WizardViewModel(
                new FileSelectionService(),
                new CertificateStoreService(),
                configModel,
                bindingName,
                fileName);

            this.wizardViewModel.RequestClose += new EventHandler(this.WizardViewModel_RequestClose);

            this.DataContext = this.wizardViewModel;
        }

        /// <summary>
        /// Gets a value indicating whether the user has clicked ok in the dialog
        /// </summary>
        public bool Result { get; private set; }

        /// <summary>
        /// Gets the fully qualified type for the selected binding
        /// </summary>
        public string BindingType { get; private set; }

        private void WizardViewModel_RequestClose(object sender, EventArgs e)
        {
            if (!this.wizardViewModel.DialogResult)
            {
                this.Result = this.wizardViewModel.DialogResult;
                this.Close();
                return;
            }

            var basicSettingsPage = (BasicSettingsPageViewModel)this.wizardViewModel.Pages.FirstOrDefault(p => p is BasicSettingsPageViewModel);
            var scenarioPage = (ScenarioPageViewModel)this.wizardViewModel.Pages.First(p => p is ScenarioPageViewModel);
            var certificatePage = (CertificatePageViewModel)this.wizardViewModel.Pages.First(p => p is CertificatePageViewModel);
            var genericPages = this.wizardViewModel.Pages.Where(p => p is GenericWizardPageViewModel).Cast<GenericWizardPageViewModel>();

            var selectedScenario = scenarioPage.Scenarios.First(s => s.IsSelected);
            var templatePath = selectedScenario.WizardScenario.template[0].path;
            var bindingType = selectedScenario.WizardScenario.bindingName;
            var bindingImplType = selectedScenario.WizardScenario.bindingType;

            var values = new Dictionary<string, string>();

            foreach (var genericPage in genericPages)
            {
                foreach (var field in genericPage.Fields)
                {
                    string value = String.Empty;
                    if (field.HasOptions)
                    {
                        if (field.CurrentOption == null || (field.CurrentOption != null && field.CurrentOption.WizardOption == null))
                        {
                            value = field.WizardField.defaultValue;
                        }
                        else
                        {
                            value = field.CurrentOption.WizardOption.value;
                        }
                    }
                    else
                    {
                        value = field.FieldValue;
                    }

                    values.Add(field.WizardField.value, value);
                }
            }

            var templateService = new TemplateService(this.serviceProvider);
            templateService.WriteTemplateTo(
                templatePath,
                (basicSettingsPage == null) ? this.wizardViewModel.FileName : basicSettingsPage.ConfigurationFilePath,
                (basicSettingsPage == null) ? this.wizardViewModel.BindingName : basicSettingsPage.BindingName,
                bindingType,
                bindingImplType,
                certificatePage.StoreLocation,
                certificatePage.Store,
                certificatePage.Certificate,
                values);

            MessageBox.Show("The binding was successfully written in the configuration file.", "Binding Settings", MessageBoxButton.OK);

            this.Result = this.wizardViewModel.DialogResult;
            this.BindingType = bindingType;

            this.Close();
        }

        private interoperabilityWizard LoadConfigurationModel()
        {
            var loader = new ConfigurationModelLoader();
            var model = loader.Load();

            return model;
        }
    }
}