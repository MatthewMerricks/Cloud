// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class ExceptionService
    {
        [WebGet(UriTemplate = "GetThrows")]
        public void  GetThrows()
        {
            throw new InvalidOperationException("This operation is invalid.");
        }

        [WebGet(UriTemplate = "GetThrowsWebFault/{name}")]
        public void GetThrowsWebFault(string name)
        {
            throw new WebFaultException<Customer>(new Customer() { Name = name }, HttpStatusCode.Forbidden); 
        }

        [WebGet(UriTemplate = "GetThrowsWebFaultAsXmlSerializable/{name}")]
        [XmlSerializerFormat(SupportFaults = true)]
        public void GetThrowsWebFaultAsXmlSerializable(string name)
        {
            throw new WebFaultException<Customer>(new Customer() { Name = name }, HttpStatusCode.Forbidden); 
        }

        [WebGet(UriTemplate = "GetThrowsWebFaultAsJson/{name}", ResponseFormat = WebMessageFormat.Json)]
        public void GetThrowsWebFaultAsJson(string name)
        {
            throw new WebFaultException<Customer>(new Customer() { Name = name }, HttpStatusCode.Forbidden); 
        }

        [WebGet(UriTemplate = "GetThrowsWebFaultArgumentNull")]
        public void GetThrowsWebFaultArgumentNull()
        {
            // this test exercises the faultFormatter installed to recover to the XmlSerializer throwing when
            // it discovers ArgumentNullException cannot be serialized
            throw new WebFaultException<ArgumentNullException>(new ArgumentNullException(), HttpStatusCode.Forbidden);
        }
    }
}
