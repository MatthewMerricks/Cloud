// <copyright file="ProjectTemplateWizard.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ExtensionUtils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using EnvDTE;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.View;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.ViewModel;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.TemplateWizard;
    using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    /// <summary>
    /// Visual Studio Wizard implementation class associated to the Interoperable Service templates
    /// </summary>
    public class ProjectTemplateWizard : IWizard, IDisposable
    {
        private ServiceProvider serviceProvider;
        private Project templateProject;

        /// <summary>
        /// Finalizes an instance of the ProjectTemplateWizard class
        /// </summary>
        ~ProjectTemplateWizard()
        {
            this.Dispose(false);
        }

        /// <summary> 
        /// Runs custom wizard logic before opening an item in the template.
        /// </summary>
        /// <param name="projectItem"> The project item that will be opened.</param>
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        /// <summary>
        /// Runs custom wizard logic when a project has finished generating.
        /// </summary>
        /// <param name="project"> The project that finished generating</param>
        public void ProjectFinishedGenerating(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            if (project.Name != "Solution Items")
            {
                this.templateProject = project;
            }
        }

        /// <summary>
        /// Runs custom wizard logic when a project item has finished generating.
        /// </summary>
        /// <param name="projectItem">The project item that finished generating</param>
        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        /// <summary>
        /// Runs custom wizard logic when the wizard has completed all tasks
        /// </summary>
        public void RunFinished()
        {
            _DTE dte = (_DTE)this.serviceProvider.GetService(typeof(_DTE));

            if (this.templateProject == null)
            {
                foreach (Project p in dte.Solution.Projects)
                {
                    if (p.Name != "Solution Items")
                    {
                        this.templateProject = p;
                        break;
                    }
                }
            }

            string fileName = null;
            if (this.templateProject != null)
            {
                foreach (ProjectItem item in this.templateProject.ProjectItems)
                {
                    if (item.Name.Equals("web.config", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Equals("app.config", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName = item.FileNames[0];
                        break;
                    }
                }
            }

            MainWindow mainWindow = new MainWindow("interopBinding", fileName, this.serviceProvider);
            try
            {
                mainWindow.ShowDialog();

                if (mainWindow.Result)
                {
                    UpdateEndpoint(fileName, mainWindow.BindingType);
                }

                mainWindow.Close();
            }
            catch (Exception ex)
            {
                if (mainWindow != null)
                {
                    mainWindow.Close();
                }

                ErrorView view = new ErrorView();
                ErrorViewModel model = new ErrorViewModel();
                model.Exception = ex.ToString();

                view.DataContext = model;
                view.ShowDialog();
            }
        }

        /// <summary>
        /// Runs custom wizard logic at the beginning of a template wizard run
        /// </summary>
        /// <param name="automationObject">The automation object being used by the template wizard</param>
        /// <param name="replacementsDictionary">The list of standard parameters to be replaced</param>
        /// <param name="runKind">A Microsoft.VisualStudio.TemplateWizard.WizardRunKind indicating the type of wizard run</param>
        /// <param name="customParams">The custom parameters with which to perform parameter replacement in the project</param>
        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            this.serviceProvider = new ServiceProvider((IOleServiceProvider)automationObject);
        }

        /// <summary>
        /// Indicates whether the specified project item should be added to the project
        /// </summary>
        /// <param name="filePath">The path to the project item</param>
        /// <returns> true if the project item should be added to the project; otherwise, false</returns>
        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        /// <summary>
        /// Disposable implementation
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposable implementation
        /// </summary>
        /// <param name="disposing">Indicates whether this method was called from the Dispose method</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.serviceProvider.Dispose();
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
                .FirstOrDefault(e => e.Attributes().Any(a => a.Name.LocalName == "address" && string.IsNullOrEmpty(a.Value)));

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
    }
}