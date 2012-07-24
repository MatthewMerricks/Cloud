// <copyright file="MetroSecurity.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Metro
{
    using System.ServiceModel;

    /// <summary>
    /// Security binding element for Metro
    /// </summary>
    public class MetroSecurity : InteropSecurity
    {
        private MetroSecurityMode mode;

        /// <summary>
        /// Initializes a new instance of the MetroSecurity class
        /// </summary>
        /// <param name="security">Security binding element</param>
        /// <param name="mode">Security Mode</param>
        public MetroSecurity(WSHttpSecurity security, MetroSecurityMode mode)
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
        public MetroSecurityMode Mode
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
            if (this.Mode == MetroSecurityMode.UserNameOverCertificate)
            {
                this.InnerSecurity.Message.ClientCredentialType = MessageCredentialType.UserName;
            }
            else if (this.Mode == MetroSecurityMode.MutualCertificate)
            {
                this.InnerSecurity.Message.ClientCredentialType = MessageCredentialType.Certificate;
            }
        }
    }
}

