// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Web;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Common.Test;

    [TestClass]
    public class TrailingSlashModeTests
    {
        [TestMethod]
        [Description("SelectOperation tests with IgnoreTrailingSlash.")]
        public void Ignore_TrailingSlash_Tests()
        {
            string baseUri = "http://localhost/myservice";
            List<HttpOperationDescription> operationList1 = GenerateOperationsList1();
            UriAndMethodOperationSelector httpOperationSelector = new UriAndMethodOperationSelector(new Uri(baseUri), operationList1, TrailingSlashMode.Ignore);

            string baseUriWithSlash = "http://localhost/myservice/";
            List<HttpOperationDescription> operationList2 = GenerateOperationsList2();
            UriAndMethodOperationSelector httpOperationSelector2 = new UriAndMethodOperationSelector(new Uri(baseUriWithSlash), operationList2, TrailingSlashMode.Ignore);

            //{"GetCollection", new UriTemplate("")},
            VerifySelectOperationWithIgnoreMode(baseUri, "GetCollection", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/", "GetCollection", httpOperationSelector);

            // {"PQuery", new UriTemplate("p?value={value}")},
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "p", "PQuery", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "p/", "PQuery", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "p", "PQuery", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "p?value=1", "PQuery", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "p/?value=1", "PQuery", httpOperationSelector);

            // {"QQuery", new UriTemplate("q/?value={value}")},
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "q/?value=hello", "QQuery", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "q?value=hello", "QQuery", httpOperationSelector);

            // {"BarWildcard", new UriTemplate("bar/{*foo}")},
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "bar/fooVal", "BarWildcard", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "bar/", "BarWildcard", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "bar", "BarWildcard", httpOperationSelector);

            // {"GetXY", new UriTemplate("x/y")},
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "x/y", "GetXY", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "x/y/", "GetXY", httpOperationSelector);

            // {"GetRS", new UriTemplate("r/s/")},
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "r/s/", "GetRS", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "r/s", "GetRS", httpOperationSelector);

            // {"Test", new UriTemplate("test/{a=1}")},
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "test/", "Test", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "test/", "Test", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "test/aVal", "Test", httpOperationSelector);
            VerifySelectOperationWithIgnoreMode(baseUri + "/" + "test/aVal/", "Test", httpOperationSelector);

            // {"GetCollection", new UriTemplate("?value={value}")},
            VerifySelectOperationWithIgnoreMode(baseUriWithSlash, "GetCollection", httpOperationSelector2);
            VerifySelectOperationWithIgnoreMode(baseUri + "/", "GetCollection", httpOperationSelector2);
            VerifySelectOperationWithIgnoreMode(baseUriWithSlash + "?value=hello", "GetCollection", httpOperationSelector2);
            VerifySelectOperationWithIgnoreMode(baseUri + "?value=hello", "GetCollection", httpOperationSelector2);

            // {"QQuery", new UriTemplate("q/{name}/?value={value}")},
            VerifySelectOperationWithIgnoreMode(baseUriWithSlash + "q/nameVal/?value=hello", "QQuery", httpOperationSelector2);
            VerifySelectOperationWithIgnoreMode(baseUriWithSlash + "q/nameVal?value=hello", "QQuery", httpOperationSelector2);

            //{"GetId", new UriTemplate("{id}")},
            VerifySelectOperationWithIgnoreMode(baseUriWithSlash + "idVal", "GetId", httpOperationSelector2);
            VerifySelectOperationWithIgnoreMode(baseUriWithSlash + "idVal/", "GetId", httpOperationSelector2);
        }

        [TestMethod]
        [Description("SelectOperation tests with TrailingSlashMode AutoRedirect.")]
        public void AutoRedirect_TrailingSlash_Tests()
        {
            string baseUri = "http://localhost/myservice";
            List<HttpOperationDescription> operationList1 = GenerateOperationsList1();
            UriAndMethodOperationSelector httpOperationSelector = new UriAndMethodOperationSelector(new Uri(baseUri), operationList1);

            string baseUriWithSlash = "http://localhost/myservice/";
            List<HttpOperationDescription> operationList2 = GenerateOperationsList2();
            UriAndMethodOperationSelector httpOperationSelector2 = new UriAndMethodOperationSelector(new Uri(baseUriWithSlash), operationList2);

            //{"GetCollection", new UriTemplate("")},
            VerifySelectOperationWithAutoRedirectMode(baseUri, "GetCollection", httpOperationSelector, false);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/", "GetCollection", httpOperationSelector, true);

            // {"PQuery", new UriTemplate("p?value={value}")},
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "p", "PQuery", httpOperationSelector, false);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "p/", "PQuery", httpOperationSelector, true);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "p?value=1", "PQuery", httpOperationSelector, false);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "p/?value=1", "PQuery", httpOperationSelector, true);

            // {"QQuery", new UriTemplate("q/?value={value}")},
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "q/?value=hello", "QQuery", httpOperationSelector, false);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "q?value=hello", "QQuery", httpOperationSelector, true);

            // {"BarWildcard", new UriTemplate("bar/{*foo}")},
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "bar/fooVal", "BarWildcard", httpOperationSelector, false);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "bar/", "BarWildcard", httpOperationSelector, false);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "bar", "BarWildcard", httpOperationSelector, true);

            // {"GetXY", new UriTemplate("x/y")},
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "x/y", "GetXY", httpOperationSelector, false);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "x/y/", "GetXY", httpOperationSelector, true);

            // {"GetRS", new UriTemplate("r/s/")},
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "r/s/", "GetRS", httpOperationSelector, false);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "r/s", "GetRS", httpOperationSelector, true);

            // {"Test", new UriTemplate("test/{a=1}")},
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "test/", "Test", httpOperationSelector, false);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "test", "Test", httpOperationSelector, true);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "test/aVal", "Test", httpOperationSelector, false);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "/" + "test/aVal/", "Test", httpOperationSelector, true);

            // {"GetCollection", new UriTemplate("?value={value}")},
            VerifySelectOperationWithAutoRedirectMode(baseUriWithSlash, "GetCollection", httpOperationSelector2, false);
            VerifySelectOperationWithAutoRedirectMode(baseUri, "GetCollection", httpOperationSelector2, true);
            VerifySelectOperationWithAutoRedirectMode(baseUriWithSlash + "?value=hello", "GetCollection", httpOperationSelector2, false);
            VerifySelectOperationWithAutoRedirectMode(baseUri + "?value=hello", "GetCollection", httpOperationSelector2, true);

            // {"QQuery", new UriTemplate("q/{name}/?value={value}")},
            VerifySelectOperationWithAutoRedirectMode(baseUriWithSlash + "q/nameVal/?value=hello", "QQuery", httpOperationSelector2, false);
            VerifySelectOperationWithAutoRedirectMode(baseUriWithSlash + "q/nameVal?value=hello", "QQuery", httpOperationSelector2, true);

            //{"GetId", new UriTemplate("{id}")},
            VerifySelectOperationWithAutoRedirectMode(baseUriWithSlash + "idVal", "GetId", httpOperationSelector2, false);
            VerifySelectOperationWithAutoRedirectMode(baseUriWithSlash + "idVal/", "GetId", httpOperationSelector2, true);
        }

        static Dictionary<string, UriTemplate> serviceList1 = new Dictionary<string, UriTemplate>()
        {
            {"GetCollection", new UriTemplate("")},
            {"PQuery", new UriTemplate("p?value={value}")},
            {"QQuery", new UriTemplate("q/?value={value}")},
            {"BarWildcard", new UriTemplate("bar/{*foo}")},
            {"GetXY", new UriTemplate("x/y")},
            {"GetRS", new UriTemplate("r/s/")},
            {"Test", new UriTemplate("test/{a=1}")},
        };

        static Dictionary<string, UriTemplate> serviceList2 = new Dictionary<string, UriTemplate>()
        {
           {"GetCollection", new UriTemplate("?value={value}")},
           {"QQuery", new UriTemplate("q/{name}/?value={value}")},
           {"GetId", new UriTemplate("{id}")},
        };

        public static List<HttpOperationDescription> GenerateOperationsList1()
        {
            List<HttpOperationDescription> operationList = new List<HttpOperationDescription>();
            foreach (KeyValuePair<string, UriTemplate> item in serviceList1)
            {
                HttpOperationDescription data = new HttpOperationDescription();
                data.Name = item.Key;
                data.Behaviors.Add(new WebGetAttribute() { UriTemplate = item.Value.ToString() });
                operationList.Add(data);
            }

            return operationList;
        }

        public static List<HttpOperationDescription> GenerateOperationsList2()
        {
            List<HttpOperationDescription> operationList = new List<HttpOperationDescription>();
            foreach (KeyValuePair<string, UriTemplate> item in serviceList2)
            {
                HttpOperationDescription data = new HttpOperationDescription();
                data.Name = item.Key;
                data.Behaviors.Add(new WebGetAttribute() { UriTemplate = item.Value.ToString() });
                operationList.Add(data);
            }

            return operationList;
        }

        public static void VerifySelectOperationWithIgnoreMode(string requestUri, string operationName, UriAndMethodOperationSelector httpOperationSelector)
        {
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            string matchedOperationName = httpOperationSelector.SelectOperation(message);
            Assert.AreEqual(matchedOperationName, operationName, String.Format("request {0} should match the operation {1}", requestUri, operationName));
        }

        public static void VerifySelectOperationWithAutoRedirectMode(string requestUri, string operationName, UriAndMethodOperationSelector httpOperationSelector, bool shouldAutoRedirect)
        {
            string matchedOperationName;
            bool matchDifferByTrailingSlash;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            bool result = httpOperationSelector.TrySelectOperation(request, out matchedOperationName, out matchDifferByTrailingSlash);
            Assert.IsTrue(result);
            Assert.AreEqual(matchedOperationName, operationName, String.Format("request {0} should match the operation {1}", requestUri, operationName));
            Assert.AreEqual(matchDifferByTrailingSlash, shouldAutoRedirect);

            Uri originalUri = new Uri(requestUri);
            UriBuilder uriBuilder = new UriBuilder(originalUri);
            uriBuilder.Path = originalUri.AbsolutePath.EndsWith("/") ? uriBuilder.Path.TrimEnd('/') : uriBuilder.Path + "/";
            Uri backSlashAlteredUri = uriBuilder.Uri;

            if (!shouldAutoRedirect)
            {
                matchedOperationName = httpOperationSelector.SelectOperation(request);
                Assert.AreEqual(matchedOperationName, operationName, String.Format("request {0} should match the operation {1}", requestUri, operationName));
            }
            else
            {
                ExceptionAssert.Throws<HttpResponseException>(
                    () => httpOperationSelector.SelectOperation(request),
                    (responseException) =>
                        {
                            HttpResponseMessage expectedResponse = StandardHttpResponseMessageBuilder.CreateTemporaryRedirectResponse(request, originalUri, backSlashAlteredUri);
                            HttpAssert.AreEqual(expectedResponse, responseException.Response); 
                        });
            }
        }
    }
}
