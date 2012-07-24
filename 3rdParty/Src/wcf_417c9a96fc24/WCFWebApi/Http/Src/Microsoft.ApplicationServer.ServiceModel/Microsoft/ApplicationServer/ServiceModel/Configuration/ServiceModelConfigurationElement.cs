// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Configuration
{
    using System.Configuration;
    using System.Diagnostics.Contracts;
    using Microsoft.ApplicationServer.Common;

    public abstract class ServiceModelConfigurationElement : ConfigurationElement
    {
        internal ServiceModelConfigurationElement()
        {
        }

        protected void SetPropertyValueIfNotDefaultValue<T>(string propertyName, T value)
        {
            ConfigurationProperty configurationProperty = this.Properties[propertyName];

            Fx.Assert(configurationProperty != null, "Parameter 'propertyName' should be the name of a configuration property of type T");
            Fx.Assert(configurationProperty.Type.IsAssignableFrom(typeof(T)), "Parameter 'propertyName' should be the name of a configuration property of type T");

            if (!object.Equals(value, configurationProperty.DefaultValue))
            {
                SetPropertyValue(configurationProperty, value, /*ignoreLocks = */ false);
            }
        }
    }
}
