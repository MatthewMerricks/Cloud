// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System.Linq;

namespace ContactManager_Simple
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using Microsoft.ApplicationServer.Http;

    [ServiceContract]
    public class ContactsResource
    {
        private readonly IContactRepository repository;

        public ContactsResource():this(new ContactRepository())
        {
        }

        public ContactsResource(IContactRepository repository)
        {
            this.repository = repository;
        }
        
        [WebGet(UriTemplate = "")]
        public List<Contact> Get()
        {
            return this.repository.GetAll();
        }

        [WebInvoke(UriTemplate = "", Method = "POST")]
        public HttpResponseMessage<Contact> Post(Contact contact)
        {
            this.repository.Post(contact);
            var response = new HttpResponseMessage<Contact>(contact);
            response.StatusCode = HttpStatusCode.Created;
            return response;
        }
    }
}