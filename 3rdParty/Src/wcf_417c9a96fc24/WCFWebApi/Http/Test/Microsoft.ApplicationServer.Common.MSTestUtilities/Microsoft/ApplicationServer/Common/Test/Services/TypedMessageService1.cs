// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class TypedMessageService1
    {
        [WebGet()]
        public void TypedMessageOperation(TypedMessage typedMessage)
        {
        }
    }

    [MessageContract]
    public class TypedMessage
    {

    }
}
