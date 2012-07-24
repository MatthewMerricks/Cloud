namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;

    internal class DelegateInstanceFactory : IResourceFactory
    {
        private readonly Func<Type, InstanceContext, HttpRequestMessage,object> getInstance;

        private readonly Action<InstanceContext, object> releaseInstance;

        public DelegateInstanceFactory(Func<Type, InstanceContext, HttpRequestMessage, object> getInstance, Action<InstanceContext, object> releaseInstance)
        {
            this.getInstance = getInstance;
            this.releaseInstance = releaseInstance;
        }

        public object GetInstance(Type serviceType, InstanceContext instanceContext, HttpRequestMessage request)
        {
            return this.getInstance(serviceType, instanceContext, request);
        }

        public void ReleaseInstance(InstanceContext instanceContext, object service)
        {
            this.releaseInstance(instanceContext, service);
        }
    }
}