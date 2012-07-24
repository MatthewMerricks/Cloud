// <copyright file="WizardCommand.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ExtensionUtils
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using System.ServiceModel.Channels;
    using System.Xml.Linq;
    using EnvDTE;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.View;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.ViewModel;
    using VSLangProj;

    /// <summary>
    /// Visual studio command implementation that launches the configuration wizard
    /// </summary>
    public class WizardCommand
    {
        private IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the WizardCommand class
        /// </summary>
        /// <param name="serviceProvider">Visual studio service provider</param>
        public WizardCommand(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Executes the command
        /// </summary>
        public void Run()
        {
            _DTE dte = (_DTE)this.serviceProvider.GetService(typeof(_DTE));

            Project project = null;

            foreach (Project p in (IEnumerable)dte.ActiveSolutionProjects)
            {
                project = p;
                break;
            }

            string fileName = null;
            if (project != null)
            {
                foreach (ProjectItem item in project.ProjectItems)
                {
                    if (item.Name.Equals("web.config", StringComparison.InvariantCultureIgnoreCase) ||
                        item.Name.Equals("app.config", StringComparison.InvariantCultureIgnoreCase))
                    {
                        fileName = item.FileNames[0];
                        break;
                    }
                }
            }

            MainWindow mainWindow = new MainWindow(null, fileName, this.serviceProvider);
            try
            {
                mainWindow.ShowModal();

                if (mainWindow.Result && project != null)
                {
                    var systemServiceModel = typeof(Binding).Assembly.Location;
                    var assemblyName = typeof(InteropBinding).Assembly.Location;

                    var vistualStudioProj = project.Object as VSProject;

                    var serviceModelDefined = AnyReference(vistualStudioProj, "System.ServiceModel.dll");
                    var interopDefined = AnyReference(vistualStudioProj, "Microsoft.ServiceModel.Interop.dll");

                    if (!serviceModelDefined)
                    {
                        vistualStudioProj.References.Add(systemServiceModel);
                    }

                    if (!interopDefined)
                    {
                        vistualStudioProj.References.Add(assemblyName);
                    }

                    UpdateEndpoint(fileName, mainWindow.BindingType);
                }
            }
            catch (Exception ex)
            {
                ErrorView view = new ErrorView();
                ErrorViewModel model = new ErrorViewModel();
                model.Exception = ex.ToString();

                view.DataContext = model;
                view.ShowModal();
            }
        }

        private static void UpdateEndpoint(string fileName, string bindingType)
        {
            string content = null;
            using (var sr = new StreamReader(fileName))
            {
                content = sr.ReadToEnd();
            }

            var root = XElement.Parse(content);

            var endpointElement = root.Descendants("endpoint")
                .FirstOrDefault(e => e.Attributes().Any(a => a.Name.LocalName == "address" && String.IsNullOrEmpty(a.Value)));

            if (endpointElement != null)
            {
                var bindingTypeAttribute = endpointElement.Attribute("binding");
                if (bindingTypeAttribute != null)
                {
                    bindingTypeAttribute.Value = bindingType;
                }
            }

            root.Save(fileName);
        }

        private static bool AnyReference(VSProject project, string path)
        {
            foreach (Reference reference in project.References)
            {
                if (reference.Path.EndsWith(path, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

