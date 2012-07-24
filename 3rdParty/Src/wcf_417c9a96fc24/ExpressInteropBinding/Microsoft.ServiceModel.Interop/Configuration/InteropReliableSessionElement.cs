// <copyright file="InteropReliableSessionElement.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Configuration
{
    using System;
    using System.ComponentModel;
    using System.Configuration;

    /// <summary>
    /// Base class for the reliable session binding element that the rest of the service stacks derive
    /// </summary>
    public class InteropReliableSessionElement : ConfigurationElement
    {
        private const string EnabledProperty = "enabled";
        private const string InactivityTimeoutProperty = "inactivityTimeout";

        /// <summary>
        /// Initializes a new instance of the InteropReliableSessionElement class
        /// </summary>
        public InteropReliableSessionElement()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether reliable session is enabled
        /// </summary>
        [ConfigurationProperty(EnabledProperty, IsRequired = true)]
        public bool Enabled
        {
            get { return (bool)base[EnabledProperty]; }
            set { base[EnabledProperty] = value; }
        }

        /// <summary>
        /// Gets or sets the inactivity timeout for the reliable session
        /// </summary>
        [TypeConverter(typeof(TimeSpanOrInfiniteConverter)), ConfigurationProperty(InactivityTimeoutProperty, DefaultValue = "00:10:00")]
        public TimeSpan InactivityTimeout
        {
            get { return (TimeSpan)base[InactivityTimeoutProperty]; }
            set { base[InactivityTimeoutProperty] = value; }
        }

        /// <summary>
        /// Gets all the available configuration properties
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection properties = base.Properties;
                properties.Add(new ConfigurationProperty(EnabledProperty, typeof(bool), true, ConfigurationPropertyOptions.IsRequired));
                properties.Add(new ConfigurationProperty(InactivityTimeoutProperty, typeof(TimeSpan), "00:10:00", new TimeSpanOrInfiniteConverter(), null, ConfigurationPropertyOptions.None));

                return properties;
            }
        }

        /// <summary>
        /// Applies this configuration instance to an existing reliable session element
        /// </summary>
        /// <param name="reliableSession">Reliable session binding element</param>
        public virtual void ApplyConfiguration(InteropReliableSession reliableSession)
        {
            if (reliableSession == null)
            {
                throw new ArgumentNullException("reliableSession");
            }

            reliableSession.Enabled = this.Enabled;
            reliableSession.InactivityTimeout = this.InactivityTimeout;
        }

        /// <summary>
        /// Initializes this configuration instance from an existing reliable session element
        /// </summary>
        /// <param name="reliableSession">Reliable session binding element</param>
        public virtual void InitializeFrom(InteropReliableSession reliableSession)
        {
            if (reliableSession == null)
            {
                throw new ArgumentNullException("reliableSession");
            }

            this.Enabled = reliableSession.Enabled;
            this.InactivityTimeout = reliableSession.InactivityTimeout;
        }
    }
}

