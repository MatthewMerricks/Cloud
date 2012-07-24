// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonValueSample
{
    using System.Json;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class ContactsResource
    {
        private static int nextId = 1;

        [WebInvoke(UriTemplate = "", Method = "POST")]
        public JsonValue Post(JsonValue jsonContact) //contact will be passed as JsonValue
        {
            dynamic contact = jsonContact;
            dynamic contactResponse = new JsonObject();
            contactResponse.Name = contact.Name;
            contactResponse.ContactId = nextId++;
            return contactResponse;
        }
    }
}