using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.ApplicationServer.Http;
using Microsoft.ApplicationServer.Http.Channels;
using Microsoft.ApplicationServer.Http.Description;
using Microsoft.ApplicationServer.Http.Dispatcher;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationServer.HttpEnhancements.Tests
{
    public static class HttpHostConfigurationExtensions
    {
        public static HttpHostConfiguration ToConfiguration(this IHttpHostConfigurationBuilder builder)
        {
            return (HttpHostConfiguration) builder;
        }
    }

    [TestClass]
    public class HttpHostConfigurationTest
    {
        [TestMethod]
        public void WhenCreateIsCalledBuilderIsReturned()
        {
            var builder = HttpHostConfiguration.Create();
            Assert.IsNotNull(builder);
        }

        [TestMethod]
        public void WhenSetInstanceFactoryIsCalledWithAnInstanceThenInstanceFactoryIsSet()
        {
            var builder = HttpHostConfiguration.Create();
            var instanceFactory = new TestInstanceFactory();
            builder.SetResourceFactory(instanceFactory);
            Assert.AreEqual(instanceFactory, builder.ToConfiguration().InstanceFactory);
        }

        [TestMethod]
        public void WhenSetInstanceFactoryIsCalledPassingALambdaThenInstanceFactoryIsSetToDelegateFactory()
        {
            var builder = HttpHostConfiguration.Create();
            builder.SetResourceFactory((t, i, o) => null, (i, o) => { });
            Assert.IsNotNull(builder.ToConfiguration().InstanceFactory);
            Assert.IsTrue(builder.ToConfiguration().InstanceFactory is DelegateInstanceFactory);
        }

        [TestMethod]
        public void WhenSetOperationHandlerFactoryIsCalledWithAnInstanceThenOperationHandlerFactoryIsSet()
        {
            var builder = HttpHostConfiguration.Create();
            var factory = new TestOperationHandlerFactory();
            builder.SetOperationHandlerFactory(factory);
            Assert.AreEqual(factory, builder.ToConfiguration().OperationHandlerFactory);
        }

        [TestMethod]
        public void WhenAddRequestHandlersIsCalledThenHandlerItemIsAdded()
        {
            var builder = HttpHostConfiguration.Create();
            Action<Collection<HttpOperationHandler>> handlers = c => { };
            builder.AddRequestHandlers(handlers, (s, o) => true);
            var config = (HttpHostConfiguration) builder;
            Assert.AreEqual(handlers, config.requestHandlers[0].Handlers);
        }

        [TestMethod]
        public void WhenAddResponseHandlersIsCalledThenHandlerItemIsAdded()
        {
            var builder = HttpHostConfiguration.Create();
            Action<Collection<HttpOperationHandler>> handlers = c => { };
            builder.AddResponseHandlers(handlers, (s, o) => true);
            var config = (HttpHostConfiguration)builder;
            Assert.AreEqual(handlers, config.responseHandlers[0].Handlers);
        }

        [TestMethod]
        public void WhenAddFormattersIsCalledThenFormattersAreAdded()
        {
            var builder = HttpHostConfiguration.Create();
            var formatter = new TestMediaTypeFormatter();
            builder.AddFormatters(formatter);
            var config = (HttpHostConfiguration) builder;
            Assert.IsTrue(config.formatters.Contains(formatter));
        }

        [TestMethod]
        public void WhenSetMessageHandlerFactoryIsCalledWithAnInstanceThenMessageHandlerFactoryIsSet()
        {
            var builder = HttpHostConfiguration.Create();
            var factory = new TestMessageHandlerFactory();
            builder.SetMessageHandlerFactory(factory);
            Assert.AreEqual(factory, builder.ToConfiguration().MessageHandlerFactory);
        }

        [TestMethod]
        public void WhenSetMessageHandlerFactoryIsCalledPassingALamdaThenMessageHandlerFactoryIsSetToDelegateFactory()
        {
            var builder = HttpHostConfiguration.Create();
            builder.SetMessageHandlerFactory((c) => null);
            Assert.IsNotNull(builder.ToConfiguration().MessageHandlerFactory);
            Assert.IsTrue(builder.ToConfiguration().MessageHandlerFactory is DelegateMessageHandlerFactory);
        }

        [TestMethod]
        public void WhenSetErrorHandlerIsCalledWithAnInstanceThenErrorHandlerIsSet()
        {
            var builder = HttpHostConfiguration.Create();
            HttpErrorHandler errorHandler = new TestHttpErrorHandler();
            builder.SetErrorHandler(errorHandler);
        }

        [TestMethod]
        public void WhenSetErrorHandlerIsCalledPassingALamdaThenErrorHandlerIsSet()
        {
            var builder = HttpHostConfiguration.Create();
            builder.SetErrorHandler((e) => true, (e) => null);
            Assert.IsNotNull(builder.ToConfiguration().ErrorHandler);
            Assert.IsTrue(builder.ToConfiguration().ErrorHandler is DelegateErrorHandler);
        }




    }
}
