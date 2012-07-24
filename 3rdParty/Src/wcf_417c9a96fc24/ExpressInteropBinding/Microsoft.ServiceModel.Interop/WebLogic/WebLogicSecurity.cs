// <copyright file="WebLogicSecurity.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.WebLogic
{
    using System.ServiceModel;

    /// <summary>
    /// Security binding element for WebLogic
    /// </summary>
    public class WebLogicSecurity : InteropSecurity
    {
        private WebLogicSecurityMode mode;

        /// <summary>
        /// Initializes a new instance of the WebLogicSecurity class
        /// </summary>
        /// <param name="security">Security binding element</param>
        /// <param name="mode">Security mode</param>
        public WebLogicSecurity(WSHttpSecurity security, WebLogicSecurityMode mode)
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
        public WebLogicSecurityMode Mode
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
            if (this.Mode == WebLogicSecurityMode.UserNameOverCertificate)
            {
                this.InnerSecurity.Message.ClientCredentialType = MessageCredentialType.UserName;
            }
            else if (this.Mode == WebLogicSecurityMode.MutualCertificate)
            {
                this.InnerSecurity.Message.ClientCredentialType = MessageCredentialType.Certificate;
            }
        }
    }
}

