// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Activation
{
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;

    internal class AspNetEnvironment
    {
        public const string HostingMessagePropertyName = "webhost";
        
        private const string HostingMessagePropertyTypeName = "System.ServiceModel.Activation.HostingMessageProperty";
        private static AspNetEnvironment current;
        private static object thisLock = new object();

        public static AspNetEnvironment Current
        {
            get
            {
                if (current == null)
                {
                    lock (thisLock)
                    {
                        if (current == null)
                        {
                            current = new AspNetEnvironment();
                        }
                    }
                }

                return current;
            }
        }

        // ALTERED_FOR_PORT:
        // The GetHostingProperty() code below is an altered implementation from the System.ServiceModel.Activation.HostedAspNetEnvironment class.
        // The original implementation casts the hostingProperty to type System.ServiceModel.Activation.HostingMessageProperty.  However,
        //  this class is internal sealed, therefore we simply check the type name.
        [SuppressMessage(FxCop.Category.Performance, FxCop.Rule.MarkMembersAsStatic, Justification = "This is existing public API")]
        public object GetHostingProperty(Message message)
        {
            object hostingProperty;
            if (message.Properties.TryGetValue(HostingMessagePropertyName, out hostingProperty))
            {
                string hostingPropertyName = hostingProperty.GetType().FullName;
                if (string.Equals(hostingPropertyName, HostingMessagePropertyTypeName, System.StringComparison.Ordinal))
                {
                    return hostingProperty;
                }
            }

            return null;
        }

        // TODO: CSDMAIN 205599 -- verify this approach works under IIS
        [SuppressMessage(FxCop.Category.Performance, FxCop.Rule.MarkMembersAsStatic, Justification = "This is existing public API")]
        public object GetConfigurationSection(string sectionPath)
        {
            return ConfigurationManager.GetSection(sectionPath);
        }
    }
}
