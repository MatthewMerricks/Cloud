// <copyright file="WebSphereSecurity.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.WebSphere
{
    using System.ServiceModel;

    /// <summary>
    /// Security binding implementation for WebSphere
    /// </summary>
    public class WebSphereSecurity : InteropSecurity
    {
        private WebSphereSecurityMode mode;

        /// <summary>
        /// Initializes a new instance of the WebSphereSecurity class
        /// </summary>
        /// <param name="security">Security binding element</param>
        /// <param name="mode">Security mode</param>
        public WebSphereSecurity(WSHttpSecurity security, WebSphereSecurityMode mode)
            : base(security)
        {
            this.InnerSecurity.Mode = SecurityMode.Message;
            this.InnerSecurity.Message.NegotiateServiceCredential = false;

            this.mode = mode;

            this.ConfigureSecurity();
        }

        /// <summary>
        /// Gets or sets the security mode
        /// </summary>
        public WebSphereSecurityMode Mode
        {
            get
            {
                return this.mode;
            }

            set
            {
                this.mode = value;

                this.ConfigureSecurity();
            }
        }

        private void ConfigureSecurity()
        {
            if (this.Mode == WebSphereSecurityMode.UserNameOverCertificate)
            {
                this.InnerSecurity.Message.ClientCredentialType = MessageCredentialType.UserName;
            }
            else if (this.Mode == WebSphereSecurityMode.MutualCertificate)
            {
                this.InnerSecurity.Message.ClientCredentialType = MessageCredentialType.Certificate;
            }
        }
    }
}

