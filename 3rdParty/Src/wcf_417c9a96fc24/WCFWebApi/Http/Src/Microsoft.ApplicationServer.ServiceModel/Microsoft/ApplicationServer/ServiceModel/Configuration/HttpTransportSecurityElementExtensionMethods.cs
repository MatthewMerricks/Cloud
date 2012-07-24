// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Configuration
{
    using System.Configuration;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Configuration;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.ServiceModel.Channels;

    internal static class HttpTransportSecurityElementExtensionMethods
    {
        internal static void ApplyConfiguration(this HttpTransportSecurityElement httpTransportSecurityElement, HttpTransportSecurity httpTransportSecurity)
        {
            Fx.Assert(httpTransportSecurityElement != null, "httpTransportSecurityElement cannot be null");
            Fx.Assert(httpTransportSecurity != null, "httpTransportSecurity cannot be null");

            httpTransportSecurity.ClientCredentialType = httpTransportSecurityElement.ClientCredentialType;
            httpTransportSecurity.ProxyCredentialType = httpTransportSecurityElement.ProxyCredentialType;
            httpTransportSecurity.Realm = httpTransportSecurityElement.Realm;
            httpTransportSecurity.ExtendedProtectionPolicy = ChannelBindingUtility.BuildPolicy(httpTransportSecurityElement.ExtendedProtectionPolicy);
        }

        internal static void InitializeFrom(this HttpTransportSecurityElement httpTransportSecurityElement, HttpTransportSecurity httpTransportSecurity)
        {
            Fx.Assert(httpTransportSecurityElement != null, "httpTransportSecurityElement cannot be null");
            Fx.Assert(httpTransportSecurity != null, "httpTransportSecurity cannot be null");

            ConfigurationElementProxy proxy = new ConfigurationElementProxy(httpTransportSecurityElement);

            proxy.SetPropertyValueIfNotDefaultValue<HttpClientCredentialType>(ConfigurationStrings.ClientCredentialType, httpTransportSecurity.ClientCredentialType);
            proxy.SetPropertyValueIfNotDefaultValue<HttpProxyCredentialType>(ConfigurationStrings.ProxyCredentialType, httpTransportSecurity.ProxyCredentialType);
            proxy.SetPropertyValueIfNotDefaultValue<string>(ConfigurationStrings.Realm, httpTransportSecurity.Realm);

            ChannelBindingUtility.InitializeFrom(httpTransportSecurity.ExtendedProtectionPolicy, httpTransportSecurityElement.ExtendedProtectionPolicy);
        }

        private class ConfigurationElementProxy
        {
            private static PropertyInfo propertiesGetter = typeof(ConfigurationElement).GetProperty("Properties", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            private static MethodInfo setPropertyValueMethod = typeof(ConfigurationElement).GetMethod("SetPropertyValue", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            public ConfigurationElementProxy(ConfigurationElement configurationElement)
            {
                Fx.Assert(configurationElement != null, "configurationElement cannot be null");
                Fx.Assert(propertiesGetter != null, "propertiesGetter could not be located");
                Fx.Assert(setPropertyValueMethod != null, "setPropertyValueMethod could not be located");

                this.ConfigurationElement = configurationElement;
            }

            public ConfigurationPropertyCollection Properties
            {
                get
                {
                    return propertiesGetter.GetValue(this.ConfigurationElement, null) as ConfigurationPropertyCollection;
                }
            }

            private ConfigurationElement ConfigurationElement { get; set; }

            public void SetPropertyValueIfNotDefaultValue<T>(string propertyName, object value)
            {
                ConfigurationPropertyCollection properties = this.Properties;
                ConfigurationProperty configurationProperty = properties[propertyName];

                Fx.Assert(configurationProperty != null, "Parameter 'propertyName' should be the name of a configuration property of type T");
                Fx.Assert(configurationProperty.Type.IsAssignableFrom(typeof(T)), "Parameter 'propertyName' should be the name of a configuration property of type T");

                if (!object.Equals(value, configurationProperty.DefaultValue))
                {
                    this.SetPropertyValue(configurationProperty, value);
                }
            }

            public void SetPropertyValue(ConfigurationProperty property, object value)
            {
                setPropertyValueMethod.Invoke(this.ConfigurationElement, new object[] { property, value, /*ignoreLocks = */ false });
            }
        }
    }
}
