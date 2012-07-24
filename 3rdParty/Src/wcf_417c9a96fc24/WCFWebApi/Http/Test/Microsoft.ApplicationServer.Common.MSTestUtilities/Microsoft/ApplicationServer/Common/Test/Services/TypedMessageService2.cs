// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class TypedMessageService2
    {
        [WebGet()]
        public TypedMessage TypedMessageOperation()
        {
            return new TypedMessage();
        }
    }
}
