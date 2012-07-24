// <copyright file="WebSphereSecurityMode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.WebSphere
{
    /// <summary>
    /// Supported security modes in WebSphere
    /// </summary>
    public enum WebSphereSecurityMode
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

