// <copyright file="WSO2InteropBindingElement.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Wso2.Configuration
{
    using System;
    using System.Configuration;
    using System.Globalization;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;

    /// <summary>
    /// Binding element implementation for Apache Axis 2
    /// </summary>
    public class Wso2InteropBindingElement : StandardBindingElement
    {
        /// <summary>
        /// Initializes a new instance of the Wso2InteropBindingElement class
        /// </summary>
        /// <param name="configurationName">Binding configuration name</param>
        public Wso2InteropBindingElement(string configurationName)
            : base(configurationName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Wso2InteropBindingElement class
        /// </summary>
        public Wso2InteropBindingElement()
        {
        }
       
        /// <summary>
        /// Gets or sets the security configuration element
        /// </summary>
        [ConfigurationProperty(Constants.Security)]
        public Wso2SecurityElement Security
        {
            get { return (Wso2SecurityElement)base[Constants.Security]; }
            set { base[Constants.Security] = value; }
        }

        /// <summary>
        /// Gets the binding implementation type
        /// </summary>
        protected override Type BindingElementType
        {
            get
            {
                return typeof(Wso2InteropBinding);
            }
        }

        /// <summary>
        /// Gets all the available configuration properties
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection properties = base.Properties;
                properties.Add(new ConfigurationProperty(Constants.Security, typeof(Wso2SecurityElement), new Wso2SecurityElement()));
                return properties;
            }
        }

        /// <summary>
        /// Applies this configuration element to an existing binding instance
        /// </summary>
        /// <param name="binding">Binding instance</param>
        protected override void OnApplyConfiguration(Binding binding)
        {
            if (binding == null)
            {
                throw new System.ArgumentNullException(Constants.Binding);
            }

            if (binding.GetType() != typeof(Wso2InteropBinding))
            {
                throw new System.ArgumentException(string.Format(CultureInfo.CurrentCulture, "Invalid type for binding. Expected type: {0}. Type passed in: {1}.", typeof(Wso2InteropBinding).AssemblyQualifiedName, binding.GetType().AssemblyQualifiedName));
            }

            Wso2InteropBinding wso2InteropBinding = (Wso2InteropBinding)binding;
            wso2InteropBinding.Pattern = this.Security.Pattern;
            wso2InteropBinding.Bootstrap = this.Security.SecureConversationBootstrap;
        }
    }
}

