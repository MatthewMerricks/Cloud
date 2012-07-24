// <copyright file="WebLogicSecurityMode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.WebLogic
{
    /// <summary>
    /// Supported security modes for WebLogic
    /// </summary>
    public enum WebLogicSecurityMode
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

