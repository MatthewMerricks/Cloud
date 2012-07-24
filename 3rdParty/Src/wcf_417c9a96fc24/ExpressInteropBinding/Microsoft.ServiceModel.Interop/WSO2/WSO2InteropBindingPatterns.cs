// <copyright file="WSO2InteropBindingPatterns.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Wso2
{
    /// <summary>
    /// Supported security patterns
    /// </summary>
    internal static class Wso2InteropBindingPatters
    {
        internal const string UserNameOverTransport = "UserNameOverTransport";
        internal const string MutualCertificateDuplex = "MutualCertificateDuplex";
        internal const string UserNameForCertificate = "UserNameForCertificate";
        internal const string AnonymousForCertificate = "AnonymousForCertificate";
        internal const string SecureConversation = "SecureConversation";
        internal const string Kerberos = "Kerberos";
    }
}
