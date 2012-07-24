// <copyright file="TemplateService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;

    /// <summary>
    /// Template engine service
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>
        /// Writes the template output to a file
        /// </summary>
        /// <param name="templateName">Name of the template</param>
        /// <param name="filePath">Output path where the configuration is going to be saved</param>
        /// <param name="bindingName">Name for the generated binding</param>
        /// <param name="bindingType">WCF binding type for the generated binding</param>
        /// <param name="bindingImplementationType">Fully qualified type name for the generated binding implementation</param>
        /// <param name="certificateStoreLocation">Certificate location name</param>
        /// <param name="certificateStore">Certificate store</param>
        /// <param name="certificateName">Certificate name</param>
        /// <param name="fields">Template fields or variables to be passed to the template</param>
        void WriteTemplateTo(
            string templateName,
            string filePath,
            string bindingName,
            string bindingType,
            string bindingImplementationType,
            string certificateStoreLocation,
            string certificateStore,
            string certificateName,
            IDictionary<string, string> fields);
    }

    /// <summary>
    /// Template engine service implementation that uses T4 for rendering content
    /// </summary>
    public class TemplateService : ITemplateService
    {
        private IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the TemplateService class
        /// </summary>
        /// <param name="serviceProvider">Visual studio service provider</param>
        public TemplateService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Writes the template output to a file
        /// </summary>
        /// <param name="templateName">Name of the template</param>
        /// <param name="filePath">Output path where the configuration is going to be saved</param>
        /// <param name="bindingName">Name for the generated binding</param>
        /// <param name="bindingType">WCF binding type for the generated binding</param>
        /// <param name="bindingImplementationType">Fully qualified type name for the generated binding implementation</param>
        /// <param name="certificateStoreLocation">Certificate location name</param>
        /// <param name="certificateStore">Certificate store</param>
        /// <param name="certificateName">Certificate name</param>
        /// <param name="fields">Template fields or variables to be passed to the template</param>
        public void WriteTemplateTo(
            string templateName, 
            string filePath,
            string bindingName, 
            string bindingType, 
            string bindingImplementationType,
            string certificateStoreLocation, 
            string certificateStore, 
            string certificateName,
            IDictionary<string, string> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException("fields");
            }

            var template = LoadTemplate(templateName);

            var invoker = new T4Invoker(this.serviceProvider);

            fields.Add("BindingName", bindingName);
            fields.Add("BindingType", bindingType);

            var transformationResult = invoker.TransformText(templateName, template, fields);

            var content = transformationResult.Content;

            WriteBindingToFile(
                filePath, 
                bindingType, 
                bindingImplementationType, 
                content,
                certificateStoreLocation, 
                certificateStore, 
                certificateName);
        }

        private static string LoadTemplate(string templateName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Stream ms = assembly.GetManifestResourceStream("Microsoft.ServiceModel.Interop.ExtensionUtils.Templates." + templateName);

            using (var sr = new StreamReader(ms))
            {
                return sr.ReadToEnd();
            }
        }

        private static void WriteBindingToFile(
            string filePath, 
            string bindingType, 
            string bindingImplementationType, 
            string binding,
            string certificateStoreLocation, 
            string certificateStore, 
            string certificateName)
        {
            XElement configuration;

            using (var sr = new StreamReader(filePath))
            {
                var content = sr.ReadToEnd();
                configuration = XElement.Parse(content);
            }

            var serviceModelElement = configuration.Element("system.serviceModel");
            if (serviceModelElement == null)
            {
                serviceModelElement = new XElement("system.serviceModel");
                configuration.Add(serviceModelElement);
            }

            var bindingsElement = serviceModelElement.Element("bindings");
            if (bindingsElement == null)
            {
                bindingsElement = new XElement("bindings");
                serviceModelElement.Add(bindingsElement);
            }

            var bindingTypeElement = bindingsElement.Element(bindingType);
            if (bindingTypeElement == null)
            {
                bindingTypeElement = new XElement(bindingType);
                bindingsElement.Add(bindingTypeElement);
            }

            var bindingElement = XElement.Parse(binding);
            bindingTypeElement.Add(bindingElement);

            var extensions = serviceModelElement.Element("extensions");
            if (extensions == null)
            {
                extensions = new XElement("extensions");
                serviceModelElement.Add(extensions);
            }

            var bindingExtensions = extensions.Element("bindingExtensions");
            if (bindingExtensions == null)
            {
                bindingExtensions = new XElement("bindingExtensions");
                extensions.Add(bindingExtensions);
            }

            var bindingTypeExtension = bindingExtensions.Elements().FirstOrDefault(e => e.Name == "add" &&
                e.Attributes().Any(a => a.Name == "name" && a.Value == bindingType));

            if (bindingTypeExtension == null)
            {
                bindingTypeExtension = new XElement("add");
                bindingTypeExtension.Add(new XAttribute("name", bindingType));
                bindingTypeExtension.Add(new XAttribute("type", bindingImplementationType));

                bindingExtensions.Add(bindingTypeExtension);
            }

            var serviceCertificateElement = serviceModelElement.Descendants()
                .FirstOrDefault(e => e.Name == "serviceCertificate");
            if (serviceCertificateElement != null)
            {
                serviceCertificateElement.Attribute("storeLocation").Value = certificateStoreLocation;
                serviceCertificateElement.Attribute("storeName").Value = certificateStore;
                serviceCertificateElement.Attribute("findValue").Value = certificateName;
            }

            using (var sw = new StreamWriter(filePath, false))
            {
                sw.Write(configuration.ToString());
            }
        }
    }
}

