namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.ServiceModel;
    using System.Text;
    using Microsoft.ServiceModel.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JsonValueFormatterTest
    {
        [TestMethod]
        public void DifferentWriteEncodingsTest()
        {
            Encoding[] validEncodings = new Encoding[]
            {
                Encoding.UTF8,
                Encoding.Unicode,
                Encoding.BigEndianUnicode,
            };

            string[] charsetValues = new string[] { "utf-8", "utf-16LE", "utf-16BE" };

            for (int i = 0; i < validEncodings.Length; i++)
            {
                Encoding encoding = validEncodings[i];
                WebHttpBinding binding = new WebHttpBinding();
                binding.WriteEncoding = encoding;
                WebHttpBehavior3 behavior = new WebHttpBehavior3();
                string baseAddress = TestService.BaseAddress;

                using (ServiceHost host = new ServiceHost(typeof(TestService), new Uri(baseAddress)))
                {
                    host.AddServiceEndpoint(typeof(ITestService), binding, "").Behaviors.Add(behavior);
                    host.Open();
                    HttpWebRequest request = WebHttpBehavior3Test.CreateRequest("GET", baseAddress + "/EchoGet?a=1", null, null, null);
                    HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
                    Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
                    Assert.AreEqual("application/json; charset=" + charsetValues[i], resp.ContentType);
                    Stream respStream = resp.GetResponseStream();
                    string responseBody = new StreamReader(respStream, encoding).ReadToEnd();
                    Assert.AreEqual("{\"a\":\"1\"}", responseBody);
                }
            }
        }

        [TestMethod]
        public void FormsUrlEncodedMaxDepthQuotaTest()
        {
            string errorContentType = "text/html";
            int anyMaxDepth = 5;
            WebHttpBinding binding = new WebHttpBinding();
            binding.ReaderQuotas.MaxDepth = anyMaxDepth;
            WebHttpBehavior3 behavior = new WebHttpBehavior3();
            string formsEncodedWithinQuota = "a[b][c][d]=e".Replace("[", "%5B").Replace("]", "%5D");
            string formsEncodedExceededQuota = "a[b][c][d][e]=g".Replace("[", "%5B").Replace("]", "%5D");
            string jsonWithinQuota = "{\"a\":{\"b\":{\"c\":{\"d\":\"e\"}}}}";
            string jsonExceededQuota = "{\"a\":{\"b\":{\"c\":{\"d\":{\"e\":\"f\"}}}}}";

            string baseAddress = TestService.BaseAddress;
            using (ServiceHost host = new ServiceHost(typeof(TestService), new Uri(baseAddress)))
            {
                host.AddServiceEndpoint(typeof(ITestService), binding, "").Behaviors.Add(behavior);
                host.Open();
                this.TestSendRequest("POST", baseAddress + "/EchoPost", "application/json", jsonWithinQuota, HttpStatusCode.OK, WebHttpBehavior3Test.ApplicationJsonContentType, jsonWithinQuota);
                this.TestSendRequest("POST", baseAddress + "/EchoPost", "application/x-www-form-urlencoded", formsEncodedWithinQuota, HttpStatusCode.OK, WebHttpBehavior3Test.ApplicationJsonContentType, jsonWithinQuota);
                this.TestSendRequest("POST", baseAddress + "/EchoPost", "application/json", jsonExceededQuota, HttpStatusCode.BadRequest, errorContentType, null);
                this.TestSendRequest("POST", baseAddress + "/EchoPost", "application/x-www-form-urlencoded", formsEncodedExceededQuota, HttpStatusCode.BadRequest, errorContentType, null);
            }
        }

        void TestSendRequest(string method, string address, string contentType, string body, HttpStatusCode expectedResponseStatusCode, string expectedResponseContentType, string expectedResponseBody)
        {
            HttpWebRequest request = WebHttpBehavior3Test.CreateRequest(method, address, contentType, body, body == null ? null : Encoding.UTF8);
            HttpWebResponse resp;
            try
            {
                resp = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                resp = (HttpWebResponse)e.Response;
            }

            Assert.AreEqual(expectedResponseStatusCode, resp.StatusCode);
            Assert.AreEqual(expectedResponseContentType, resp.ContentType);
            if (expectedResponseBody != null)
            {
                Stream respStream = resp.GetResponseStream();
                string responseBody = new StreamReader(respStream).ReadToEnd();
                Assert.AreEqual(expectedResponseBody, responseBody);
            }
        }
    }
}
