// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Microsoft.ApplicationServer.Http.Dispatcher;

    public class ResourceFactoryProvider : HttpInstanceProvider
    {
        private readonly Type serviceType;

        private readonly IResourceFactory resourceFactory;

        public ResourceFactoryProvider(Type serviceType, IResourceFactory resourceFactory)
        {
            this.serviceType = serviceType;
            this.resourceFactory = resourceFactory;
        }

        protected override object OnGetInstance(InstanceContext instanceContext, System.Net.Http.HttpRequestMessage request)
        {
            return this.resourceFactory.GetInstance(serviceType, instanceContext, request);
        }

        protected override object OnGetInstance(InstanceContext instanceContext)
        {
            return OnGetInstance(instanceContext, null);
        }

        protected override void OnReleaseInstance(InstanceContext instanceContext, object instance)
        {
            this.resourceFactory.ReleaseInstance(instanceContext, instance);
        }
    }
}
