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
    public class HttpMessageHandlerFactoryTests
    {
        #region Type Tests

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerFactory is public non-abstract class.")]
        public void HttpMessageHandlerFactory_Is_A_Public_Non_Abstract_Class()
        {
            Type type = typeof(HttpMessageHandlerFactory);
            Assert.IsTrue(type.IsPublic, "HttpMessageHandlerFactory should be abstract.");
            Assert.IsFalse(type.IsAbstract, "HttpMessageHandlerFactory should not be abstract.");
            Assert.IsTrue(type.IsClass, "HttpMessageHandlerFactory should be a class.");
            Assert.IsFalse(type.IsGenericType, "HttpMessageHandlerFactory should not be a generic type."); ;
            Assert.IsFalse(type.IsSealed, "HttpMessageHandlerFactory should not be sealed.");
            Assert.IsFalse(typeof(IDisposable).IsAssignableFrom(type), "HttpMessageHandlerFactory does not implement IDisposable."); ;
        }

        #endregion Type Tests

        #region Constructor Tests

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerFactory constructor accepts an empty list of handlers.")]
        public void HttpMessageHandlerFactory_Constructor2()
        {
            HttpMessageHandlerFactory factory = new HttpMessageHandlerFactory();
            Assert.IsNotNull(factory);
        }

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerFactory constructor throws with null parameter.")]
        public void HttpMessageHandlerFactory_Constructor_Throws_With_Null_Parameter()
        {
            ExceptionAssert.ThrowsArgumentNull("handlers", () => new HttpMessageHandlerFactory(null));
        }

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerFactory constructor throws with non DelegatingChannel-derived types.")]
        public void HttpMessageHandlerFactory_Constructor_Throws_With_Non_DelegatingChannel_Derived_Types()
        {
            ExceptionAssert.Throws<ArgumentException>(
                SR.HttpMessageHandlerTypeNotSupported(typeof(int).Name, typeof(DelegatingChannel).Name, typeof(HttpMessageChannel).Name), 
                () => 
                {
                    new HttpMessageHandlerFactory(typeof(int));
                });
        }

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerFactory constructor throws with abstract types.")]
        public void HttpMessageHandlerFactory_Constructor_Throws_With_Abstract_Types()
        {
            ExceptionAssert.Throws<ArgumentException>(
                SR.HttpMessageHandlerTypeNotSupported(typeof(DelegatingChannel).Name, typeof(DelegatingChannel).Name, typeof(HttpMessageChannel).Name),
                () =>
                {
                    new HttpMessageHandlerFactory(typeof(DelegatingChannel));
                });
        }

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerFactory constructor throws with types that don't have a valid constructor.")]
        public void HttpMessageHandlerFactory_Constructor_Throws_With_Invalid_Constructor_Types()
        {
            ExceptionAssert.Throws<ArgumentException>(
                SR.HttpMessageHandlerTypeNotSupported(typeof(MockInvalidMessageHandler).Name, typeof(DelegatingChannel).Name, typeof(HttpMessageChannel).Name),
                () =>
                {
                    new HttpMessageHandlerFactory(typeof(MockInvalidMessageHandler));
                });
        }

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerFactory constructor throws with a null handler.")]
        public void HttpMessageHandlerFactory_Constructor_Throws_With_Null_Handler()
        {
            ExceptionAssert.Throws<ArgumentException>(
                SR.HttpMessageHandlerTypeNotSupported("null", typeof(DelegatingChannel).Name, typeof(HttpMessageChannel).Name),
                () =>
                {
                    new HttpMessageHandlerFactory(typeof(MockValidMessageHandler), null);
                });
        }

        #endregion Constructor Tests

        #region Count Property Tests

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerFactory.HttpMessageHandlers returns a read-only collection of handler types.")]
        public void HttpMessageHandlers_Returns_Handler_Types()
        {
            HttpMessageHandlerFactory factory = new HttpMessageHandlerFactory(typeof(MockValidMessageHandler));
            ReadOnlyCollection<Type> handlers = factory.HttpMessageHandlers;
            Assert.IsNotNull(handlers);
            Assert.AreEqual(1, handlers.Count());
        }

        #endregion Count Property Tests

        #region Create Method Tests

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerFactory.Create throws with a null parameter.")]
        public void Create_Throww_With_Null_Parameter()
        {
            HttpMessageHandlerFactory factory = new HttpMessageHandlerFactory(typeof(MockValidMessageHandler));
            ExceptionAssert.ThrowsArgumentNull("innerChannel", () => factory.Create(null));
        }

        #endregion Create Method Tests
    }
}