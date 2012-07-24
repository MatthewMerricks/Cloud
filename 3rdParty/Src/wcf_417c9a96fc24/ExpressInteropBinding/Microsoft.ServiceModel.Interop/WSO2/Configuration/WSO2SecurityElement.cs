// <copyright file="WSO2InteropBindingElement.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Wso2.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;
    using System.Text;

    /// <summary>
    /// Security binding element for Apache Axis 2
    /// </summary>
    public class Wso2SecurityElement : BindingElementExtensionElement
    {
        /// <summary>
        /// Initializes a new instance of the Wso2SecurityElement class
        /// </summary>
        public Wso2SecurityElement()
        {
            this.Pattern = Constants.DefaultPattern;
        }

        /// <summary>
        /// Initializes a new instance of the Wso2SecurityElement class
        /// </summary>
        /// <param name="pattern">Security pattern</param>
        public Wso2SecurityElement(string pattern)
        {
            this.Pattern = pattern;
        }

        /// <summary>
        /// Gets the binding element implementation type
        /// </summary>
        public override Type BindingElementType
        {
            get
            {
                return typeof(Wso2InteropBinding);
            }
        }

        /// <summary>
        /// Gets or sets the security pattern
        /// </summary>
        [ConfigurationProperty(Constants.Pattern, DefaultValue = Constants.DefaultPattern)]
        public string Pattern
        {
            get
            {
                return (string)base[Constants.Pattern];
            }

            set
            {
                base[Constants.Pattern] = value;
            }
        }

        /// <summary>
        /// Gets or sets the security pattern for secure conversation
        /// </summary>
        [ConfigurationProperty(Constants.SecureConversationBootstrap, DefaultValue = Constants.DefaultBootstrap)]
        public string SecureConversationBootstrap
        {
            get
            {
                return (string)base[Constants.SecureConversationBootstrap];
            }

            set
            {
                base[Constants.SecureConversationBootstrap] = value;
            }
        }

        /// <summary>
        /// Creates an instance of the associated binding element
        /// </summary>
        /// <returns>Binding element instance</returns>
        protected override BindingElement CreateBindingElement()
        {
            return null;
        }
    }
}
