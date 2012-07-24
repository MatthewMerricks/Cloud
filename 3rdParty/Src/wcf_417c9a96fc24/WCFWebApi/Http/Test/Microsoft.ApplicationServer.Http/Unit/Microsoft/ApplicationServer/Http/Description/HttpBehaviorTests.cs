// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Web;
    using System.Threading;
    using System.Xml.Serialization;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Services;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Runtime.Serialization.Json;
    using System.Collections.ObjectModel;
    using Microsoft.ApplicationServer.Common.Test.Types;
    using Microsoft.ApplicationServer.Http.Test;

    [TestClass]
    public class HttpBehaviorTests
    {
        private static MediaTypeWithQualityHeaderValue XmlMediaType = new MediaTypeWithQualityHeaderValue("application/xml");
        private static MediaTypeWithQualityHeaderValue JsonMediaType = new MediaTypeWithQualityHeaderValue("application/json");
        private static MediaTypeWithQualityHeaderValue TextXmlMediaType = new MediaTypeWithQualityHeaderValue("text/xml");
        private static MediaTypeWithQualityHeaderValue TextJsonMediaType = new MediaTypeWithQualityHeaderValue("text/json");
        private static MediaTypeWithQualityHeaderValue MediaRangeMediaType = new MediaTypeWithQualityHeaderValue("*/*");
        private static int portNumber = 1000;
        private static ConcurrentDictionary<Type, Tuple<ServiceHost, string>> serviceTypeToHttpServiceHostMapping = new ConcurrentDictionary<Type,Tuple<ServiceHost,string>>();
        private static ConcurrentDictionary<Type, Tuple<ServiceHost, string>> serviceTypeToWebHttpServiceHostMapping = new ConcurrentDictionary<Type,Tuple<ServiceHost,string>>(); 

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            foreach (Type key in serviceTypeToHttpServiceHostMapping.Keys)
            {
                Tuple<ServiceHost, string> serviceHostData = null;
                if (serviceTypeToHttpServiceHostMapping.TryRemove(key, out serviceHostData))
                {
                    serviceHostData.Item1.Close();
                }
            }

            foreach (Type key in serviceTypeToWebHttpServiceHostMapping.Keys)
            {
                Tuple<ServiceHost, string> serviceHostData = null;
                if (serviceTypeToWebHttpServiceHostMapping.TryRemove(key, out serviceHostData))
                {
                    serviceHostData.Item1.Close();
                }
            }
        }

        #region Constructor Tests
        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior ctor sets defaults")]
        public void HttpBehavior_Ctor_Defaults()
        {
            HttpBehavior behavior = new HttpBehavior();
            Assert.AreEqual(TrailingSlashMode.AutoRedirect, behavior.TrailingSlashMode, "TrailingSlashMode");
            Assert.IsFalse(behavior.HelpEnabled, "HelpEnabled");
        } 

        #endregion

        #region TrailingSlashMode Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.TrailingSlashMode throws for illegal values")]
        public void TrailingSlashMode_Throws_If_Set_To_Invalid_Value()
        {
            HttpBehavior behavior = new HttpBehavior();

            ExceptionAssert.Throws<ArgumentOutOfRangeException>(
                "TrailingSlashMode should throw if invalid value set",
                () => behavior.TrailingSlashMode = (TrailingSlashMode)99,
                (e) => Assert.AreEqual("value", e.ParamName));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.TrailingSlashMode will redirect by default when the request differs by just a trailing slash")]
        public void TrailingSlashMode_Redirects_By_Default()
        {
            HttpBehavior httpBehavior = new HttpBehavior();

            string actualBaseAddress = null;
            ServiceHost actualHost = GetServiceHost(typeof(TrailingSlashModeService), httpBehavior, new HttpBinding(), out actualBaseAddress);

            using (HttpClient client = new HttpClient())
            {
                client.Channel = new WebRequestChannel();
                using (HttpResponseMessage actualResponse = client.Get(actualBaseAddress + "/"))
                {
                    Assert.AreEqual(actualResponse.RequestMessage.RequestUri.ToString(), actualBaseAddress, "The server should have redirected to the address without the trailing slash.");
                }
            }

            actualHost.Close();        
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.TrailingSlashMode will dispatch if set to 'Ignore'")]
        public void TrailingSlashMode_Dispatches_If_Set_To_Ignore()
        {
            HttpBehavior httpBehavior = new HttpBehavior();
            httpBehavior.TrailingSlashMode = TrailingSlashMode.Ignore;

            string actualBaseAddress = null;
            ServiceHost actualHost = GetServiceHost(typeof(TrailingSlashModeService), httpBehavior, new HttpBinding(), out actualBaseAddress);

            using (HttpClient client = new HttpClient())
            {
                client.Channel = new WebRequestChannel();
                using (HttpResponseMessage actualResponse = client.Get(actualBaseAddress + "/"))
                {
                    Assert.AreEqual(actualResponse.RequestMessage.RequestUri.ToString(), actualBaseAddress + "/", "The server should have dispatched without redirecting.");
                }
            }

            actualHost.Close();
        }

        #endregion TrailingSlashMode Tests

        #region ApplyClientBehavior Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.ApplyClientBehavior throws")]
        public void HttpBehavior_ApplyClientBehavior_Throws()
        {
            HttpBehavior behavior = new HttpBehavior();
            ContractDescription cd = ContractDescription.GetContract(typeof(CustomerService));
            ServiceEndpoint endpoint = new ServiceEndpoint(cd);
            EndpointDispatcher dispatcher = new EndpointDispatcher(new EndpointAddress("http://someuri"), cd.Name, cd.Namespace);

            ExceptionAssert.Throws<NotSupportedException>(
                "ApplyClientBehavior throws always",
                Http.SR.ApplyClientBehaviorNotSupportedByHttpBehavior(typeof(HttpBehavior).Name),
                () => ((IEndpointBehavior)behavior).ApplyClientBehavior(null, null));
        }

        #endregion ApplyClientBehavior Tests

        #region ApplyDispatchBehavior Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.ApplyDispatchBehavior with null endpoint throws.")]
        public void HttpBehavior_ApplyDispatchBehavior_Throws_For_Null_Endpoint()
        {
            HttpBehavior behavior = new HttpBehavior();
            ContractDescription cd = ContractDescription.GetContract(typeof(CustomerService));
            ServiceEndpoint endpoint = new ServiceEndpoint(cd);
            EndpointDispatcher dispatcher = new EndpointDispatcher(new EndpointAddress("http://someuri"), cd.Name, cd.Namespace);

            ExceptionAssert.ThrowsArgumentNull("endpoint", () => behavior.ApplyDispatchBehavior(null, dispatcher));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.ApplyDispatchBehavior with null endpointDispatcher parameter throws.")]
        public void HttpBehavior_ApplyDispatchBehavior_Throws_For_Null_EndpointDispatcher()
        {
            HttpBehavior behavior = new HttpBehavior();
            ContractDescription cd = ContractDescription.GetContract(typeof(CustomerService));
            ServiceEndpoint endpoint = new ServiceEndpoint(cd);
            EndpointDispatcher dispatcher = new EndpointDispatcher(new EndpointAddress("http://someuri"), cd.Name, cd.Namespace);

            ExceptionAssert.ThrowsArgumentNull("endpointDispatcher", () => behavior.ApplyDispatchBehavior(endpoint, null));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.ApplyDispatchBehavior does not add help operations.")]
        public void HttpBehavior_Test()
        {
            ContractDescription cd = ContractDescription.GetContract(typeof(CustomerService));
            OperationDescription d1 = new OperationDescription("Name", cd);
            OperationDescription d2 = new OperationDescription("Name", cd);

            cd.Operations.Add(d1);
            cd.Operations.Add(d2);

            OperationDescription d3 = cd.Operations.Find("Name");
        }

        #endregion

        #region Validate Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate does not throw for a valid HttpBinding.")]
        public void Validate_Does_Not_Throw_For_HttpBinding()
        {
            HttpBehavior behavior = new HttpBehavior();
            ServiceEndpoint endpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(CustomerService)));
            endpoint.Binding = new HttpBinding();
            endpoint.Address = new EndpointAddress("http://somehost");
            behavior.Validate(endpoint);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if the 'endpoint' parameter is null.")]
        public void Validate_Throws_With_Null_Endpoint()
        {
            HttpBehavior behavior = new HttpBehavior();

            ExceptionAssert.ThrowsArgumentNull("endpoint", () => behavior.Validate(null));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if the endpoint binding is null.")]
        public void Validate_Throws_With_Null_Binding_On_Endpoint()
        {
            HttpBehavior behavior = new HttpBehavior();
            ServiceEndpoint endpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(CustomerService)));

            ExceptionAssert.Throws<InvalidOperationException>(
                "Validate should throw for null binding",
                Http.SR.HttpBehaviorBindingRequired(typeof(HttpBehavior).Name),
                () => behavior.Validate(endpoint));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if the endpoint address has headers.")]
        public void Validate_Throws_With_Endpoint_Address_Headers()
        {
            HttpBehavior behavior = new HttpBehavior();
            ServiceEndpoint endpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(CustomerService)));
            endpoint.Binding = new HttpBinding();
            AddressHeader[] headers = new AddressHeader[] { AddressHeader.CreateAddressHeader("hello") };
            endpoint.Address = new EndpointAddress(new Uri("http://somehost"), headers);
            ExceptionAssert.Throws<InvalidOperationException>(
                "Address with headers should throw",
                Http.SR.HttpServiceEndpointCannotHaveMessageHeaders(endpoint.Address),
                () => behavior.Validate(endpoint));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if the endpoint binding is not http.")]
        public void Validate_Throws_With_Non_Http_Binding()
        {
            HttpBehavior behavior = new HttpBehavior();
            ServiceEndpoint endpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(CustomerService)));
            NetTcpBinding netTcpBinding = new NetTcpBinding();
            endpoint.Binding = netTcpBinding;
            endpoint.Address = new EndpointAddress("http://somehost");
            ExceptionAssert.Throws<InvalidOperationException>(
                 "Non-Http Binding should throw",
                 Http.SR.InvalidUriScheme(endpoint.Address.Uri.AbsoluteUri),
                 () => behavior.Validate(endpoint));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if the MessageEncodingBindingElement is not HttpMessageEncodingBindingElement.")]
        public void Validate_Throws_With_Non_HttpMessageEncodingBindingElement()
        {
            HttpBehavior behavior = new HttpBehavior();
            ServiceEndpoint endpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(CustomerService)));
            WebHttpBinding webHttpBinding = new WebHttpBinding();
            endpoint.Binding = webHttpBinding;
            endpoint.Address = new EndpointAddress("http://somehost");

            ExceptionAssert.Throws<InvalidOperationException>(
                "Non-HttpMessageEncodingBindingElement should throw",
                Http.SR.InvalidMessageEncodingBindingElement(
                    endpoint.Address.Uri.AbsoluteUri,
                    typeof(MessageEncodingBindingElement).Name,
                    typeof(HttpMessageEncodingBindingElement).Name),
                () => behavior.Validate(endpoint));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if the transport does not support manual addressing.")]
        public void Validate_Throws_With_Non_ManualAddressing()
        {
            HttpBehavior behavior = new HttpBehavior();
            ServiceEndpoint endpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(CustomerService)));
            BindingElementCollection bindingElements = new HttpBinding().CreateBindingElements();
            (bindingElements[bindingElements.Count - 1] as HttpTransportBindingElement).ManualAddressing = false;
            endpoint.Binding = new CustomBinding(bindingElements);
            endpoint.Address = new EndpointAddress("http://somehost");
            ExceptionAssert.Throws<InvalidOperationException>(
                 "Non-manual addressing should throw",
                 Http.SR.InvalidManualAddressingValue(endpoint.Address.Uri.AbsoluteUri),
                 () => behavior.Validate(endpoint));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if the MessageVersion is not MessageVersion.None.")]
        public void Validate_Throws_With_Non_MessageVersion_None()
        {
            HttpBehavior behavior = new HttpBehavior();
            ServiceEndpoint endpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(CustomerService)));
            endpoint.Binding = new BasicHttpBinding();
            endpoint.Address = new EndpointAddress("http://somehost");
            ExceptionAssert.Throws<InvalidOperationException>(
                 "Non-MessageVersion.None should throw",
                 Http.SR.InvalidMessageVersion(endpoint.Address.Uri.AbsoluteUri),
                 () => behavior.Validate(endpoint));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if any operation has an XmlSerializerFormat attribute with the FormatStyle set to RPC.")]
        public void Validate_Throws_If_XmlSerializerFormat_With_Rpc_Format_Style()
        {
            HttpBehavior behavior = new HttpBehavior();

            ContractDescription description = ContractDescription.GetContract(typeof(CustomerService));
            ServiceEndpoint endpoint = new ServiceEndpoint(description);
            endpoint.Binding = new HttpBinding();
            endpoint.Address = new EndpointAddress("http://somehost");

            OperationDescription od = description.Operations[0];
            XmlSerializerFormatAttribute attr = new XmlSerializerFormatAttribute() { Style = OperationFormatStyle.Rpc };
            od.Behaviors.Add(new XmlSerializerOperationBehavior(od, attr));

            ExceptionAssert.Throws<InvalidOperationException>(
                 "XmlSerializerFormat with RPC should throw",
                 Http.SR.InvalidXmlSerializerFormatAttribute(
                    od.Name,
                    od.DeclaringContract.Name,
                    typeof(XmlSerializerFormatAttribute).Name),
                 () => behavior.Validate(endpoint));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if any operation has a request message with headers.")]
        public void Validate_Throws_If_Request_Message_Headers_Present()
        {
            HttpBehavior behavior = new HttpBehavior();

            ContractDescription description = ContractDescription.GetContract(typeof(CustomerService));
            ServiceEndpoint endpoint = new ServiceEndpoint(description);
            endpoint.Binding = new HttpBinding();
            endpoint.Address = new EndpointAddress("http://somehost");

            OperationDescription od = description.Operations[0];
            od.Messages[0].Headers.Add(new MessageHeaderDescription("someName", "someNamespace"));
            ExceptionAssert.Throws<InvalidOperationException>(
                 "An operation with request message headers should throw",
                 Http.SR.InvalidOperationWithMessageHeaders(od.Name, od.DeclaringContract.Name),
                 () => behavior.Validate(endpoint));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if any operation has a response message with headers.")]
        public void Validate_Throws_If_Response_Message_Headers_Present()
        {
            HttpBehavior behavior = new HttpBehavior();

            ContractDescription description = ContractDescription.GetContract(typeof(CustomerService));
            ServiceEndpoint endpoint = new ServiceEndpoint(description);
            endpoint.Binding = new HttpBinding();
            endpoint.Address = new EndpointAddress("http://somehost");

            OperationDescription od = description.Operations[0];
            od.Messages[1].Headers.Add(new MessageHeaderDescription("someName", "someNamespace"));

            ExceptionAssert.Throws<InvalidOperationException>(
                 "An operation with response message headers should throw",
                 Http.SR.InvalidOperationWithMessageHeaders(od.Name, od.DeclaringContract.Name),
                 () => behavior.Validate(endpoint));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if any operation has a request with a typed message.")]
        public void Validate_Throws_If_Any_Operation_Has_Typed_Message_Request()
        {
            string throwAway = null;

            ExceptionAssert.Throws<InvalidOperationException>(
                "An operation with a request with a typed message should throw",
                Http.SR.InvalidMessageContractParameter(
                    "TypedMessageOperation", 
                    "TypedMessageService1",
                    typeof(MessageContractAttribute).Name,
                    typeof(TypedMessage).Name),
                () => GetHttpServiceHost(typeof(TypedMessageService1), out throwAway));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if any operation has a response with a typed message.")]
        public void Validate_Throws_If_Any_Operation_Has_Typed_Message_Response()
        {
            string throwAway = null;

            ExceptionAssert.Throws<InvalidOperationException>(
                "An operation with a request with a typed message should throw",
                Http.SR.InvalidMessageContractParameter(
                    "TypedMessageOperation",
                    "TypedMessageService2",
                    typeof(MessageContractAttribute).Name,
                    typeof(TypedMessage).Name),
                () => GetHttpServiceHost(typeof(TypedMessageService2), out throwAway));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior.Validate throws if any operation has both a WebGet and WebInvoke attribute.")]
        public void Validate_Throws_If_Any_Operation_Has_Both_WebGet_And_WebInvoke()
        {
            string throwAway = null;
            ExceptionAssert.Throws<InvalidOperationException>(
                "An operation with both WebGet and WebInvoke attributes should throw",
                Http.SR.MultipleWebAttributes(
                    "WebGetAndInvokeOperation",
                    typeof(WebGetAndInvokeService).Name,
                    typeof(WebGetAttribute).Name,
                    typeof(WebInvokeAttribute).Name),
                 () => GetHttpServiceHost(typeof(WebGetAndInvokeService), out throwAway));
        }

        #endregion Validate Tests

        #region Back Compat Content Negotiation Tests

        [Ignore]
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test that the default reponse format is Xml.")]
        public void Default_Response_Format_Is_Xml()
        {
            string relativeUri = "/GetCustomer/SomeName";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(CustomerService), actualRequest))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(CustomerService), expectedRequest))
                {
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test that the accept header overrides the default Xml.")]
        public void Accept_Header_Overrides_Default_Format()
        {
            string relativeUri = "/GetCustomer/SomeName";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            actualRequest.Headers.Accept.Add(JsonMediaType);
            expectedRequest.Headers.Accept.Add(JsonMediaType);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(CustomerService), actualRequest))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(CustomerService), expectedRequest))
                {
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test that the accept header overrides the operation ResponseFormat.")]
        public void Accept_Header_Overrides_Operation_Response_Format()
        {
            string relativeUri = "/GetCustomerAsJson/SomeName";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            actualRequest.Headers.Accept.Add(XmlMediaType);
            expectedRequest.Headers.Accept.Add(XmlMediaType);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(CustomerService), actualRequest))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(CustomerService), expectedRequest))
                {
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test that the accept header text/json overrides the default Xml.")]
        public void Accept_Header_Text_Json_Overrides_Default_Format()
        {
            string relativeUri = "/GetCustomer/SomeName";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            actualRequest.Headers.Accept.Add(TextJsonMediaType);
            expectedRequest.Headers.Accept.Add(TextJsonMediaType);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(CustomerService), actualRequest))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(CustomerService), expectedRequest))
                {
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test that the accept header text/xml overrides the operation ResponseFormat.")]
        public void Accept_Header_Text_Xml_Overrides_Operation_Response_Format()
        {
            string relativeUri = "/GetCustomerAsJson/SomeName";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            actualRequest.Headers.Accept.Add(TextXmlMediaType);
            expectedRequest.Headers.Accept.Add(TextXmlMediaType);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(CustomerService), actualRequest))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(CustomerService), expectedRequest))
                {
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        #endregion Back Compat Content Negotiation Tests

        #region Back Compat Error Handler Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test that the default reponse is html for general exceptions.")]
        public void Error_Handler_Default_Is_Html_For_General_Exceptions()
        {
            string relativeUri = "/GetThrows";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(ExceptionService), actualRequest))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(ExceptionService), expectedRequest))
                {
                    // Add the charset to the expectedResponse contentType because the old stack was not consistent
                    expectedResponse.Content.Headers.ContentType.CharSet = "utf-8";

                    // The new stack returns InternalServerError
                    expectedResponse.StatusCode = HttpStatusCode.InternalServerError;
                    expectedResponse.ReasonPhrase = "Internal Server Error";

                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test that the operation XmlSerializerFormatAtttribute is honored for WebFaultExceptions.")]
        public void Error_Handler_Honors_XmlSerializerFormat_On_Operation()
        {
            string relativeUri = "/GetThrowsWebFaultAsXmlSerializable/SomeName";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(ExceptionService), actualRequest))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(ExceptionService), expectedRequest))
                {
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        #endregion Back Compat Error Handler Tests

        #region Back Compat Operation Selection Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test for unknown endpoint.")]
        public void Unknown_Uri_Is_Handled()
        {
            string relativeUri = "/SomeUnknownUri";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(CustomerService), actualRequest))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(CustomerService), expectedRequest))
                {
                    // Patch the charset on the expectedResponse contentType because the old stack's casing was not consistent
                    expectedResponse.Content.Headers.ContentType.CharSet = "utf-8";
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test for unknown endpoint when the request uses the machine name and not 'localhost'.")]
        public void Unknown_Uri_Is_Handled_With_Machine_Name()
        {
            string relativeUri = "/SomeUnknownUri";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(CustomerService), actualRequest, useMachineNameWithRequest: true))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(CustomerService), expectedRequest, useMachineNameWithRequest: true))
                {
                    // Patch the charset on the expectedResponse contentType because the old stack's casing was not consistent
                    expectedResponse.Content.Headers.ContentType.CharSet = "utf-8";
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test for method not allowed.")]
        public void Method_Not_Allowed_Is_Handled()
        {
            string relativeUri = "/GetCustomer/SomeName";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Post, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Post, relativeUri);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(CustomerService), actualRequest))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(CustomerService), expectedRequest))
                {
                    // Patch the charset on the expectedResponse contentType because the old stack's casing was not consistent
                    expectedResponse.Content.Headers.ContentType.CharSet = "utf-8";
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test for method not allowed when the request uses the machine name and not 'localhost'.")]
        public void Method_Not_Allowed_Is_Handled_With_Machine_Name()
        {
            string relativeUri = "/GetCustomer/SomeName";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Post, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Post, relativeUri);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(CustomerService), actualRequest, useMachineNameWithRequest:true))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(CustomerService), expectedRequest, useMachineNameWithRequest:true))
                {
                    // Patch the charset on the expectedResponse contentType because the old stack's casing was not consistent
                    expectedResponse.Content.Headers.ContentType.CharSet = "utf-8";
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        #endregion Back Compat Operation Selection Tests

        #region Back Compat Help Page Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test the main help page.")]
        public void Help_Page_Is_Available()
        {
            string relativeUri = "/help";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(CustomerService), actualRequest))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(CustomerService), expectedRequest))
                {
                    // Patch the content which  on the expectedResponse contentType because the old stack's casing was not consistent
                    expectedResponse.Content.Headers.ContentType.CharSet = "utf-8";
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test an operation help page.")]
        public void Operation_Help_Page_Is_Available()
        {
            string relativeUri = "/help/operations/GetCustomer";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(CustomerService), actualRequest))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(CustomerService), expectedRequest))
                {
                    // Patch the content which  on the expectedResponse contentType because the old stack's casing was not consistent
                    expectedResponse.Content.Headers.ContentType.CharSet = "utf-8";
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpBehavior back compat test for an unknown operation help page.")]
        public void Unknown_Operation_Help_Page_Is_Handled()
        {
            string relativeUri = "/help/UnknownOperation";

            HttpRequestMessage actualRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            HttpRequestMessage expectedRequest = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            using (HttpResponseMessage actualResponse = GetResponse(typeof(CustomerService), actualRequest))
            {
                using (HttpResponseMessage expectedResponse = GetBackCompatResponse(typeof(CustomerService), expectedRequest))
                {
                    // Patch the content which  on the expectedResponse contentType because the old stack's casing was not consistent
                    expectedResponse.Content.Headers.ContentType.CharSet = "utf-8";
                    HttpAssert.AreEqual(expectedResponse, actualResponse);
                }
            }
        }

        #endregion Back Compat Help Page Tests

        #region Back Compat Serialization Tests

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpBehavior is backwards compatible when serializing types using the DataContractSerializer for XML.")]
        public void BackCompatSerializedAsDataContractInXml()
        {
            IEnumerable<TestData> testData = TestData.ValueAndRefTypeTestDataCollection;
            TestDataVariations variations = TestDataVariations.AllNonInterfaces & ~TestDataVariations.AsDerivedType; 

            TestDataAssert.Execute(
                testData,
                variations,
                "Back compat serializing using the DataContractSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType(), useDataContract:true);
                    service.OnGetReturnInstance = () => obj;

                    WebHttpServiceHostAssert.Execute(
                        service,
                        GenericWebService.GetRequest(),
                        (expectedResponse) =>
                        {
                            Assert.AreEqual(HttpStatusCode.OK, expectedResponse.StatusCode, "Response status code should have been a 200.");
                            HttpServiceHostAssert.Execute(service, GenericWebService.GetRequest(), expectedResponse);
                        });
                });
        }

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("")]
        [Description("HttpBehavior is backwards compatible when serializing types using the XmlSerializer.")]
        public void BackCompatSerializedAsXmlSerializable()
        {
            IEnumerable<TestData> testData = new List<TestData>() {
                    TestData.DateTimeTestData, TestData.DoubleTestData, TestData.PocoTypeTestData, 
                    TestData.DataContractTypeTestData, TestData.DerivedDataContractTypeTestData,
                    TestData.DerivedXmlSerializableTypeTestData, TestData.XmlSerializableTypeTestData, 
                    TestData.StringTestData};
            TestDataVariations variations = TestDataVariations.AllNonInterfaces & ~TestDataVariations.AsDerivedType;

            TestDataAssert.Execute(
                testData,
                variations,
                "Back compat serializing using the DataContractSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType());
                    service.OnGetReturnInstance = () => obj;

                    WebHttpServiceHostAssert.Execute(
                        service,
                        GenericWebService.GetRequest(),
                        (expectedResponse) =>
                        {
                            Assert.AreEqual(HttpStatusCode.OK, expectedResponse.StatusCode, "Response status code should have been a 200.");
                            HttpServiceHostAssert.Execute(service, GenericWebService.GetRequest(), expectedResponse);
                        });
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpBehavior is backwards compatible when serializing types using the DataContractSerializer for Json.")]
        public void BackCompatSerializedAsDataContractInJson()
        {
            // DataContract with IsReference = true is not supported by the DataContractJsonSerializer.
            IEnumerable<TestData> testData = TestData.ValueAndRefTypeTestDataCollection
                                                     .Where(td => td.Type != typeof(ReferenceDataContractType));
            TestDataVariations variations = TestDataVariations.AllNonInterfaces & ~TestDataVariations.AsDerivedType;

            TestDataAssert.Execute(
                testData,
                variations,
                "Back compat serializing using the DataContractSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType(), useDataContract: true);
                    service.OnGetReturnInstance = () => obj;

                    HttpRequestMessage expectedRequest = GenericWebService.GetRequest();
                    expectedRequest.Headers.Accept.Add(JsonMediaType);

                    WebHttpServiceHostAssert.Execute(
                        service,
                        expectedRequest,
                        (expectedResponse) =>
                        {
                            Assert.AreEqual(HttpStatusCode.OK, expectedResponse.StatusCode, "Response status code should have been a 200.");

                            HttpRequestMessage actualRequest = GenericWebService.GetRequest();
                            actualRequest.Headers.Accept.Add(JsonMediaType);

                            HttpServiceHostAssert.Execute(service, actualRequest, expectedResponse);
                        });
                });
        }

        #endregion Back Compat Serialization Tests

        #region Back Compat Deserialization Tests

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpBehavior is backwards compatible when deserializing types using the DataContractSerializer for XML.")]
        public void BackCompatDeserializedAsDataContractInXml()
        {
            IEnumerable<TestData> testData = TestData.RepresentativeValueAndRefTypeTestDataCollection;
            TestDataVariations variations = TestDataVariations.AllNonInterfaces & ~TestDataVariations.AsDerivedType;

            TestDataAssert.Execute(
                testData,
                variations,
                "Back compat deserializing using the DataContractSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType(), useDataContract:true);
                    DataContractSerializer serializer = new DataContractSerializer(obj.GetType());

                    using (MemoryStream stream = new MemoryStream())
                    {
                        serializer.WriteObject(stream, obj);

                        HttpRequestMessage expectedRequest = GenericWebService.InvokeRequest();
                        expectedRequest.Content = new ByteArrayContent(stream.ToArray());
                        expectedRequest.Content.Headers.ContentType = XmlMediaType;

                        WebHttpServiceHostAssert.Execute(
                            service,
                            expectedRequest,
                            (expectedResponse) =>
                            {
                                Assert.AreEqual(HttpStatusCode.OK, expectedResponse.StatusCode, "Response status code should have been a 200.");

                                HttpRequestMessage actualRequest = GenericWebService.InvokeRequest();
                                actualRequest.Content = new ByteArrayContent(stream.ToArray());
                                actualRequest.Content.Headers.ContentType = XmlMediaType;
                                HttpServiceHostAssert.Execute(service, actualRequest, expectedResponse);
                            });
                    }
                });
        }

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("")]
        [Description("HttpBehavior is backwards compatible when deserializing types using the XmlSerializer.")]
        public void BackCompatDeserializedAsXmlSerializable()
        {
            IEnumerable<TestData> testData = new List<TestData>() {
                TestData.DateTimeTestData, TestData.DoubleTestData, TestData.PocoTypeTestData, 
                TestData.DataContractTypeTestData, TestData.DerivedDataContractTypeTestData,
                TestData.DerivedXmlSerializableTypeTestData, TestData.XmlSerializableTypeTestData};
            TestDataVariations variations = TestDataVariations.AllNonInterfaces & ~TestDataVariations.AsDerivedType;

            TestDataAssert.Execute(
                testData,
                variations,
                "Back compat deserializing using the DataContractSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType());
                    XmlSerializer serializer = new XmlSerializer(obj.GetType());

                    using (MemoryStream stream = new MemoryStream())
                    {
                        serializer.Serialize(stream, obj);

                        HttpRequestMessage expectedRequest = GenericWebService.InvokeRequest();
                        expectedRequest.Content = new ByteArrayContent(stream.ToArray());
                        expectedRequest.Content.Headers.ContentType = XmlMediaType;

                        WebHttpServiceHostAssert.Execute(
                            service,
                            expectedRequest,
                            (expectedResponse) =>
                            {
                                Assert.AreEqual(HttpStatusCode.OK, expectedResponse.StatusCode, "Response status code should have been a 200.");

                                HttpRequestMessage actualRequest = GenericWebService.InvokeRequest();
                                actualRequest.Content = new ByteArrayContent(stream.ToArray());
                                actualRequest.Content.Headers.ContentType = XmlMediaType;
                                HttpServiceHostAssert.Execute(service, actualRequest, expectedResponse);
                            });
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpBehavior is backwards compatible when deserializing types using the DataContractSerializer for Json.")]
        public void BackCompatDeserializedAsDataContractInJson()
        {
            // DataContract with IsReference = true is not supported by the DataContractJsonSerializer.
            IEnumerable<TestData> testData = TestData.RepresentativeValueAndRefTypeTestDataCollection
                                                     .Where(td => td.Type != typeof(ReferenceDataContractType));
            TestDataVariations variations = TestDataVariations.AllNonInterfaces;

            TestDataAssert.Execute(
                testData,
                variations,
                "Back compat deserializing using the DataContractSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType(), useDataContract:true);
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());

                    using (MemoryStream stream = new MemoryStream())
                    {
                        serializer.WriteObject(stream, obj);

                        HttpRequestMessage expectedRequest = GenericWebService.InvokeRequest();
                        expectedRequest.Content = new ByteArrayContent(stream.ToArray());
                        expectedRequest.Content.Headers.ContentType = JsonMediaType;
                        expectedRequest.Headers.Accept.Add(JsonMediaType);

                        WebHttpServiceHostAssert.Execute(
                            service,
                            expectedRequest,
                            (expectedResponse) =>
                            {
                                Assert.AreEqual(HttpStatusCode.OK, expectedResponse.StatusCode, "Response status code should have been a 200.");

                                HttpRequestMessage actualRequest = GenericWebService.InvokeRequest();
                                actualRequest.Content = new ByteArrayContent(stream.ToArray());
                                actualRequest.Content.Headers.ContentType = JsonMediaType;
                                actualRequest.Headers.Accept.Add(JsonMediaType);
                                HttpServiceHostAssert.Execute(service, actualRequest, expectedResponse);
                            });
                    }
                });
        }

        #endregion Back Compat Deserialization Tests

        #region Test Helpers

        private static HttpResponseMessage GetResponse(Type serviceType, HttpRequestMessage request, bool useMachineNameWithRequest = false)
        {
            return GetResponse(serviceType, request, false, useMachineNameWithRequest);
        }

        private static HttpResponseMessage GetBackCompatResponse(Type serviceType, HttpRequestMessage request, bool useMachineNameWithRequest = false)
        {
            return GetResponse(serviceType, request, true, useMachineNameWithRequest);
        }

        private static HttpResponseMessage GetResponse(Type serviceType, HttpRequestMessage request, bool getBackCompatResponse, bool useMachineNameWithRequest = false)
        {
            string relativeAddress = request.RequestUri.OriginalString;
            string baseAddress = null;
            ServiceHost host = (getBackCompatResponse) ?
                        GetWebServiceHost(serviceType, out baseAddress) :
                        GetHttpServiceHost(serviceType, out baseAddress);
            UriBuilder builder = new UriBuilder(baseAddress + relativeAddress);

            if (useMachineNameWithRequest)
            {
                builder.Host = Environment.MachineName;
            }

            request.RequestUri = builder.Uri;

            HttpResponseMessage response = null;
            using (HttpClient client = new HttpClient())
            {
                client.Channel = new WebRequestChannel();
                response = client.Send(request);
            }

            return response;
        }

        private static ServiceHost GetHttpServiceHost(Type serviceType, out string baseAddress)
        {
            Tuple<ServiceHost, string> serviceHostData = null;
            if (!serviceTypeToHttpServiceHostMapping.TryGetValue(serviceType, out serviceHostData))
            {
                ServiceHost host = null;
                baseAddress = null;
                while (host == null)
                {
                    try
                    {
                        baseAddress = string.Format("http://localhost:{0}", GetNextPortNumber());
                        host = new HttpServiceHost(serviceType, new Uri(baseAddress));
                        HttpBehavior behavior = host.AddDefaultEndpoints()[0].Behaviors.Find<HttpBehavior>();
                        behavior.HelpEnabled = true;
                        host.Open();
                    }
                    catch (AddressAlreadyInUseException)
                    {
                        host = null;
                    }
                    catch (UriFormatException)
                    {
                        host = null;
                    }
                }

                serviceHostData = new Tuple<ServiceHost, string>(host, baseAddress);
                if (!serviceTypeToHttpServiceHostMapping.TryAdd(serviceType, serviceHostData))
                {
                    host.Close();
                    return GetHttpServiceHost(serviceType, out baseAddress);
                }
            }

            baseAddress = serviceHostData.Item2;
            return serviceHostData.Item1;
        }

        private static ServiceHost GetWebServiceHost(Type serviceType, out string baseAddress)
        {
            Tuple<ServiceHost, string> serviceHostData = null;
            if (!serviceTypeToWebHttpServiceHostMapping.TryGetValue(serviceType, out serviceHostData))
            {
                ServiceHost host = null;
                baseAddress = null;
                while (host == null)
                {
                    try
                    {
                        baseAddress = string.Format("http://localhost:{0}", GetNextPortNumber());
                        host = new ServiceHost(serviceType, new Uri(baseAddress));
                        ServiceEndpoint endpoint = host.AddServiceEndpoint(serviceType, new WebHttpBinding(), string.Empty);
                        WebHttpBehavior behavior = new WebHttpBehavior();
                        behavior.AutomaticFormatSelectionEnabled = true;
                        behavior.HelpEnabled = true;
                        endpoint.Behaviors.Add(behavior);
                        host.Open();
                    }
                    catch (AddressAlreadyInUseException)
                    {
                        host = null;
                    }
                    catch (UriFormatException)
                    {
                        host = null;
                    }
                }

                serviceHostData = new Tuple<ServiceHost, string>(host, baseAddress);
                if (!serviceTypeToWebHttpServiceHostMapping.TryAdd(serviceType, serviceHostData))
                {
                    host.Close();
                    return GetHttpServiceHost(serviceType, out baseAddress);
                }
            }

            baseAddress = serviceHostData.Item2;
            return serviceHostData.Item1;
        }

        private static ServiceHost GetServiceHost(Type serviceType, IEndpointBehavior endpointBehavior, Binding binding, out string baseAddress)
        {
            ServiceHost host = null;
            baseAddress = null;
            while (host == null)
            {
                try
                {
                    baseAddress = string.Format("http://localhost:{0}/SomeBasePath", GetNextPortNumber());
                    host = new ServiceHost(serviceType, new Uri(baseAddress));
                    ServiceEndpoint endpoint = host.AddServiceEndpoint(serviceType, binding, string.Empty);
                    endpoint.Behaviors.Add(endpointBehavior);
                    host.Open();
                }
                catch (AddressAlreadyInUseException)
                {
                    host = null;
                }
                catch (UriFormatException)
                {
                    host = null;
                }
            }

            return host;
        }

        private static int GetNextPortNumber()
        {
            return Interlocked.Increment(ref portNumber);
        }

        #endregion Test Helpers
    }
}
