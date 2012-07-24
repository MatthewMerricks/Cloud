// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.Xml;
    using System.Xml.Linq;

    [ServiceContract]
    public class InvalidXmlParameterService1_XmlElement
    {
        // Illegal because it asks for Json
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        public XmlElement Operation()
        {
            return null;
        }
    }

    [ServiceContract]
    public class InvalidXmlParameterService1_XmlNode
    {
        // Illegal because it asks for Json
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        public XmlNode Operation()
        {
            return null;
        }
    }

    [ServiceContract]
    public class InvalidXmlParameterService1_XElement
    {
        // Illegal because it asks for Json
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        public XElement Operation()
        {
            return null;
        }
    }

    [ServiceContract]
    public class InvalidXmlParameterService1_XDocument
    {
        // Illegal because it asks for Json
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        public XDocument Operation()
        {
            return null;
        }
    }
}
