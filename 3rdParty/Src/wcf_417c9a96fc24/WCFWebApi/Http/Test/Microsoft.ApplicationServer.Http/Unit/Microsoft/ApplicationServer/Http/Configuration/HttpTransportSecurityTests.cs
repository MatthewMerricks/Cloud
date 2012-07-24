// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Configuration
{
    using System;
    using System.Net;
    using System.Security.Authentication.ExtendedProtection;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.ServiceModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpTransportSecurityTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpTransportSecurity.ConfigureTransportAuthentication sets HttpTransportBindingElement")]
        public void HttpTransportSecurity_ConfigureTransportAuthentication()
        {
            ExtendedProtectionPolicy policy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
            HttpTransportSecurity security = new HttpTransportSecurity()
                                             {
                                                ClientCredentialType = HttpClientCredentialType.Basic,
                                                ProxyCredentialType = HttpProxyCredentialType.Basic,
                                                Realm = "MyRealm",
                                                ExtendedProtectionPolicy = policy
                                             };
            HttpTransportBindingElement binding = new HttpTransportBindingElement();


            security.ConfigureTransportAuthentication(binding);

            Assert.AreEqual(AuthenticationSchemes.Basic, binding.AuthenticationScheme, "AuthenticationScheme failed to init");
            Assert.AreEqual(AuthenticationSchemes.Basic, binding.ProxyAuthenticationScheme, "ProxyAuthenticationScheme failed to init");
            Assert.AreEqual("MyRealm", binding.Realm, "Realm failed to init");
            Assert.AreEqual(policy, binding.ExtendedProtectionPolicy, "ExtendedProtectionPolicy failed to init");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpTransportSecurity.ConfigureTransportAuthentication sets HttpsTransportBindingElement")]
        public void HttpTransportSecurity_ConfigureTransportAuthentication_Https()
        {
            ExtendedProtectionPolicy policy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
            HttpTransportSecurity security = new HttpTransportSecurity()
            {
                ClientCredentialType = HttpClientCredentialType.Basic,
                ProxyCredentialType = HttpProxyCredentialType.Basic,
                Realm = "MyRealm",
                ExtendedProtectionPolicy = policy
            };
            HttpsTransportBindingElement binding = new HttpsTransportBindingElement();


            security.ConfigureTransportAuthentication(binding);

            Assert.AreEqual(AuthenticationSchemes.Basic, binding.AuthenticationScheme, "AuthenticationScheme failed to init");
            Assert.AreEqual(AuthenticationSchemes.Basic, binding.ProxyAuthenticationScheme, "ProxyAuthenticationScheme failed to init");
            Assert.AreEqual("MyRealm", binding.Realm, "Realm failed to init");
            Assert.AreEqual(policy, binding.ExtendedProtectionPolicy, "ExtendedProtectionPolicy failed to init");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpTransportSecurity.DisableTransportAuthentication sets HttpsTransportBindingElement")]
        public void HttpTransportSecurity_DisableTransportAuthentication()
        {
            ExtendedProtectionPolicy policy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
            HttpTransportSecurity security = new HttpTransportSecurity()
            {
                ClientCredentialType = HttpClientCredentialType.Basic,
                ProxyCredentialType = HttpProxyCredentialType.Basic,
                Realm = "MyRealm",
                ExtendedProtectionPolicy = policy
            };
            HttpTransportBindingElement binding = new HttpTransportBindingElement();

            // first configure it
            security.ConfigureTransportAuthentication(binding);

            Assert.AreEqual(AuthenticationSchemes.Basic, binding.AuthenticationScheme, "AuthenticationScheme failed to init");
            Assert.AreEqual(AuthenticationSchemes.Basic, binding.ProxyAuthenticationScheme, "ProxyAuthenticationScheme failed to init");
            Assert.AreEqual("MyRealm", binding.Realm, "Realm failed to init");
            Assert.AreEqual(policy, binding.ExtendedProtectionPolicy, "ExtendedProtectionPolicy failed to init");

            // then disable it
            security.DisableTransportAuthentication(binding);

            Assert.AreEqual(AuthenticationSchemes.Anonymous, binding.AuthenticationScheme, "AuthenticationScheme failed to init");
            Assert.AreEqual(AuthenticationSchemes.Anonymous, binding.ProxyAuthenticationScheme, "ProxyAuthenticationScheme failed to init");
            Assert.AreEqual(string.Empty, binding.Realm, "Realm failed to init");
            Assert.AreEqual(policy, binding.ExtendedProtectionPolicy, "ExtendedProtectionPolicy failed to init");
        }
    }
}
