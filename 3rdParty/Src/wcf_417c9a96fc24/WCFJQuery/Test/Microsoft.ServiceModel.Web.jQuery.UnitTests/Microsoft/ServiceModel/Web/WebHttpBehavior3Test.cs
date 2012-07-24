namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.IO;
    using System.Json;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using System.Text;
    using Microsoft.ServiceModel.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebHttpBehavior3Test
    {
        internal const string ApplicationJsonContentType = "application/json; charset=utf-8";
        internal const string ApplicationXmlContentType = "application/xml; charset=utf-8";

        [TestMethod]
        public void DefaultPropertiesTest()
        {
            WebHttpBehavior3 target = new WebHttpBehavior3();
            Assert.AreEqual(true, target.AutomaticFormatSelectionEnabled);
            Assert.AreEqual(WebMessageBodyStyle.Bare, target.DefaultBodyStyle);
            Assert.AreEqual(WebMessageFormat.Xml, target.DefaultOutgoingRequestFormat);
            Assert.AreEqual(WebMessageFormat.Xml, target.DefaultOutgoingResponseFormat);
            Assert.AreEqual(false, target.FaultExceptionEnabled);
            Assert.AreEqual(true, target.HelpEnabled);
        }

        [TestMethod]
        public void CheckBodyStyleTest()
        {
            this.TestValidationError<BadServices.NegativeTestService, BadServices.IJsonValueInputWithBodyStyleWrapped, InvalidOperationException>(null);
            this.TestValidationError<BadServices.NegativeTestService, BadServices.IJsonValueInputWithBodyStyleWrappedRequest, InvalidOperationException>(null);
            this.TestValidationError<BadServices.NegativeTestService, BadServices.IJsonValueReturnWithBodyStyleWrapped, InvalidOperationException>(null);
            this.TestValidationError<BadServices.NegativeTestService, BadServices.IJsonValueReturnWithBodyStyleWrappedResponse, InvalidOperationException>(null);
        }

        [TestMethod]
        public void CheckNoUnmappedParametersTest()
        {
            this.TestValidationError<BadServices.NegativeTestService, BadServices.IJsonValueInputWithUnmappedExtraParameter, InvalidOperationException>(null);
            this.TestValidationError<BadServices.NegativeTestService, BadServices.IParameterInTemplateNotInOperation, InvalidOperationException>(null);
            this.TestValidationError<BadServices.NegativeTestService, BadServices.IParameterInTemplateQueryNotInOperation, InvalidOperationException>(null);
            this.TestValidationError<BadServices.NegativeTestService, BadServices.IParameterInTemplateQueryNotConvertible, InvalidOperationException>(null);
        }

        [TestMethod]
        public void CheckResponseFormatTest()
        {
            this.TestValidationError<BadServices.NegativeTestService, BadServices.IJsonValueReturnWithResponseFormatXml, InvalidOperationException>(null);
            this.TestValidationError<BadServices.NegativeTestService, BadServices.IJsonValueReturnWithResponseFormatXml2, InvalidOperationException>(null);
        }

        [TestMethod]
        public void CheckWebInvokeAndWebGetTest()
        {
            this.TestValidationError<BadServices.NegativeTestService, BadServices.IJsonValueWebGetAndWebInvoke, InvalidOperationException>(null);
        }

        [TestMethod]
        public void DifferentRequestTypesTest()
        {
            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3(),
                "POST",
                TestService.BaseAddress + "/EchoPost",
                ApplicationJsonContentType,
                "{\"a\":123}",
                null,
                CreateResponseValidator(HttpStatusCode.OK, ApplicationJsonContentType, "{\"a\":123}"));

            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3(),
                "POST",
                TestService.BaseAddress + "/EchoPost",
                "application/x-www-form-urlencoded",
                "a=123",
                null,
                CreateResponseValidator(HttpStatusCode.OK, ApplicationJsonContentType, "{\"a\":\"123\"}"));

            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3(),
                "GET",
                TestService.BaseAddress + "/EchoGET?a=123",
                null,
                null,
                null,
                CreateResponseValidator(HttpStatusCode.OK, ApplicationJsonContentType, "{\"a\":\"123\"}"));

            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3(),
                "GET",
                TestService.BaseAddress + "/EchoGET?simpleString",
                null,
                null,
                null,
                CreateResponseValidator(HttpStatusCode.OK, ApplicationJsonContentType, "{\"simpleString\":null}"));

            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3(),
                "HEAD",
                TestService.BaseAddress + "/EchoHEAD?a=123",
                null,
                null,
                null,
                delegate(HttpWebResponse resp)
                {
                    Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
                    Assert.AreEqual(ApplicationJsonContentType, resp.ContentType);
                    Assert.AreNotEqual(0, resp.ContentLength);
                    Stream respStream = resp.GetResponseStream();
                    Assert.AreEqual("", new StreamReader(respStream).ReadToEnd());
                });
        }

        [TestMethod]
        public void DifferentContentTypesTest()
        {
            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3(),
                "POST",
                TestService.BaseAddress + "/EchoPost",
                "application/json; charset=utf-16LE",
                "{\"a\":123}",
                Encoding.Unicode,
                CreateResponseValidator(HttpStatusCode.OK, ApplicationJsonContentType, "{\"a\":123}"));

            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3(),
                "POST",
                TestService.BaseAddress + "/EchoPost",
                "application/json; charset=utf-16BE",
                "{\"a\":123}",
                Encoding.BigEndianUnicode,
                CreateResponseValidator(HttpStatusCode.OK, ApplicationJsonContentType, "{\"a\":123}"));

            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3(),
                "POST",
                TestService.BaseAddress + "/EchoPost",
                "application/x-www-form-urlencoded; charset=utf-16",
                "a=123",
                Encoding.Unicode,
                CreateResponseValidator(HttpStatusCode.OK, ApplicationJsonContentType, "{\"a\":\"123\"}"));

            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3(),
                "POST",
                TestService.BaseAddress + "/EchoPost",
                "application/x-www-form-urlencoded; charset=utf-16BE",
                "a=123",
                Encoding.BigEndianUnicode,
                CreateResponseValidator(HttpStatusCode.OK, ApplicationJsonContentType, "{\"a\":\"123\"}"));

            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3(),
                "POST",
                TestService.BaseAddress + "/EchoPOST",
                "text/plain",
                "content-type is not supported",
                null,
                delegate(HttpWebResponse resp)
                {
                    Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
                    Assert.AreEqual("text/html", resp.ContentType);
                    Assert.AreNotEqual(0, resp.ContentLength);
                });
        }

        [TestMethod]
        public void EmptyRequestsTest()
        {
            WebHttpBehavior[] webHttpBehaviors = { new WebHttpBehavior3(), new WebHttpBehavior() };
            Type[] serviceTypes = { typeof(TestService), typeof(TestService35) };
            Type[] interfaceTypes = { typeof(ITestService), typeof(ITestService35) };
            for (int i = 0; i < 2; i++)
            {
                TestSendRequest(
                    serviceTypes[i],
                    interfaceTypes[i],
                    webHttpBehaviors[i],
                    "POST",
                    TestService.BaseAddress + "/EchoPOST",
                    null,
                    "",
                    null,
                    CreateResponseValidator(HttpStatusCode.OK, "", ""));

                TestSendRequest(
                    serviceTypes[i],
                    interfaceTypes[i],
                    webHttpBehaviors[i],
                    "GET",
                    TestService.BaseAddress + "/EchoNull",
                    null,
                    null,
                    null,
                    CreateResponseValidator(HttpStatusCode.OK, "", ""));

                TestSendRequest(
                    serviceTypes[i],
                    interfaceTypes[i],
                    webHttpBehaviors[i],
                    "POST",
                    TestService.BaseAddress + "/EchoPOST",
                    ApplicationJsonContentType,
                    "",
                    null,
                    CreateResponseValidator(HttpStatusCode.OK, "", ""));
            }
        }

        [TestMethod]
        public void WebFaultExceptionOfJsonValueTest()
        {
            HttpStatusCode anyStatusCode = HttpStatusCode.Unauthorized;
            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3 { FaultExceptionEnabled = false },
                "POST",
                TestService.BaseAddress + "/ThrowWebFaultExceptionOfJsonValueResponseFormatJson?statusCode=" + (int)anyStatusCode,
                "text/json",
                AnyInstance.AnyJsonValue1.ToString(),
                Encoding.UTF8,
                CreateResponseValidator(anyStatusCode, ApplicationJsonContentType, AnyInstance.AnyJsonValue1.ToString()));

            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3 { FaultExceptionEnabled = false },
                "POST",
                TestService.BaseAddress + "/ThrowWebFaultExceptionOfJsonValueResponseFormatXml?statusCode=" + (int)anyStatusCode,
                "text/json",
                AnyInstance.AnyJsonValue1.ToString(),
                Encoding.UTF8,
                CreateResponseValidator(anyStatusCode, ApplicationJsonContentType, AnyInstance.AnyJsonValue1.ToString()));

            anyStatusCode = HttpStatusCode.UnsupportedMediaType;
            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3 { FaultExceptionEnabled = false },
                "POST",
                TestService.BaseAddress + "/ThrowWebFaultExceptionOfJsonValueResponseFormatJson?statusCode=" + (int)anyStatusCode,
                null,
                "",
                Encoding.UTF8,
                CreateResponseValidator(anyStatusCode, "", ""));

            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3 { FaultExceptionEnabled = false },
                "POST",
                TestService.BaseAddress + "/ThrowWebFaultExceptionOfJsonValueResponseFormatXml?statusCode=" + (int)anyStatusCode,
                null,
                "",
                Encoding.UTF8,
                CreateResponseValidator(anyStatusCode, "", ""));
        }

        [TestMethod]
        public void WebFaultExceptionTest()
        {
            HttpStatusCode anyStatusCode = HttpStatusCode.ServiceUnavailable;
            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3 { FaultExceptionEnabled = false },
                "GET",
                TestService.BaseAddress + "/ThrowWebFaultExceptionResponseFormatJson?statusCode=" + (int)anyStatusCode,
                null,
                null,
                null,
                CreateResponseValidator(anyStatusCode, "", ""));

            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3 { FaultExceptionEnabled = false },
                "GET",
                TestService.BaseAddress + "/ThrowWebFaultExceptionResponseFormatXml?statusCode=" + (int)anyStatusCode,
                null,
                null,
                null,
                CreateResponseValidator(anyStatusCode, "", ""));
        }

        [TestMethod]
        public void WebFaultExceptionOfXElementTest()
        {
            string expectedContentType = "application/xml; charset=utf-8";
            HttpStatusCode anyStatusCode = HttpStatusCode.Forbidden;
            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3 { FaultExceptionEnabled = false },
                "GET",
                TestService.BaseAddress + "/ThrowWebFaultExceptionResponseFormatJson?statusCode=" + (int)anyStatusCode + "&throwWebFaultOfXElement=true",
                null,
                null,
                null,
                CreateResponseValidator(anyStatusCode, expectedContentType, "<statusCode>" + (int)anyStatusCode + "</statusCode>"));

            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3 { FaultExceptionEnabled = false },
                "GET",
                TestService.BaseAddress + "/ThrowWebFaultExceptionResponseFormatXml?statusCode=" + (int)anyStatusCode + "&throwWebFaultOfXElement=true",
                null,
                null,
                null,
                CreateResponseValidator(anyStatusCode, expectedContentType, "<statusCode>" + (int)anyStatusCode + "</statusCode>"));
        }

        [TestMethod]
        public void HasExactlyOneJsonValueTest()
        {
            TestSendRequest<BadServices.NegativeTestService, BadServices.IInputWithMoreThanOneJsonValueInputs>(
                new WebHttpBehavior3(),
                "POST",
                TestService.BaseAddress + "/Operation",
                "application/json",
                "{\"input1\":123,\"input2\":456}",
                Encoding.UTF8,
                CreateResponseValidator(HttpStatusCode.BadRequest, null, null));
        }

        [TestMethod]
        public void AutomaticFormatSelectionEnabledTest()
        {
            WebHttpBehavior3 target = new WebHttpBehavior3();
            Assert.IsTrue(target.AutomaticFormatSelectionEnabled);
            target.AutomaticFormatSelectionEnabled = false;
            Assert.IsFalse(target.AutomaticFormatSelectionEnabled);
        }

        [TestMethod]
        public void FaultExceptionEnabledTest()
        {
            WebHttpBehavior3 target = new WebHttpBehavior3();
            Assert.IsFalse(target.FaultExceptionEnabled);
            target.FaultExceptionEnabled = true;
            Assert.IsTrue(target.FaultExceptionEnabled);
        }

        [TestMethod]
        public void HelpEnabledTest()
        {
            WebHttpBehavior3 target = new WebHttpBehavior3();
            Assert.IsTrue(target.HelpEnabled);
            target.HelpEnabled = false;
            Assert.IsFalse(target.HelpEnabled);
        }

        [TestMethod]
        public void ValidationTest()
        {
            TestSendRequest<TestService, ITestService>(
                new WebHttpBehavior3() { ValidationEnabled = true },
                "POST",
                TestService.BaseAddress + "/ThrowValidation",
                "application/json; charset=utf-8",
                "{\"Age\":123}",
                Encoding.UTF8,
                CreateResponseValidator(HttpStatusCode.InternalServerError, ApplicationJsonContentType, "{\"Age\":\"The field Age must be between 18 and 20.\"}"));
        }

        internal static HttpWebRequest CreateRequest(string method, string uri, string contentType, string body, Encoding bodyEncoding)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            request.Method = method;
            if (contentType != null)
            {
                request.ContentType = contentType;
            }

            if (body != null)
            {
                Stream reqStream = request.GetRequestStream();
                if (bodyEncoding == null)
                {
                    bodyEncoding = Encoding.UTF8;
                }

                byte[] bodyBytes = bodyEncoding.GetBytes(body);
                reqStream.Write(bodyBytes, 0, bodyBytes.Length);
                reqStream.Close();
            }

            return request;
        }

        static Action<HttpWebResponse> CreateResponseValidator(HttpStatusCode statusCode, string contentType, string body)
        {
            return delegate(HttpWebResponse resp)
            {
                Assert.AreEqual(statusCode, resp.StatusCode);
                if (contentType != null)
                {
                    Assert.AreEqual(contentType, resp.ContentType);
                }

                if (body != null)
                {
                    string responseBody = new StreamReader(resp.GetResponseStream()).ReadToEnd();
                    Assert.AreEqual(body, responseBody);
                }
            };
        }

        static void TestSendRequest<TService, TInterface>(WebHttpBehavior behavior, string method, string uri, string contentType, string body, Encoding bodyEncoding, Action<HttpWebResponse> responseValidator)
        {
            TestSendRequest(typeof(TService), typeof(TInterface), behavior, method, uri, contentType, body, bodyEncoding, responseValidator);
        }

        static void TestSendRequest(Type serviceType, Type interfaceType, WebHttpBehavior behavior, string method, string uri, string contentType, string body, Encoding bodyEncoding, Action<HttpWebResponse> responseValidator)
        {
            string baseAddress = TestService.BaseAddress;
            using (ServiceHost host = new ServiceHost(serviceType, new Uri(baseAddress)))
            {
                host.AddServiceEndpoint(interfaceType, new WebHttpBinding(), "").Behaviors.Add(behavior);
                host.Open();
                HttpWebRequest request = CreateRequest(method, uri, contentType, body, bodyEncoding);
                HttpWebResponse resp;
                try
                {
                    resp = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException e)
                {
                    resp = (HttpWebResponse)e.Response;
                }

                if (responseValidator != null)
                {
                    responseValidator(resp);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Reliability", "CA2000",
            Justification = "The host cannot be disposed, since it calls Close, and the host is Faulted (so Close will throw)")]
        void TestValidationError<TService, TInterface, TExpectedException>(Action<WebHttpBehavior3> behaviorChanges) where TExpectedException : Exception
        {
            string baseAddress = TestService.BaseAddress;
            ServiceHost host = new ServiceHost(typeof(TService), new Uri(baseAddress));
            WebHttpBehavior3 behavior = new WebHttpBehavior3();
            if (behaviorChanges != null)
            {
                behaviorChanges(behavior);
            }

            host.AddServiceEndpoint(typeof(TInterface), new WebHttpBinding(), "").Behaviors.Add(behavior);
            try
            {
                host.Open();
                Assert.Fail("Error, expected exception {0}, got none", typeof(TExpectedException).FullName);
            }
            catch (TExpectedException e)
            {
                Console.WriteLine("For interface {0}, caught: {1}", typeof(TInterface).Name, e);
            }
            finally
            {
                host.Abort();
            }
        }

        public class BadServices
        {
            [ServiceContract]
            public interface IJsonValueReturnWithBodyStyleWrappedResponse
            {
                [OperationContract, WebGet(BodyStyle = WebMessageBodyStyle.WrappedResponse)]
                JsonValue Operation();
            }

            [ServiceContract]
            public interface IJsonValueReturnWithBodyStyleWrapped
            {
                [OperationContract, WebGet(BodyStyle = WebMessageBodyStyle.Wrapped)]
                JsonValue Operation();
            }

            [ServiceContract]
            public interface IJsonValueInputWithBodyStyleWrapped
            {
                [OperationContract, WebInvoke(BodyStyle = WebMessageBodyStyle.Wrapped)]
                void Operation(JsonValue input);
            }

            [ServiceContract]
            public interface IJsonValueInputWithBodyStyleWrappedRequest
            {
                [OperationContract, WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedRequest)]
                void Operation(JsonValue input);
            }

            [ServiceContract]
            public interface IJsonValueInputWithUnmappedExtraParameter
            {
                [OperationContract, WebInvoke(UriTemplate = "/Operation")]
                void Operation(string str, JsonValue input);
            }

            [ServiceContract]
            public interface IJsonValueInputWithUnmappedExtraParameter2
            {
                [OperationContract, WebInvoke]
                void Operation(string str, JsonValue input);
            }

            [ServiceContract]
            public interface IJsonValueReturnWithResponseFormatXml
            {
                [WebGet(ResponseFormat = WebMessageFormat.Xml)]
                JsonValue Operation();
            }

            [ServiceContract]
            public interface IJsonValueReturnWithResponseFormatXml2
            {
                [WebInvoke(ResponseFormat = WebMessageFormat.Xml)]
                JsonValue Operation();
            }

            [ServiceContract]
            public interface IJsonValueWebGetAndWebInvoke
            {
                [WebGet, WebInvoke]
                JsonValue Operation();
            }

            [ServiceContract]
            public interface IInputWithMoreThanOneJsonValueInputs
            {
                [OperationContract, WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedRequest)]
                JsonValue Operation(JsonValue input1, JsonValue input2);
            }

            [ServiceContract]
            public interface IParameterInTemplateNotInOperation
            {
                [OperationContract, WebInvoke(UriTemplate = "/Operation/{foo}")]
                void Operation(JsonValue input);
            }

            [ServiceContract]
            public interface IParameterInTemplateQueryNotInOperation
            {
                [OperationContract, WebInvoke(UriTemplate = "/Operation?foo={foo}")]
                void Operation(JsonValue input);
            }

            [ServiceContract]
            public interface IParameterInTemplateQueryNotConvertible
            {
                [OperationContract, WebInvoke(UriTemplate = "/Operation?binding={binding}")]
                void Operation(JsonValue input, WebHttpBinding binding);
            }

            public sealed class NegativeTestService :
                IJsonValueInputWithBodyStyleWrapped,
                IJsonValueInputWithBodyStyleWrappedRequest,
                IJsonValueReturnWithBodyStyleWrapped,
                IJsonValueReturnWithBodyStyleWrappedResponse,
                IJsonValueInputWithUnmappedExtraParameter,
                IJsonValueInputWithUnmappedExtraParameter2,
                IJsonValueReturnWithResponseFormatXml,
                IJsonValueReturnWithResponseFormatXml2,
                IJsonValueWebGetAndWebInvoke,
                IInputWithMoreThanOneJsonValueInputs,
                IParameterInTemplateNotInOperation,
                IParameterInTemplateQueryNotInOperation,
                IParameterInTemplateQueryNotConvertible
            {
                void IJsonValueInputWithBodyStyleWrapped.Operation(JsonValue input)
                {
                    throw new NotSupportedException("This should never be reached");
                }

                void IJsonValueInputWithBodyStyleWrappedRequest.Operation(JsonValue input)
                {
                    throw new NotSupportedException("This should never be reached");
                }

                JsonValue IJsonValueReturnWithBodyStyleWrapped.Operation()
                {
                    throw new NotSupportedException("This should never be reached");
                }

                JsonValue IJsonValueReturnWithBodyStyleWrappedResponse.Operation()
                {
                    throw new NotSupportedException("This should never be reached");
                }

                void IJsonValueInputWithUnmappedExtraParameter.Operation(string str, JsonValue input)
                {
                    throw new NotSupportedException("This should never be reached");
                }

                void IJsonValueInputWithUnmappedExtraParameter2.Operation(string str, JsonValue input)
                {
                    throw new NotSupportedException("This should never be reached");
                }

                JsonValue IJsonValueReturnWithResponseFormatXml.Operation()
                {
                    throw new NotSupportedException("This should never be reached");
                }

                JsonValue IJsonValueReturnWithResponseFormatXml2.Operation()
                {
                    throw new NotSupportedException("This should never be reached");
                }

                JsonValue IJsonValueWebGetAndWebInvoke.Operation()
                {
                    throw new NotSupportedException("This should never be reached");
                }

                JsonValue IInputWithMoreThanOneJsonValueInputs.Operation(JsonValue input1, JsonValue input2)
                {
                    return input1;
                }

                void IParameterInTemplateNotInOperation.Operation(JsonValue input)
                {
                    throw new NotSupportedException("This should never be reached");
                }

                void IParameterInTemplateQueryNotInOperation.Operation(JsonValue input)
                {
                    throw new NotSupportedException("This should never be reached");
                }

                void IParameterInTemplateQueryNotConvertible.Operation(JsonValue input, WebHttpBinding binding)
                {
                    throw new NotSupportedException("This should never be reached");
                }
            }
        }
    }
}