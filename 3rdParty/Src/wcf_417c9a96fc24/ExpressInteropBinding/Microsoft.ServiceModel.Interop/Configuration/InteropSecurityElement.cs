// <copyright file="InteropSecurityElement.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Configuration
{
    using System;
    using System.ComponentModel;
    using System.Configuration;
    using System.ServiceModel.Security;

    /// <summary>
    /// Base class for the security configuration element that the rest of the service stacks derive
    /// </summary>
    public class InteropSecurityElement : ConfigurationElement
    {
        private const string EstablishSecurityContextProperty = "establishSecurityContext";
        private const string AlgorithmSuiteProperty = "algorithmSuite";

        /// <summary>
        /// Initializes a new instance of the InteropSecurityElement class
        /// </summary>
        protected InteropSecurityElement()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether a secure conversation must be established
        /// </summary>
        [ConfigurationProperty(EstablishSecurityContextProperty, IsRequired = false, DefaultValue = false)]
        public bool EstablishSecurityContext
        {
            get { return (bool)base[EstablishSecurityContextProperty]; }
            set { base[EstablishSecurityContextProperty] = value; }
        }

        /// <summary>
        /// Gets or sets the security algorithm
        /// </summary>
        [TypeConverter(typeof(SecurityAlgorithmSuiteConverter)), ConfigurationProperty(AlgorithmSuiteProperty, DefaultValue = "Default")]
        public SecurityAlgorithmSuite AlgorithmSuite
        {
            get { return (SecurityAlgorithmSuite)base[AlgorithmSuiteProperty]; }
            set { base[AlgorithmSuiteProperty] = value; }
        }

        /// <summary>
        /// Gets all the available configuration properties
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection properties = base.Properties;
                properties.Add(new ConfigurationProperty(EstablishSecurityContextProperty, typeof(bool), false));
                properties.Add(new ConfigurationProperty(AlgorithmSuiteProperty, typeof(SecurityAlgorithmSuite), "Default", new SecurityAlgorithmSuiteConverter(), null, ConfigurationPropertyOptions.None));

                return properties;
            }
        }

        /// <summary>
        /// Applies this configuration instance to an existing security binding element
        /// </summary>
        /// <param name="security">Security binding element</param>
        public virtual void ApplyConfiguration(InteropSecurity security)
        {
            if (security == null)
            {
                throw new ArgumentNullException("security");
            }

            security.AlgorithmSuite = this.AlgorithmSuite;
            security.EstablishSecurityContext = this.EstablishSecurityContext;
        }

        /// <summary>
        /// Initializes this security configuration from an existing security binding element
        /// </summary>
        /// <param name="security">Security binding element</param>
        public virtual void InitializeFrom(InteropSecurity security)
        {
            if (security == null)
            {
                throw new ArgumentNullException("security");
            }

            this.AlgorithmSuite = security.AlgorithmSuite;
            this.EstablishSecurityContext = security.EstablishSecurityContext;
        }
    }
}

