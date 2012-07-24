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

    internal static class HttpTransportSecurityExtensionMethods
    {
        internal static void ConfigureTransportProtectionAndAuthentication(this HttpTransportSecurity httpTransportSecurity, HttpsTransportBindingElement httpsTransportBindingElement)
        {
            Fx.Assert(httpTransportSecurity != null, "httpTransportSecurity cannot be null");
            Fx.Assert(httpsTransportBindingElement != null, "httpsTransportBindingElement cannot be null");

            httpTransportSecurity.ConfigureAuthentication(httpsTransportBindingElement);
            httpsTransportBindingElement.RequireClientCertificate = httpTransportSecurity.ClientCredentialType == HttpClientCredentialType.Certificate;
        }

        internal static void ConfigureTransportAuthentication(this HttpTransportSecurity httpTransportSecurity, HttpTransportBindingElement httpTransportBindingElement)
        {
            Fx.Assert(httpTransportSecurity != null, "httpTransportSecurity cannot be null");
            Fx.Assert(httpTransportBindingElement != null, "httpTransportBindingElement cannot be null");

            if (httpTransportSecurity.ClientCredentialType == HttpClientCredentialType.Certificate)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SR.CertificateUnsupportedForHttpTransportCredentialOnly));
            }

            httpTransportSecurity.ConfigureAuthentication(httpTransportBindingElement);
        }

        internal static void DisableTransportAuthentication(this HttpTransportSecurity httpTransportSecurity, HttpTransportBindingElement httpTransportBindingElement)
        {
            Fx.Assert(httpTransportSecurity != null, "httpTransportSecurity cannot be null");
            Fx.Assert(httpTransportBindingElement != null, "httpTransportBindingElement cannot be null");

            httpTransportBindingElement.AuthenticationScheme = AuthenticationSchemes.Anonymous;
            httpTransportBindingElement.ProxyAuthenticationScheme = AuthenticationSchemes.Anonymous;
            httpTransportBindingElement.Realm = string.Empty;
            httpTransportBindingElement.ExtendedProtectionPolicy = httpTransportSecurity.ExtendedProtectionPolicy;
        }

        private static void ConfigureAuthentication(this HttpTransportSecurity httpTransportSecurity, HttpTransportBindingElement httpTransportBindingElement)
        {
            Fx.Assert(httpTransportSecurity != null, "httpTransportSecurity cannot be null");
            Fx.Assert(httpTransportBindingElement != null, "httpTransportBindingElement cannot be null");

            httpTransportBindingElement.AuthenticationScheme = HttpClientCredentialTypeHelper.MapToAuthenticationScheme(httpTransportSecurity.ClientCredentialType);
            httpTransportBindingElement.ProxyAuthenticationScheme = HttpProxyCredentialTypeHelper.MapToAuthenticationScheme(httpTransportSecurity.ProxyCredentialType);
            httpTransportBindingElement.Realm = httpTransportSecurity.Realm;
            httpTransportBindingElement.ExtendedProtectionPolicy = httpTransportSecurity.ExtendedProtectionPolicy;
        }
    }
}
