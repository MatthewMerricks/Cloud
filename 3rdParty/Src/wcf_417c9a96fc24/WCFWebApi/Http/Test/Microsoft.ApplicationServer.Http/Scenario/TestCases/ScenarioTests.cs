// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario
{
    using System;
    using System.Net;
    using System.Net.Http;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Http.CIT.Scenario.Common;

    [TestClass]
    public class ScenarioTests
    {
        #region GET tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a GET request for all customers that already exist by default.")]
        public void Get_All_Existing_Customer()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "Customers");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The status code should have been 'OK'.");
                    string[] responseContent = response.Content.ReadAsString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    Assert.AreEqual("Id = 1; Name = Customer1", responseContent[0], "The response content should have been 'Id = 1; Name = Customer1'.");
                    Assert.AreEqual("Id = 2; Name = Customer2", responseContent[1], "The response content should have been 'Id = 2; Name = Customer2'.");
                    Assert.AreEqual("Id = 3; Name = Customer3", responseContent[2], "The response content should have been 'Id = 3; Name = Customer3'.");
                }
            }
        }

        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a GET request for a single customer that already exists by default.")]
        public void Get_Existing_Customer()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "Customers?id=1");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The status code should have been 'OK'.");
                    Assert.AreEqual("Id = 1; Name = Customer1", response.Content.ReadAsString(), "The response content should have been 'Id = 1; Name = Customer1'.");
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a GET request for a customer that doesn't exist by default.")]
        public void Get_Non_Existing_Customer()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "Customers?id=5");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "The status code should have been 'NotFound'.");
                    Assert.AreEqual("There is no customer with id '5'.", response.Content.ReadAsString(), "The response content should have been 'There is no customer with id '5'.'");
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a GET request with an id that can't be parsed as an integer.")]
        public void Get_With_Non_Integer_Id()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "Customers?id=foo");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "The status code should have been 'BadRequest'.");
                    Assert.AreEqual("An 'id' with a integer value must be provided in the query string.", response.Content.ReadAsString(), "The response content should have been 'An 'id' with a integer value must be provided in the query string.'");
                }
            }
        }

        #endregion GET Tests

        #region PUT Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a PUT request to update a single customer that already exists by default.")]
        public void Update_Existing_Customer()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "Customers?id=1");
                request.Content = new StringContent("Id = 1; Name = NewCustomerName1");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The status code should have been 'OK'.");
                    Assert.AreEqual("Id = 1; Name = NewCustomerName1", response.Content.ReadAsString(), "The response content should have been 'Id = 1; Name = NewCustomerName1'.");
                }

                // Put server back in original state
                request = new HttpRequestMessage(HttpMethod.Put, "Customers?id=1");
                request.Content = new StringContent("Id = 1; Name = Customer1");
                client.Send(request);
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a PUT request to update a single customer that doesn't exist by default.")]
        public void Update_Non_Existing_Customer()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "Customers?id=5");
                request.Content = new StringContent("Id = 5; Name = NewCustomerName1");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "The status code should have been 'NotFound'.");
                    Assert.AreEqual("There is no customer with id '5'.", response.Content.ReadAsString(), "The response content should have been 'There is no customer with id '5'.'");
                }
            }
        }

        #endregion PUT Tests

        #region POST Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a POST request to create a new customer.")]
        public void Create_New_Customer()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "Customers");
                request.Content = new StringContent("Id = 7; Name = NewCustomer7");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, "The status code should have been 'Created'.");
                    Assert.IsNotNull(response.Headers.Location, "The location header should not have been null.");
                    Assert.AreEqual(new Uri("http://localhost:8080/Customers?id=7"), response.Headers.Location, "The location header should have beeen 'http://localhost:8080/Customers?id=7'.");
                    Assert.AreEqual("Id = 7; Name = NewCustomer7", response.Content.ReadAsString(), "The response content should have been 'Id = 7; Name = NewCustomer7'.");
                }

                // Put server back in original state
                request = new HttpRequestMessage(HttpMethod.Delete, "Customers?id=7");
                client.Send(request);
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a POST request to create a customer that already exists.")]
        public void Create_Customer_That_Already_Exists()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "Customers?id=2");
                request.Content = new StringContent("Id = 2; Name = AlreadyCustomer2");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode, "The status code should have been 'Conflict'.");
                    Assert.AreEqual("There already a customer with id '2'.", response.Content.ReadAsString(), "The response content should have been 'There already a customer with id '2'.'");
                }
            }
        }

        #endregion POST Tests

        #region DELETE Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a DELETE request to remove a single customer that already exists by default.")]
        public void Delete_Existing_Customer()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "Customers?id=3");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The status code should have been 'OK'.");
                    Assert.AreEqual(string.Empty, response.Content.ReadAsString(), "The response content should have been an empty string.");
                }

                // Put server back in the original state
                request = new HttpRequestMessage(HttpMethod.Post, "Customers");
                request.Content = new StringContent("Id = 3; Name = Customer3");
                client.Send(request);
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a DELETE request to remove a single customer that doesn't exist.")]
        public void Delete_Non_Existing_Customer()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "Customers?id=4");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "The status code should have been 'NotFound'.");
                    Assert.AreEqual("There is no customer with id '4'.", response.Content.ReadAsString(), "The response content should have been 'There is no customer with id '4'.'");
                }
            }
        }

        #endregion DELETE Tests

        #region CatchAll Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a request that will get handled by the catch all operation because of the HTTP method used.")]
        public void Send_Request_With_Unknown_Method()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("UNKNOWN"), "Customers");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "The status code should have been 'NotFound'.");
                    Assert.AreEqual("The uri and/or method is not valid for any customer resource.", response.Content.ReadAsString(), "The response content should have been 'The uri and/or method is not valid for any customer resource.'.");
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a request that will get handled by the catch all operation because of the URI used.")]
        public void Send_Request_With_Unknown_Uri()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "UnknownUri");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "The status code should have been 'NotFound'.");
                    Assert.AreEqual("The uri and/or method is not valid for any customer resource.", response.Content.ReadAsString(), "The response content should have been 'The uri and/or method is not valid for any customer resource.'.");
                }
            }
        }

        #endregion CatchAll Tests

        #region Message Inspector Tests
        
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        
        [Description("Sends a GET request for all customers and uses the 'NamesOnly' custom header.")]
        public void Get_With_Custom_Header_For_Customer_Names_Only()
        {
            Uri baseAddress = new Uri("http://localhost:8080/");
            CustomServiceHost host = new CustomServiceHost(typeof(CustomerService), baseAddress);
            using (host)
            {
                host.Open();

                HttpClient client = new HttpClient(baseAddress);
                client.Channel = new WebRequestChannel();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "Customers");
                request.Headers.Add("NamesOnly", "Ok");
                HttpResponseMessage response = client.Send(request);
                using (response)
                {
                    Assert.IsNotNull(response, "The response should not have been null.");
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The status code should have been 'OK'.");
                    Assert.AreEqual("Customer1, Customer2, Customer3", response.Content.ReadAsString(), "The response content should have been 'Customer1, Customer2, Customer3'.");
                }
            }
        }

        #endregion Message Inspector Tests
    }
}
