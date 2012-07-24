// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel;
    using System.Text;

    [ServiceContract]
    internal class CustomerService
    {
        private static ConcurrentDictionary<int, Customer> customers;

        static CustomerService()
        {
            customers =  new ConcurrentDictionary<int,Customer>();
            customers.TryAdd(1, new Customer() { Id = 1, Name = "Customer1" });
            customers.TryAdd(2, new Customer() { Id = 2, Name = "Customer2" });
            customers.TryAdd(3, new Customer() { Id = 3, Name = "Customer3" });
        }

        [OperationContract]
        public HttpResponseMessage GetCustomers(HttpRequestMessage request)
        {
            if (HasIdInQueryString(request))
            {
                int id = GetIdFromQueryString(request);
                Customer customer = GetCustomerFromId(id);
                return GetResponseForCustomer(customer);
            }

            return GetResponseWithAllCustomers();
        }

        [OperationContract]
        public HttpResponseMessage PutCustomers(HttpRequestMessage request)
        {
            int id = GetIdFromQueryString(request);
            Customer customer = GetCustomerFromId(id);
            Customer updatedCustomer = GetCustomerFromRequest(request);
            customer.Name = updatedCustomer.Name;
            return GetResponseForCustomer(customer);
        }

        [OperationContract]
        public HttpResponseMessage PostCustomers(HttpRequestMessage request)
        {
            Customer newCustomer = GetCustomerFromRequest(request);
            AddCustomer(newCustomer);        
            HttpResponseMessage response = GetResponseForCustomer(newCustomer);
            response.Headers.Location = GetLocationHeader(request, newCustomer.Id);
            response.StatusCode = HttpStatusCode.Created;
            return response;
        }

        [OperationContract]
        public void DeleteCustomers(HttpRequestMessage request)
        {
            int id = GetIdFromQueryString(request);
            RemoveCustomer(id);
        }

        [OperationContract]
        public HttpResponseMessage CatchAll(HttpRequestMessage request)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new StringContent("The uri and/or method is not valid for any customer resource.");
            return response;
        }

        private static int GetIdFromQueryString(HttpRequestMessage request)
        {
            var queryString = request.RequestUri.Query.ParseAsQueryString();

            int id;
            if (!int.TryParse(queryString["id"], out id))
            {
                HttpResponseMessage response = new HttpResponseMessage();
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent("An 'id' with a integer value must be provided in the query string.");
                throw new HttpResponseMessageException(response);
            }

            return id;
        }

        private static bool HasIdInQueryString(HttpRequestMessage request)
        {
            var queryString = request.RequestUri.Query.ParseAsQueryString();

            return queryString["id"] != null;
        }

        private static Customer GetCustomerFromId(int id)
        {
            Customer customer;
            if (!customers.TryGetValue(id, out customer))
            {
                HttpResponseMessage response = new HttpResponseMessage();
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(string.Format("There is no customer with id '{0}'.", id));
                throw new HttpResponseMessageException(response);
            }

            return customer;
        }

        private static void RemoveCustomer(int id)
        {
            Customer customer;
            if (!customers.TryRemove(id, out customer))
            {
                HttpResponseMessage response = new HttpResponseMessage();
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(string.Format("There is no customer with id '{0}'.", id));
                throw new HttpResponseMessageException(response);
            }
        }

        private static HttpResponseMessage GetResponseForCustomer(Customer customer)
        {
            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(SerializeCustomer(customer))
            };
        }

        private static HttpResponseMessage GetResponseWithAllCustomers()
        {
            StringBuilder builder = new StringBuilder();
            foreach (Customer customer in customers.Values)
            {
                builder.AppendLine(SerializeCustomer(customer));
            }

            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(builder.ToString())
            };
        }

        private static string SerializeCustomer(Customer customer)
        {
            return string.Format("Id = {0}; Name = {1}", customer.Id, customer.Name);
        }

        private static Customer GetCustomerFromRequest(HttpRequestMessage request)
        {
            if (request.Content == null)
            {
                HttpResponseMessage noContentResponse = new HttpResponseMessage();
                noContentResponse.StatusCode = HttpStatusCode.BadRequest;
                noContentResponse.Content = new StringContent("Expected an entity body with customer data but no content was found.");
                throw new HttpResponseMessageException(noContentResponse);
            }

            string content = request.Content.ReadAsString();
            string[] contentSplit = content.Split('=', ';');

            if (contentSplit.Length == 4)
            {
                if (string.Equals(contentSplit[0].Trim(), "Id", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(contentSplit[2].Trim(), "Name", StringComparison.OrdinalIgnoreCase))
                {
                    Customer customer = new Customer();
                    int id;
                    if (int.TryParse(contentSplit[1].Trim(), out id))
                    {
                        customer.Id = id;
                        customer.Name = contentSplit[3].Trim();
                        return customer;
                    }
                }
            }

            HttpResponseMessage faileParseResponse = new HttpResponseMessage();
            faileParseResponse.StatusCode = HttpStatusCode.BadRequest;
            faileParseResponse.Content = new StringContent("Parsing the entity body as a customer failed.");
            throw new HttpResponseMessageException(faileParseResponse);
        }

        private static void AddCustomer(Customer customer)
        {
            if (!customers.TryAdd(customer.Id, customer))
            {
                HttpResponseMessage response = new HttpResponseMessage();
                response.StatusCode = HttpStatusCode.Conflict;
                response.Content = new StringContent(string.Format("There already a customer with id '{0}'.", customer.Id));
                throw new HttpResponseMessageException(response);
            }
        }

        private static Uri GetLocationHeader(HttpRequestMessage request, int id)
        {
            return new Uri(
                string.Format("{0}://{1}{2}?id={3}",
                    request.RequestUri.Scheme,
                    request.RequestUri.Authority,
                    request.RequestUri.AbsolutePath,
                    id));
        }
    }
}
