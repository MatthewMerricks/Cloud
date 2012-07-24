// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;

    public interface IResourceFactory
    {
        object GetInstance(Type serviceType, InstanceContext instanceContext, HttpRequestMessage request);

        void ReleaseInstance(InstanceContext instanceContext, object service);
    }
}