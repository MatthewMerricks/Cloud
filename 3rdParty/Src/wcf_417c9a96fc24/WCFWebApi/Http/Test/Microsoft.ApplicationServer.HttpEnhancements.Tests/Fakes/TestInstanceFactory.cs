using System;
using System.ServiceModel;
using Microsoft.ApplicationServer.Http.Description;

namespace Microsoft.ApplicationServer.HttpEnhancements.Tests
{
    public class TestInstanceFactory : IResourceFactory
    {
        public object GetInstance(Type serviceType, InstanceContext instanceContext, System.Net.Http.HttpRequestMessage request)
        {
            throw new NotImplementedException();
        }

        public void ReleaseInstance(InstanceContext instanceContext, object service)
        {
            throw new NotImplementedException();
        }
    }
}