// <copyright file="ConfigurationModelLoader.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.Services
{
    using System.Reflection;
    using System.Xml.Serialization;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.Configuration;

    /// <summary>
    /// Helper class for loading the wizard configuration model
    /// </summary>
    public class ConfigurationModelLoader
    {
        /// <summary>
        /// Loads the configuration model included as a embedded resource
        /// </summary>
        /// <returns>A valid instance of the configuration model</returns>
        public interoperabilityWizard Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var sr = assembly.GetManifestResourceStream("Microsoft.ServiceModel.Interop.ExtensionUtils.ConfigurationWizard.WizardUI.xml"))
            {
                var serializer = new XmlSerializer(typeof(interoperabilityWizard));
                var model = (interoperabilityWizard)serializer.Deserialize(sr);

                return model;
            }
        }
    }
}

