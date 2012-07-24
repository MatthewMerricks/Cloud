// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Channels.Mocks;
    using System.Collections.ObjectModel;

    [TestClass]
    public class HttpMessageHandlerChannelFactoryTests
    {
        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        public void DefaultHttpMessagePluginFactory_Type1()
        {
            Type type = typeof(HttpMessageHandlerFactory);
            Assert.IsFalse(type.IsAbstract);
            Assert.IsTrue(type.IsClass);
            Assert.IsFalse(type.IsGenericType);
            Assert.IsTrue(type.IsPublic);
            Assert.IsFalse(type.IsSealed);
            Assert.IsFalse(typeof(IDisposable).IsAssignableFrom(type));
            Assert.IsTrue(typeof(HttpMessageHandlerFactory).IsAssignableFrom(type));
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DefaultHttpMessagePluginFactory_Constructor1()
        {
            var be = new HttpMessageHandlerFactory(null);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        public void DefaultHttpMessagePluginFactory_Constructor2()
        {
            var be = new HttpMessageHandlerFactory();
            Assert.IsNotNull(be);
            Assert.IsTrue(typeof(HttpMessageHandlerFactory).IsAssignableFrom(be.GetType()));
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [ExpectedException(typeof(ArgumentException))]
        public void DefaultHttpMessagePluginFactory_Constructor3()
        {
            var plugins = new Type[] { typeof(int) };
            var be = new HttpMessageHandlerFactory(plugins);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [ExpectedException(typeof(ArgumentException))]
        public void DefaultHttpMessagePluginFactory_Constructor4()
        {
            var plugins = new Type[] { typeof(DelegatingChannel) };
            var be = new HttpMessageHandlerFactory(plugins);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [ExpectedException(typeof(ArgumentException))]
        public void DefaultHttpMessagePluginFactory_Constructor5()
        {
            var plugins = new Type[] { typeof(InvalidConstructorHandler) };
            var be = new HttpMessageHandlerFactory(plugins);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [ExpectedException(typeof(ArgumentException))]
        public void DefaultHttpMessagePluginFactory_Constructor6()
        {
            var plugins = new Type[] { typeof(ValidConstructorHandler), null };
            var be = new HttpMessageHandlerFactory(plugins);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [ExpectedException(typeof(ArgumentException))]
        public void DefaultHttpMessagePluginFactory_Constructor7()
        {
            var plugins = new Type[] { typeof(ValidConstructorHandler), typeof(InvalidConstructorHandler) };
            var be = new HttpMessageHandlerFactory(plugins);
        }

        private class InvalidConstructorHandler : DelegatingChannel
        {
            public InvalidConstructorHandler(HttpMessageChannel innerChannel, bool test) : base(innerChannel) { }
        }

        private class ValidConstructorHandler : DelegatingChannel
        {
            public ValidConstructorHandler(HttpMessageChannel innerChannel) : base(innerChannel) { }
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        public void DefaultHttpMessagePluginFactory_Property1()
        {
            var be = new HttpMessageHandlerFactory();
            var plugins = be.HttpMessageHandlers;
            Assert.IsNotNull(plugins);
            Assert.AreEqual(0, plugins.Count());
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DefaultHttpMessagePluginFactory_Method1()
        {
            var be = new HttpMessageHandlerFactory();
            be.Create(null);
        }
    }
}