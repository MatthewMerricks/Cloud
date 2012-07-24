// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Description;

    public interface IEndpointFactory
    {
        ServiceEndpoint CreateEndpoint(Type serviceType, HttpServiceHost host);
    }
}
