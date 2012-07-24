// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.ServiceModel.Web;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Services;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Http.Description.Moles;
    using Microsoft.ApplicationServer.Http.Dispatcher.Moles;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class UriAndMethodOperationSelectorTests : UnitTest<UriAndMethodOperationSelector>
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector is public, and concrete.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "UriAndMethodOperationSelector should be public.");
            Assert.IsFalse(t.IsAbstract, "UriAndMethodOperationSelector should not be abstract.");
            Assert.IsFalse(t.IsSealed, "UriAndMethodOperationSelector should not be sealed.");
            Assert.AreEqual(typeof(HttpOperationSelector), t.BaseType, "UriAndMethodOperationSelector should derive from HttpOperationSelector.");
        }

        #endregion Type

        #region Constructors

        #region UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>) accepts non-null parameters.")]
        public void Constructor()
        {
            Uri baseAddress = new Uri("http://localhost");
            List<HttpOperationDescription> operations = new List<HttpOperationDescription>();
            operations.Add(new SHttpOperationDescription());

            UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations);
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>) accepts an empty operations collection.")]
        public void ConstructorAcceptsEmptyOperations()
        {
            Uri baseAddress = new Uri("http://localhost");
            List<HttpOperationDescription> operations = new List<HttpOperationDescription>();

            UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations);
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>) throws if the operations parameter is null.")]
        public void ConstructorThrowsWithNullOperations()
        {
            ExceptionAssert.ThrowsArgumentNull("operations", () => new UriAndMethodOperationSelector(new Uri("http://localhost"), null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>) throws if the baseAddress parameter is null.")]
        public void ConstructorThrowsWithNullUri()
        {
            ExceptionAssert.ThrowsArgumentNull("baseAddress", () => new UriAndMethodOperationSelector(null, new List<HttpOperationDescription>()));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>) throws if there is more than one entry for the wildcard match.")]
        public void ConstructorThrowsWithDuplicateWildCardMethods()
        {
            SHttpOperationDescription operation1 = new SHttpOperationDescription();
            operation1.Name = "Operation1";
            operation1.Behaviors.Add(new WebInvokeAttribute() { Method = "*", UriTemplate = "*" });

            SHttpOperationDescription operation2 = new SHttpOperationDescription();
            operation2.Name = "Operation2";
            operation2.Behaviors.Add(new WebInvokeAttribute() { Method = "*", UriTemplate = "*" });

            List<HttpOperationDescription> operations = new List<HttpOperationDescription>();
            operations.Add(operation1);
            operations.Add(operation2);

            ExceptionAssert.Throws<InvalidOperationException>(
               "A UriAndMethodOperationSelector with duplicate wildcard entry should throw.",
               SR.MultipleOperationsWithSameMethodAndUriTemplate("*", "*"),
               () => new UriAndMethodOperationSelector(new Uri("http://localhost/myservice"), operations));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>) throws if there are duplicate entries with the same UriTemplate and method.")]
        public void ConstructorThrowsWithDuplicateMethodAndUriTemplate()
        {
            SHttpOperationDescription operation1 = new SHttpOperationDescription();
            operation1.Name = "Operation1";
            operation1.Behaviors.Add(new WebInvokeAttribute() { Method = "GET", UriTemplate = "ATemplate" });

            SHttpOperationDescription operation2 = new SHttpOperationDescription();
            operation2.Name = "Operation2";
            operation2.Behaviors.Add(new WebInvokeAttribute() { Method = "GET", UriTemplate = "ATemplate" });

            List<HttpOperationDescription> operations = new List<HttpOperationDescription>();
            operations.Add(operation1);
            operations.Add(operation2);

            ExceptionAssert.Throws<InvalidOperationException>(
               "A UriAndMethodOperationSelector with duplicate entries should throw.",
               SR.MultipleOperationsWithSameMethodAndUriTemplate("ATemplate", "GET"),
               () => new UriAndMethodOperationSelector(new Uri("http://localhost/myservice"), operations));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>) sets TrailingSlashMode to TrailingSlashMode.AutoRedirect.")]
        public void ConstructorSetsTrailingSlashMode()
        {
            UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(new Uri("http://localhost"), new List<HttpOperationDescription>());
            Assert.AreEqual(TrailingSlashMode.AutoRedirect, selector.TrailingSlashMode, "The default value of TrailingSlashMode should have been TrailingSlashMode.AutoRedirect");
        }

        #endregion UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>)

        #region UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>, TrailingSlashMode)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>, TrailingSlashMode) accepts non-null parameters and all TrailingSlashMode values.")]
        public void Constructor1()
        {
            foreach (TrailingSlashMode trailingSlashMode in Enum.GetValues(typeof(TrailingSlashMode)))
            {
                Uri baseAddress = new Uri("http://localhost");
                List<HttpOperationDescription> operations = new List<HttpOperationDescription>();
                operations.Add(new SHttpOperationDescription());

                UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations, trailingSlashMode);
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>, TrailingSlashMode) accepts an empty operations collection.")]
        public void Constructor1AcceptsEmptyOperations()
        {
            Uri baseAddress = new Uri("http://localhost");
            List<HttpOperationDescription> operations = new List<HttpOperationDescription>();

            UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations, TrailingSlashMode.Ignore);
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>, TrailingSlashMode) throws if the operations parameter is null.")]
        public void Constructor1ThrowsWithNullOperations()
        {
            ExceptionAssert.ThrowsArgumentNull("operations", () => new UriAndMethodOperationSelector(new Uri("http://localhost"), null, TrailingSlashMode.Ignore));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>, TrailingSlashMode) throws if the baseAddress parameter is null.")]
        public void Constructor1ThrowsWithNullUri()
        {
            ExceptionAssert.ThrowsArgumentNull("baseAddress", () => new UriAndMethodOperationSelector(null, new List<HttpOperationDescription>(), TrailingSlashMode.Ignore));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>, TrailingSlashMode) sets TrailingSlashMode.")]
        public void Constructor1SetsTrailingSlashMode()
        {
            foreach (TrailingSlashMode trailingSlashMode in Enum.GetValues(typeof(TrailingSlashMode)))
            {
                Uri baseAddress = new Uri("http://localhost");
                List<HttpOperationDescription> operations = new List<HttpOperationDescription>();
                operations.Add(new SHttpOperationDescription());

                UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations, trailingSlashMode);
                Assert.AreEqual(trailingSlashMode, selector.TrailingSlashMode, "The TrailingSlashMode should have been set to the value passed into the constructor.");
            }
        }

        #endregion UriAndMethodOperationSelector(Uri, IEnumerable<HttpOperationDescription>, TrailingSlashMode)

        #endregion Constructors

        #region Properties

        #region TrailingSlashMode

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("TrailingSlashMode property only has a getter.")]
        public void TrailingSlashModeGetsValue()
        {
            foreach (TrailingSlashMode trailingSlashMode in Enum.GetValues(typeof(TrailingSlashMode)))
            {
                Uri baseAddress = new Uri("http://localhost");
                List<HttpOperationDescription> operations = new List<HttpOperationDescription>();
                operations.Add(new SHttpOperationDescription());

                UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations, trailingSlashMode);
                Assert.AreEqual(trailingSlashMode, selector.TrailingSlashMode, "The TrailingSlashMode should have been set to the value passed into the constructor.");
            }
        }

        #endregion TrailingSlashMode

        #region HelpPageUri (Internal)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HelpPageUri property has a getter and a setter and is null by default.")]
        public void HelpUriGetsSetsValue()
        {
            Uri baseAddress = new Uri("http://localhost");
            List<HttpOperationDescription> operations = new List<HttpOperationDescription>();
            operations.Add(new SHttpOperationDescription());

            UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations);
            Assert.IsNull(selector.HelpPageUri, "The HelpPageUri should have been null by default.");

            Uri helpPageUri = new Uri("http://localhost/Help");
            selector.HelpPageUri = helpPageUri;
            Assert.AreEqual(helpPageUri, selector.HelpPageUri, "The HelpPageUri should have been set.");
        }

        #endregion HelpPageUri (Internal)

        #endregion Properties

        #region Methods

        #region SelectOperation(HttpRequestMessage)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SelectOperation(HttpRequestMessage) returns the selected operation name.")]
        public void SelectOperationReturnsOperationName()
        {
            HttpOperationAssert.Execute<UriTemplateService>(
                (operations) =>
                {
                    Uri baseAddress = new Uri("http://localhost/baseAddress");
                    UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations, TrailingSlashMode.Ignore);

                    foreach (HttpOperationDescription operation in operations)
                    {
                        UriTemplate uriTemplate = operation.GetUriTemplate();
                        HttpMethod method = operation.GetHttpMethod();
                        string expectedOperation = operation.Name;
                        
                        string[] uriParameters = uriTemplate.PathSegmentVariableNames
                                                            .Concat(uriTemplate.QueryValueVariableNames)
                                                            .ToArray();
                        Uri requestUri = uriTemplate.BindByPosition(baseAddress, uriParameters);
                        HttpRequestMessage request = new HttpRequestMessage(method, requestUri);

                        string actualOperation = selector.SelectOperation(request);
                        Assert.AreEqual(expectedOperation, actualOperation, "The UriAndMethodOperationSelector should have returned the selected operation name.");
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SelectOperation(HttpRequestMessage) returns the operation name if the wildcard UriTemplate successfully matches.")]
        public void SelectOperationReturnsWildCardTemplateOperationName()
        {
            Uri baseAddress = new Uri("http://localhost");
            List<HttpOperationDescription> operations = new List<HttpOperationDescription>();
            SHttpOperationDescription operation = new SHttpOperationDescription();
            operation.Name = "wildCardGet";
            operation.Behaviors.Add(new WebGetAttribute() { UriTemplate = "basePath/*" });
            operations.Add(operation);

            UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations);
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://localhost/basePath/andSome/AdditionalPath");
            request.Method = HttpMethod.Get;

            string actualOperationName = selector.SelectOperation(request);
            Assert.AreEqual(operation.Name, actualOperationName, "The SelectOperation method should have returned the wildcard operation.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SelectOperation(HttpRequestMessage) calls TrySelectOperation and returns the operationName out parameter if TrySelectOperation returns true.")]
        public void SelectOperationCallsTrySelectOperation()
        {
            HttpOperationAssert.Execute<UriTemplateService>(
                (operations) =>
                {
                    Uri baseAddress = new Uri("http://localhost/baseAddress");
                    SUriAndMethodOperationSelector selector = new SUriAndMethodOperationSelector(baseAddress, operations, TrailingSlashMode.Ignore);
                    bool OnTrySelectOperationCalled = false;
                    string expectedOperationName = "operationName";
                    HttpRequestMessage expectedRequest = new HttpRequestMessage();
                    expectedRequest.RequestUri = new Uri("http://differntUri");

                    selector.OnTrySelectOperationHttpRequestMessageStringOutBooleanOut =
                        delegate(HttpRequestMessage request, out string operationName, out bool differsByTrailingSlash)
                        {
                            OnTrySelectOperationCalled = true;
                            operationName = expectedOperationName;
                            differsByTrailingSlash = false;

                            Assert.AreEqual(expectedRequest, request, "The HttpRequestMessage passed to TrySelectOperation should have been passed to OnTrySelectOperation.");
                            return true;
                        };

                    string actualOperationName = selector.SelectOperation(expectedRequest);

                    Assert.IsTrue(OnTrySelectOperationCalled, "TrySelectOperation should have called OnTrySelectionOperation.");
                    Assert.AreEqual(expectedOperationName, actualOperationName, "TrySelectOperation should have returned the operationName value from OnTrySelectionOperation.");                    
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SelectOperation(HttpRequestMessage) throws if the message parameter is null.")]
        public void SelectOperationThrowsWithNullRequest()
        {
            Uri baseAddress = new Uri("http://localhost");
            List<HttpOperationDescription> operations = new List<HttpOperationDescription>();
            operations.Add(new SHttpOperationDescription());
 
            UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations);

            ExceptionAssert.ThrowsArgumentNull("message", () => selector.SelectOperation(null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SelectOperation(HttpRequestMessage) throws HttpResponseException (Not Found) if the request URI is null.")]
        public void SelectOperationThrowsWithNullRequestUri()
        {
            Uri baseAddress = new Uri("http://localhost");
            List<HttpOperationDescription> operations = new List<HttpOperationDescription>();
            operations.Add(new SHttpOperationDescription());

            UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations);
            HttpRequestMessage request = new HttpRequestMessage();

            ExceptionAssert.Throws<HttpResponseException>(
                () => selector.SelectOperation(request),
                (responseException) =>
                    {
                        HttpResponseMessage expectedResponse = StandardHttpResponseMessageBuilder.CreateNotFoundResponse(request, null);
                        HttpAssert.AreEqual(expectedResponse, responseException.Response);
                    });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SelectOperation(HttpRequestMessage) throws HttpResponseException (Not Found) if list of operations provided with the constructor is empty.")]
        public void SelectOperationThrowsWithNoOperations()
        {
            Uri baseAddress = new Uri("http://localhost");
            List<HttpOperationDescription> operations = new List<HttpOperationDescription>();

            UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations);
            HttpRequestMessage request = new HttpRequestMessage();

            ExceptionAssert.Throws<HttpResponseException>(
                () => selector.SelectOperation(request),
                (responseException) =>
                {
                    HttpResponseMessage expectedResponse = StandardHttpResponseMessageBuilder.CreateNotFoundResponse(request, null);
                    HttpAssert.AreEqual(expectedResponse, responseException.Response);
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SelectOperation(HttpRequestMessage) throws HttpResponseException (Redirect) if the match differs by a trailing slash and TrailingSlashMode.AutoRedirect.")]
        public void SelectOperationThrowsWithMatchDifferingByTrailingSlash()
        {
            HttpOperationAssert.Execute<UriTemplateService>(
                (operations) =>
                {
                    Uri baseAddress = new Uri("http://localhost/baseAddress");
                    UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations, TrailingSlashMode.AutoRedirect);

                    foreach (HttpOperationDescription operation in operations)
                    {
                        UriTemplate uriTemplate = operation.GetUriTemplate();
                        HttpMethod method = operation.GetHttpMethod();
                        string expectedOperation = operation.Name;

                        string[] uriParameters = uriTemplate.PathSegmentVariableNames
                                                            .Concat(uriTemplate.QueryValueVariableNames)
                                                            .ToArray();
                        Uri requestUri = uriTemplate.BindByPosition(baseAddress, uriParameters);
                        UriBuilder uriBuilder =  new UriBuilder(requestUri);
                        uriBuilder.Path = requestUri.AbsolutePath.EndsWith("/") ? uriBuilder.Path.TrimEnd('/') : uriBuilder.Path + "/";
                        Uri backSlashAlteredUri = uriBuilder.Uri;

                        // Because UriTemplate.BindByPosition always adds a backslash for templates "" and "/",
                        //  the original requestUri is actually the correct backSlashAlteredUri in these cases.
                        if (uriTemplate.ToString() == "" || uriTemplate.ToString() == "/")
                        {
                            Uri temp = requestUri;
                            requestUri = backSlashAlteredUri;
                            backSlashAlteredUri = temp;
                        }

                        HttpRequestMessage request = new HttpRequestMessage(method, backSlashAlteredUri);

                        ExceptionAssert.Throws<HttpResponseException>(
                            () => selector.SelectOperation(request),
                            (responseException) =>
                            {
                                HttpResponseMessage expectedResponse = StandardHttpResponseMessageBuilder.CreateTemporaryRedirectResponse(request, backSlashAlteredUri, requestUri);
                                HttpAssert.AreEqual(expectedResponse, responseException.Response);    
                            });
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SelectOperation(HttpRequestMessage) throws HttpResponseException (Method Not Allowed) if the request URI matches but the HTTP Method does not.")]
        public void SelectOperationThrowsWithUriMatchingButMethodNotMatching()
        {
            HttpOperationAssert.Execute<UriTemplateService>(
                (IEnumerable<HttpOperationDescription> operations) =>
                {
                    Uri baseAddress = new Uri("http://localhost/baseAddress");
                    UriAndMethodOperationSelector selector = new UriAndMethodOperationSelector(baseAddress, operations, TrailingSlashMode.Ignore);

                    bool oneMethodNotAllowedFound = false;

                    foreach (HttpOperationDescription operation in operations)
                    {
                        List<HttpMethod> otherAllowedMethods = operations
                            .Where(otherOperation => otherOperation.GetUriTemplate().IsEquivalentTo(operation.GetUriTemplate()))
                            .Select( otherOperation => otherOperation.GetHttpMethod())
                            .ToList();

                        if (otherAllowedMethods.Count > 1)
                        {
                            UriTemplate uriTemplate = operation.GetUriTemplate();
                            string expectedOperation = operation.Name;
                            string[] uriParameters = uriTemplate.PathSegmentVariableNames
                                                                .Concat(uriTemplate.QueryValueVariableNames)
                                                                .ToArray();
                            Uri requestUri = uriTemplate.BindByPosition(baseAddress, uriParameters);

                            foreach (HttpMethod method in HttpTestData.AllHttpMethods.Except(otherAllowedMethods))
                            {
                                HttpRequestMessage request = new HttpRequestMessage(method, requestUri);
                                ExceptionAssert.Throws<HttpResponseException>(
                                    () => selector.SelectOperation(request),
                                    (responseException) =>
                                    {
                                        HttpResponseMessage expectedResponse = StandardHttpResponseMessageBuilder.CreateMethodNotAllowedResponse(request, otherAllowedMethods, null);
                                        HttpAssert.AreEqual(expectedResponse, responseException.Response);
                                        oneMethodNotAllowedFound = true;
                                    });
                            }
                        }
                    }

                    Assert.IsTrue(oneMethodNotAllowedFound, "No interesting test cases actually executed.");
                });     
        }

        #endregion SelectOperation(HttpRequestMessage)

        #region TrySelectOperation(HttpRequestMessage, out string, out bool)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("TrySelectOperation(HttpRequestMessage, out string, out bool) calls OnTrySelectOperation.")]
        public void TrySelectOperationCallsOnTrySelectOperation()
        {
            SUriAndMethodOperationSelector selector = new SUriAndMethodOperationSelector(new Uri("http://localhost"), new List<HttpOperationDescription>());
            bool OnTrySelectOperationCalled = false;
            string expectedOperationName = "operationName";
            bool expectedDiffersByTrailingSlash = false;
            bool expectedTrySelectionOperationReturnValue = true;
            HttpRequestMessage expectedRequest = new HttpRequestMessage();

            selector.OnTrySelectOperationHttpRequestMessageStringOutBooleanOut =
                delegate(HttpRequestMessage request, out string operationName, out bool differsByTrailingSlash)
                {
                    OnTrySelectOperationCalled = true;
                    operationName = expectedOperationName;
                    differsByTrailingSlash = expectedDiffersByTrailingSlash;

                    Assert.AreEqual(expectedRequest, request, "The HttpRequestMessage passed to TrySelectOperation should have been passed to OnTrySelectOperation.");
                    return expectedTrySelectionOperationReturnValue;
                };

            string actualOperationName = null;
            bool actualDiffersByTrailingSlash;
            bool actualTrySelectionOperationReturnValue;

            actualTrySelectionOperationReturnValue = selector.TrySelectOperation(expectedRequest, out actualOperationName, out actualDiffersByTrailingSlash);

            Assert.IsTrue(OnTrySelectOperationCalled, "TrySelectOperation should have called OnTrySelectionOperation.");
            Assert.AreEqual(expectedDiffersByTrailingSlash, actualDiffersByTrailingSlash, "TrySelectOperation should have returned the differsByTrailingSlash value from OnTrySelectionOperation.");
            Assert.AreEqual(expectedOperationName, actualOperationName, "TrySelectOperation should have returned the operationName value from OnTrySelectionOperation.");
            Assert.AreEqual(expectedTrySelectionOperationReturnValue, actualTrySelectionOperationReturnValue, "TrySelectOperation should have returned the return value from OnTrySelectionOperation.");        
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("TrySelectOperation(HttpRequestMessage, out string, out bool) throws in the request parameter is null.")]
        public void TrySelectOperationThrowsWithNullRequest()
        {
            SUriAndMethodOperationSelector selector = new SUriAndMethodOperationSelector(new Uri("http://localhost"), new List<HttpOperationDescription>());
            string operationName = null;
            bool differsByTrailingSlash;

            ExceptionAssert.ThrowsArgumentNull("request", () => selector.TrySelectOperation(null, out operationName, out differsByTrailingSlash));
        }

        #endregion TrySelectOperation(HttpRequestMessage, out string, out bool)

        #region OnTrySelectOperation(HttpRequestMessage, out string, out bool)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OnTrySelectOperation(HttpRequestMessage, out string, out bool) throws in the request parameter is null.")]
        public void OnTrySelectOperationThrowsWithNullRequest()
        {
            TestUriAndMethodOperationSelector selector = new TestUriAndMethodOperationSelector(new Uri("http://localhost"), new List<HttpOperationDescription>());
            string operationName;
            bool differsByTrailingSlash;
            
            ExceptionAssert.ThrowsArgumentNull("request", () => selector.OnTrySelectOperationProxy(null, out operationName, out differsByTrailingSlash));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OnTrySelectOperation(HttpRequestMessage, out string, out bool) returns false if the request URI in null.")]
        public void OnTrySelectOperationReturnsFalseForNullRequestUri()
        {
            TestUriAndMethodOperationSelector selector = new TestUriAndMethodOperationSelector(new Uri("http://localhost"), new List<HttpOperationDescription>());
            string operationName;
            bool differsByTrailingSlash;
            HttpRequestMessage request = new HttpRequestMessage();

            bool actual = selector.OnTrySelectOperationProxy(request, out operationName, out differsByTrailingSlash);
            Assert.AreEqual(false, actual, "The OnTrySelectOperation should have returned false because the request URI was null.");
        }

        #endregion OnTrySelectOperation(HttpRequestMessage, out string, out bool)

        #endregion Methods

        #region Test types

        public class TestUriAndMethodOperationSelector : UriAndMethodOperationSelector
        {
            public TestUriAndMethodOperationSelector(Uri baseAddress, IEnumerable<HttpOperationDescription> operations) 
                : base(baseAddress, operations)
            {
            }

            public bool OnTrySelectOperationProxy(HttpRequestMessage request, out string operationName, out bool differsByTrailingSlash)
            {
                return this.OnTrySelectOperation(request, out operationName, out differsByTrailingSlash);
            }
        }

        #endregion Test types
    }
}
