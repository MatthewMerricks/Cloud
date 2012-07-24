// <copyright file="MetroSecurityMode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Metro
{
    /// <summary>
    /// Supported security modes in Metro
    /// </summary>
    public enum MetroSecurityMode
    {
        /// <summary>
        /// Username credentials secured with a X509 certificate
        /// </summary>
        UserNameOverCertificate,

        /// <summary>
        /// X509 certificate client credentials secured with a X509 certificate
        /// </summary>
        MutualCertificate,
    }
}

