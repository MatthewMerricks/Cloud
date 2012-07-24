// <copyright file="InteropSecurityMode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop
{
    /// <summary>
    /// Supported security modes
    /// </summary>
    public enum InteropSecurityMode
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

