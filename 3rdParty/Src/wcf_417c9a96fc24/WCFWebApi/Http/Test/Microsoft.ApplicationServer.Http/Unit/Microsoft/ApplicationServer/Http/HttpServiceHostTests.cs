// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Services;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Http.Dispatcher;

    [TestClass]
    public class HttpServiceHostTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpServiceHost ctors function")]
        public void ServiceHost_Ctors()
        {
            // Default ctor works
            HttpServiceHost host = new HttpServiceHost();

            // Singleton object ctor works
            host = new HttpServiceHost(new CustomerService(), new Uri("http://somehost"));

            // service type works
            host = new HttpServiceHost(typeof(CustomerService), new Uri("http://somehost"));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpServiceHost ctors with null parameters throw")]
        public void ServiceHost_Ctors_With_Nulls_Throw()
        {
            ExceptionAssert.ThrowsArgumentNull("singletonInstance", () => new HttpServiceHost((object)null, new Uri("http://somehost")));
            ExceptionAssert.ThrowsArgumentNull("serviceType", () => new HttpServiceHost((Type) null, new Uri("http://somehost")));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpServiceHost OnOpening adds behaviors to endpoints that don't have them")]
        public void ServiceHost_Explicit_Add_Endpoint_Without_Behavior()
        {
            ContractDescription cd = ContractDescription.GetContract(typeof(LocalCustomerService));
            HttpServiceHost host = new HttpServiceHost(typeof(LocalCustomerService), new Uri("http://localhost"));
            HttpEndpoint endpoint = new HttpEndpoint(cd, new EndpointAddress("http://somehost"));
            endpoint.Behaviors.Clear();
            Assert.AreEqual(0, endpoint.Behaviors.Count, "Expected no behaviors by default");
            host.Description.Endpoints.Add(endpoint);
            host.Open();
            Assert.AreEqual(1, endpoint.Behaviors.OfType<HttpBehavior>().Count(), "Expected open to add behavior");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpServiceHost works for one-way operation")]
        public void ServiceHost_Works_With_OneWay_Operation()
        {
            ContractDescription cd = ContractDescription.GetContract(typeof(OneWayService));
            HttpServiceHost host = new HttpServiceHost(typeof(OneWayService), new Uri("http://localhost/onewayservice"));
            host.Open();

            using (HttpClient client = new HttpClient())
            {
                client.Channel = new WebRequestChannel();
                using (HttpResponseMessage actualResponse = client.Get("http://localhost/onewayservice/name"))
                {
                    Assert.AreEqual(actualResponse.StatusCode, HttpStatusCode.Accepted, "Response status code should be Accepted(202) for one-way operation");
                }
            }

            host.Close();
        }

        [ServiceContract]
        public class OneWayService
        {
            [OperationContract(IsOneWay = true)]
            [WebGet(UriTemplate = "{name}")]
            public void GetLocalCustomer(string name) { }
        }

        [ServiceContract]
        public class LocalCustomerService
        {
            [WebGet(UriTemplate="{name}")]
            public LocalCustomer GetLocalCustomer(string name) { return null; }
        }

        [DataContract]
        public class LocalCustomer
        {
            [DataMember]
            public string Name { get; set; }
        }
    }
}
