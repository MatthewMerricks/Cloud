using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Activation;
using System.ServiceModel.Dispatcher;
using System.Text;
using Microsoft.ApplicationServer.Http;
using Microsoft.ApplicationServer.Http.Activation;
using Microsoft.ApplicationServer.Http.Description;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationServer.HttpEnhancements.Tests
{
    [TestClass]
    public class HttpConfigurableServiceHostTest
    {
        private const string DummyUri = "http://localhost/foo";

        [TestMethod]
        public void WhenCreatedWithATypeThenConfigurationIsSet()
        {
            var config = new HttpHostConfiguration();
            var host = new HttpConfigurableServiceHost(typeof (TestService), config,new Uri(DummyUri));
            Assert.AreEqual(config,host.configuration);
        }

        [TestMethod]
        public void WhenCreatedWithAnInstanceThenConfigurationIsSet()
        {
            var config = new HttpHostConfiguration();
            var host = new HttpConfigurableServiceHost(new TestService(), config, new Uri(DummyUri));
            Assert.AreEqual(config, host.configuration);
        }

        [TestMethod]
        public void WhenCreatedWithATypeThenServiceTypeIsSet()
        {
            var config = new HttpHostConfiguration();
            var host = new HttpConfigurableServiceHost(typeof (TestService), config,new Uri(DummyUri));
            Assert.AreEqual(typeof(TestService), host.serviceType);
        }

        [TestMethod]
        public void WhenCreatedWithAnInstanceThenServiceTypeIsSet()
        {
            var config = new HttpHostConfiguration();
            var host = new HttpConfigurableServiceHost(new TestService(), config, new Uri(DummyUri));
            Assert.AreEqual(typeof(TestService), host.serviceType);
        }

        [TestMethod]
        public void WhenCreatedThenOperationHandlerFactoryIsSet()
        {
            var config = new HttpHostConfiguration();
            config.OperationHandlerFactory = new TestOperationHandlerFactory();
            var host = new HttpConfigurableServiceHost(typeof(TestService), config, new Uri(DummyUri));
            Assert.AreEqual(config.OperationHandlerFactory, host.OperationHandlerFactory);
        }

        [TestMethod]
        public void WhenCreatedThenMessageHandlerFactoryIsSet()
        {
            var config = new HttpHostConfiguration();
            config.MessageHandlerFactory = new TestMessageHandlerFactory();
            var host = new HttpConfigurableServiceHost(typeof(TestService), config, new Uri(DummyUri));
            Assert.AreEqual(config.MessageHandlerFactory, host.MessageHandlerFactory);
        }

        /*
        [TestMethod]
        public void WhenCreatedThenErrorHandlerBehaviorIsAddedToService()
        {
            var config = new HttpHostConfiguration();
            var host = new HttpConfigurableServiceHost(new TestService(), config, new Uri(DummyUri));
            Assert.IsTrue(host.Description.Behaviors.Contains(typeof(HttpErrorHandlerBehavior)));
        }
        */
          
        [TestMethod]
        public void WhenCreatedThenAspNetCompatibilityRequirementsAttributeIsAdded()
        {
            var config = new HttpHostConfiguration();
            var host = new HttpConfigurableServiceHost(new TestService(), config, new Uri(DummyUri));
            Assert.IsTrue(host.Description.Behaviors.Contains(typeof (AspNetCompatibilityRequirementsAttribute)));
        }


        [TestMethod]
        public void WhenCreatedThenHttpEndpointIsAddedToTheService()
        {
            var config = new HttpHostConfiguration();
            var host = new HttpConfigurableServiceHost(typeof (TestService), config, new Uri(DummyUri));
            var httpEndpoint = (HttpEndpoint) host.Description.Endpoints.Find(typeof (TestService));
            Assert.IsNotNull(httpEndpoint);
        }

        [TestMethod]
        public void WhenHttpEndpointIsCreatedThenOperationHandlerFactoryIsSet()
        {
            var config = new HttpHostConfiguration();
            config.OperationHandlerFactory = new TestOperationHandlerFactory();
            var host = new HttpConfigurableServiceHost(typeof(TestService), config, new Uri(DummyUri));
            var httpEndpoint = (HttpEndpoint)host.Description.Endpoints.Find(typeof(TestService));
            Assert.AreEqual(config.OperationHandlerFactory, httpEndpoint.OperationHandlerFactory);                        
        }

        [TestMethod]
        public void WhenHttpEndpointIsCreatedThenMessageHandlerFactoryIsSet()
        {
            var config = new HttpHostConfiguration();
            config.MessageHandlerFactory = new TestMessageHandlerFactory();
            var host = new HttpConfigurableServiceHost(typeof(TestService), config, new Uri(DummyUri));
            var httpEndpoint = (HttpEndpoint)host.Description.Endpoints.Find(typeof(TestService));
            Assert.AreEqual(config.MessageHandlerFactory, httpEndpoint.MessageHandlerFactory);                        
        }

        [TestMethod]
        public void WhenHttpEndpointIsCreatedThenInstanceProviderBehaviorIsAdded()
        {
            var config = new HttpHostConfiguration();
            config.InstanceFactory = new TestInstanceFactory();
            var host = new HttpConfigurableServiceHost(typeof(TestService), config, new Uri(DummyUri));
            var httpEndpoint = (HttpEndpoint)host.Description.Endpoints.Find(typeof(TestService));
            Assert.IsTrue(httpEndpoint.Behaviors.Contains(typeof (InstanceProviderBehavior)));
        }


       
    }
}
