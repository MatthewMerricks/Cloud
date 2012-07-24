// <copyright file="WSO2InteropBinding.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Wso2
{
    using System;
    using System.Configuration;
    using System.Globalization;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;
    using System.ServiceModel.Security;
    using Microsoft.ServiceModel.Interop.Wso2.Configuration;

    /// <summary>
    /// WCF binding implementation for Apache Axis 2
    /// </summary>
    public class Wso2InteropBinding : CustomBinding
    {
        private HttpsTransportBindingElement https = new HttpsTransportBindingElement();
        private HttpTransportBindingElement http = new HttpTransportBindingElement();

        /// <summary>
        /// Initializes a new instance of the Wso2InteropBinding class
        /// </summary>
        public Wso2InteropBinding()
        {
            this.Pattern = Constants.DefaultPattern;
        }

        /// <summary>
        /// Initializes a new instance of the Wso2InteropBinding class
        /// </summary>
        /// <param name="configurationName">Binding configuration name</param>
        public Wso2InteropBinding(string configurationName)
        {
            this.ApplyConfiguration(configurationName);
        }

        /// <summary>
        /// Gets or sets the security pattern
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// Gets or sets the security pattern to use with secure conversation
        /// </summary>
        public string Bootstrap { get; set; }

        /// <summary>
        /// Gets the transport scheme
        /// </summary>
        public override string Scheme
        {
            get
            {
                string scheme;

                if (this.Pattern == Wso2InteropBindingPatters.UserNameOverTransport)
                {
                    scheme = this.https.Scheme;
                }
                else
                {
                    scheme = this.http.Scheme;
                }

                return scheme;
            }
        }

        /// <summary>
        /// Creates the binding elements for initializating the communication channel
        /// </summary>
        /// <returns>A collection of binding elements to initialize the WCF channel</returns>
        public override BindingElementCollection CreateBindingElements()
        {
            SecurityBindingElement security = null;
            TransportBindingElement transport = this.http;
            if (this.Pattern == Wso2InteropBindingPatters.UserNameOverTransport)
            {
                transport = this.https;
                security = SecurityBindingElement.CreateUserNameOverTransportBindingElement();
            }
            else if (this.Pattern == Wso2InteropBindingPatters.SecureConversation)
            {
                SecurityBindingElement bootstrap = GetSecurityBindingElement(this.Bootstrap);

                security = SecurityBindingElement.CreateSecureConversationBindingElement(bootstrap, false);

                SymmetricSecurityBindingElement symmetricSecurity = (SymmetricSecurityBindingElement)security;

                symmetricSecurity.MessageProtectionOrder = MessageProtectionOrder.SignBeforeEncrypt;
                symmetricSecurity.ProtectionTokenParameters.RequireDerivedKeys = false;
            }
            else if (this.Pattern == Wso2InteropBindingPatters.Kerberos)
            {
                security = SecurityBindingElement.CreateKerberosBindingElement();

                SymmetricSecurityBindingElement symmetricSecurity = (SymmetricSecurityBindingElement)security;

                symmetricSecurity.MessageProtectionOrder = MessageProtectionOrder.SignBeforeEncrypt;
                symmetricSecurity.ProtectionTokenParameters.RequireDerivedKeys = false;
            }
            else
            {
                security = GetSecurityBindingElement(this.Pattern);
            }

            BindingElementCollection newCol = new BindingElementCollection();
            newCol.Add(security);
            newCol.Add(transport);
            return newCol.Clone();
        }

        private static SecurityBindingElement GetSecurityBindingElement(string scenario)
        {
            SecurityBindingElement security = null;
            if (scenario == Wso2InteropBindingPatters.MutualCertificateDuplex)
            {
                security = SecurityBindingElement.CreateMutualCertificateDuplexBindingElement();
                ((AsymmetricSecurityBindingElement)security).MessageProtectionOrder = MessageProtectionOrder.SignBeforeEncrypt;
            }
            else if (scenario == Wso2InteropBindingPatters.UserNameForCertificate)
            {
                security = SecurityBindingElement.CreateUserNameForCertificateBindingElement();

                SymmetricSecurityBindingElement symmetricSecurity = (SymmetricSecurityBindingElement)security;

                symmetricSecurity.MessageProtectionOrder = MessageProtectionOrder.SignBeforeEncrypt;
                symmetricSecurity.ProtectionTokenParameters.RequireDerivedKeys = false;
            }
            else if (scenario == Wso2InteropBindingPatters.AnonymousForCertificate)
            {
                security = SecurityBindingElement.CreateAnonymousForCertificateBindingElement();

                SymmetricSecurityBindingElement symmetricSecurity = (SymmetricSecurityBindingElement)security;

                symmetricSecurity.MessageProtectionOrder = MessageProtectionOrder.SignBeforeEncrypt;
                symmetricSecurity.ProtectionTokenParameters.RequireDerivedKeys = false;
            }
            else
            {
                throw new NotSupportedException(Constants.Pattern + " [" + scenario +
                    "] is not supported in " + Constants.WSO2InteropBinding);
            }

            return security;
        }

        private void ApplyConfiguration(string configurationName)
        {
            BindingsSection bindings = (BindingsSection)ConfigurationManager.GetSection(Constants.BindingSection);
            Wso2InteropBindingCollectionElement section = (Wso2InteropBindingCollectionElement)bindings[Constants.WSO2InteropBinding];
            Wso2InteropBindingElement element = section.Bindings[configurationName];
            if (element == null)
            {
                throw new System.Configuration.ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture, "There is no binding named {0} at {1}.", configurationName, section.BindingName));
            }
            else
            {
                element.ApplyConfiguration(this);
            }
        }
    }
}

