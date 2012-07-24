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

    internal static class HttpClientCredentialTypeHelper
    {
        internal static AuthenticationSchemes MapToAuthenticationScheme(HttpClientCredentialType clientCredentialType)
        {
            switch (clientCredentialType)
            {
                case HttpClientCredentialType.None:
                case HttpClientCredentialType.Certificate:
                    return AuthenticationSchemes.Anonymous;

                case HttpClientCredentialType.Basic:
                    return AuthenticationSchemes.Basic;

                case HttpClientCredentialType.Digest:
                    return AuthenticationSchemes.Digest;

                case HttpClientCredentialType.Ntlm:
                    return AuthenticationSchemes.Ntlm;

                case HttpClientCredentialType.Windows:
                    return AuthenticationSchemes.Negotiate;
            }

            Fx.Assert(false, "Invalid clientCredentialType " + clientCredentialType);
            return AuthenticationSchemes.Anonymous;
        }
    }
}

