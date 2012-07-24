// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Globalization;
    using System.Linq;
    using System.ServiceModel;
    using System.ServiceModel.Configuration;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Configuration;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.ServiceModel;
    using Microsoft.ApplicationServer.Common.Test.Services;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using System.Security.Authentication.ExtendedProtection;

    [TestClass]
    public class HttpBindingElementTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBindingElement ctor initializes all properties to known defaults")]
        public void HttpBindingElement_Ctor_Initializes_All_Properties()
        {
            HttpBindingElement element = new HttpBindingElement();
            HttpBinding binding = new HttpBinding();

            Assert.AreEqual(binding.CloseTimeout, element.CloseTimeout, "The HttpBinding and HttpBindingElement should have the same default CloseTimeout");
            Assert.AreEqual(binding.HostNameComparisonMode, element.HostNameComparisonMode, "The HttpBinding and HttpBindingElement should have the same default HostNameComparisonMode");
            Assert.AreEqual(binding.MaxBufferPoolSize, element.MaxBufferPoolSize, "The HttpBinding and HttpBindingElement should have the same default MaxBufferPoolSize");
            Assert.AreEqual(binding.MaxBufferSize, element.MaxBufferSize, "The HttpBinding and HttpBindingElement should have the same default MaxBufferSize");
            Assert.AreEqual(binding.MaxReceivedMessageSize, element.MaxReceivedMessageSize, "The HttpBinding and HttpBindingElement should have the same default MaxReceivedMessageSize");
            Assert.AreEqual(binding.OpenTimeout, element.OpenTimeout, "The HttpBinding and HttpBindingElement should have the same default OpenTimeout");
            Assert.AreEqual(binding.ReceiveTimeout, element.ReceiveTimeout, "The HttpBinding and HttpBindingElement should have the same default ReceiveTimeout");
            Assert.AreEqual(binding.SendTimeout, element.SendTimeout, "The HttpBinding and HttpBindingElement should have the same default ReceiveTimeout");
            Assert.AreEqual(binding.TransferMode, element.TransferMode, "The HttpBinding and HttpBindingElement should have the same default TransferMode");        
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("The HttpBinding constructor throws with an unknown configurationName parameter.")]
        [DeploymentItem("ConfigFiles\\Microsoft.ApplicationServer.Http.CIT.Unit.ConfiguredHttpBindingTest.config")]
        public void HttpBinding_Throws_With_Unknown_ConfigurationName_Parameter()
        {
            ConfigAssert.Execute("Microsoft.ApplicationServer.Http.CIT.Unit.ConfiguredHttpBindingTest.config", () =>
            {
                ExceptionAssert.Throws<ConfigurationErrorsException>(
                    null,
                    () =>
                    {
                        new HttpBinding("noSuchConfiguredBinding");
                    });
            });
        }

        [Ignore]
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("The HttpBinding constructor is correctly configured when the binding configuration exists.")]
        [DeploymentItem("ConfigFiles\\Microsoft.ApplicationServer.Http.CIT.Unit.ConfiguredHttpBindingTest.config")]
        public void HttpBinding_Correctly_Configured_With_Name_Configuration()
        {
            ConfigAssert.Execute("Microsoft.ApplicationServer.Http.CIT.Unit.ConfiguredHttpBindingTest.config", () =>
            {
                HttpBinding binding = new HttpBinding("configuredBinding");
                Assert.AreEqual(HostNameComparisonMode.Exact, binding.HostNameComparisonMode, "Binding.HostNameComparisonMode should have been HostNameComparisonMode.Exact.");
                Assert.AreEqual(500, binding.MaxBufferPoolSize, "Binding.MaxBufferPoolSize should have been 500.");
                Assert.AreEqual(100, binding.MaxReceivedMessageSize, "Binding.MaxReceivedMessageSize should have been 100.");
                Assert.AreEqual(200, binding.MaxBufferSize, "Binding.MaxBufferSize should have been 200.");
                Assert.AreEqual(TransferMode.StreamedResponse, binding.TransferMode, "Binding.TransferMode should have been TransferMode.StreamedResponse.");
                Assert.AreEqual(HttpBindingSecurityMode.Transport, binding.Security.Mode, "Binding.Security.Mode should have been HttpBindingSecurityMode.Transport.");
                Assert.AreEqual("someConfigRealm", binding.Security.Transport.Realm, "Binding.Security.Transport.Realm should have been 'someConfigRealm'.");
                Assert.AreEqual(HttpClientCredentialType.Basic, binding.Security.Transport.ClientCredentialType, "Binding.Security.Transport.ClientCredentialType should have been HttpClientCredentialType.Basic.");
                Assert.AreEqual(HttpProxyCredentialType.Ntlm, binding.Security.Transport.ProxyCredentialType, "Binding.Security.Transport.ProxyCredentialType should have been HttpProxyCredentialType.Ntlm.");
                Assert.AreEqual(PolicyEnforcement.WhenSupported, binding.Security.Transport.ExtendedProtectionPolicy.PolicyEnforcement, "Binding.Transport.ExtendedProtectionPolicy.PolicyEnforcement should have been PolicyEnforcement.WhenSupported");
            });
        }

        [Ignore]
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("The HttpBinding constructor is correctly configured when the binding configuration exists but has an empty string name.")]
        [DeploymentItem("ConfigFiles\\Microsoft.ApplicationServer.Http.CIT.Unit.ConfiguredHttpBindingTest.config")]
        public void HttpBinding_Correctly_Configured_With_Empty_Name_Configuration()
        {
            ConfigAssert.Execute("Microsoft.ApplicationServer.Http.CIT.Unit.ConfiguredHttpBindingTest.config", () =>
            {
                HttpBinding binding = new HttpBinding("");
                Assert.AreEqual(HostNameComparisonMode.WeakWildcard, binding.HostNameComparisonMode, "Binding.HostNameComparisonMode should have been HostNameComparisonMode.WeakWildcard.");
            });
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("The HttpBinding constructor throws when there is no config file for the AppDomain.")]
        public void HttpBinding_Throws_When_No_Config_File()
        {
            ExceptionAssert.Throws<ConfigurationErrorsException>(
                Http.SR.ConfigInvalidBindingConfigurationName("configuredBinding", "httpBinding"),
                () => new HttpBinding("configuredBinding"));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("The HttpBinding constructor throws when there is no binding section in the config file.")]
        [DeploymentItem("ConfigFiles\\Microsoft.ApplicationServer.Http.CIT.Unit.EmptyConfigurationTest.config")]
        public void HttpBinding_Throws_When_No_Binding_Section()
        {
            ConfigAssert.Execute("Microsoft.ApplicationServer.Http.CIT.Unit.EmptyConfigurationTest.config", () =>
            {
                ExceptionAssert.Throws<ConfigurationErrorsException>(null, () => new HttpBinding("configuredBinding"));
            });
        }
    }
}
