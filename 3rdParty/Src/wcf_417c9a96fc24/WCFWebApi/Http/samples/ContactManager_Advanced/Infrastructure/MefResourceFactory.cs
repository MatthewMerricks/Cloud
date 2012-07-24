using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.ServiceModel;
using System.ComponentModel.Composition.Primitives;
using System.ServiceModel.Channels;
using Microsoft.ApplicationServer.Http.Description;

namespace ContactManager_Advanced
{
    using System.Net.Http;

    public class MefResourceFactory : IResourceFactory
    {
        private CompositionContainer container;

        public MefResourceFactory(CompositionContainer container)
        {
            this.container = container;
        }

        // Get the instance from MEF
        public object GetInstance(Type serviceType, InstanceContext instanceContext, HttpRequestMessage message)
        {
            var contract = AttributedModelServices.GetContractName(serviceType);
            var identity = AttributedModelServices.GetTypeIdentity(serviceType);

            // force non-shared so that every service doesn't need to have a [PartCreationPolicy] attribute.
            var definition = new ContractBasedImportDefinition(contract, identity, null, ImportCardinality.ExactlyOne, false, false, CreationPolicy.NonShared);
            return this.container.GetExports(definition).First().Value;
        }

        public void ReleaseInstance(InstanceContext instanceContext, object service)
        {
            // no op
        }

    }
}