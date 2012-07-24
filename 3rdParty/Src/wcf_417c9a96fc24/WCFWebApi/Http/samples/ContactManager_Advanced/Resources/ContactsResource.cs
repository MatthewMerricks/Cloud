// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System;
using System.Linq;

namespace ContactManager_Advanced
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using Microsoft.ApplicationServer.Http;

    [ServiceContract]
    [Export]
    public class ContactsResource
    {
        private readonly IContactRepository repository;

        [ImportingConstructor]
        public ContactsResource(IContactRepository repository)
        {
            this.repository = repository;
        }
        
        [WebGet(UriTemplate = "")]
        public IQueryable<Contact> Get()
        {
            return this.repository.GetAll().AsQueryable();
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