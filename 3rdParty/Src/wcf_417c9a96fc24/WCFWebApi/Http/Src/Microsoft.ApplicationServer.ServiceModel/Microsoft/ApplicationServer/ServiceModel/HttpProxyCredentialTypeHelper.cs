// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel
{
    using System;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;

    internal class HttpProxyCredentialTypeHelper
    {
        internal static AuthenticationSchemes MapToAuthenticationScheme(HttpProxyCredentialType proxyCredentialType)
        {
            switch (proxyCredentialType)
            {
                case HttpProxyCredentialType.None:
                    return AuthenticationSchemes.Anonymous;

                case HttpProxyCredentialType.Basic:
                    return AuthenticationSchemes.Basic;

                case HttpProxyCredentialType.Digest:
                    return AuthenticationSchemes.Digest;

                case HttpProxyCredentialType.Ntlm:
                    return AuthenticationSchemes.Ntlm;

                case HttpProxyCredentialType.Windows:
                    return AuthenticationSchemes.Negotiate;
            }

            Fx.Assert(false, "Invalid proxyCredentialType " + proxyCredentialType);
            return AuthenticationSchemes.Anonymous;
        }
    }
}
