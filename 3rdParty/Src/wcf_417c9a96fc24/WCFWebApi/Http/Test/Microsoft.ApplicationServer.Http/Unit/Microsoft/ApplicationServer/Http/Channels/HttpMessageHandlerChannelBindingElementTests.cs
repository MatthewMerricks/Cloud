// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpMessageHandlerChannelBindingElementTests
    {
        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        public void HttpMessagePluginBindingElement_Type1()
        {
            Type type = typeof(HttpMessageHandlerBindingElement);
            Assert.IsFalse(type.IsAbstract);
            Assert.IsTrue(type.IsClass);
            Assert.IsFalse(type.IsGenericType);
            Assert.IsTrue(type.IsPublic);
            Assert.IsFalse(type.IsSealed);
            Assert.IsFalse(typeof(IDisposable).IsAssignableFrom(type));
            Assert.IsTrue(typeof(BindingElement).IsAssignableFrom(type));
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        public void HttpMessagePluginBindingElement_Constructor1()
        {
            var be = new HttpMessageHandlerBindingElement();
            Assert.IsNotNull(be);
            Assert.IsTrue(typeof(BindingElement).IsAssignableFrom(be.GetType()));
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        public void HttpMessagePluginBindingElement_Constructor2()
        {
            var be1 = new HttpMessageHandlerBindingElement();
            Assert.IsNotNull(be1);
            var be2 = be1.Clone();
            Assert.IsNotNull(be2);
            Assert.AreNotSame(be1, be2);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        public void HttpMessagePluginBindingElement_Property1()
        {
            var be = new HttpMessageHandlerBindingElement();

            be.MessageHandlerFactory = null;
            Assert.IsNull(be.MessageHandlerFactory);

            var factory = new HttpMessageHandlerFactory();
            be.MessageHandlerFactory = factory;
            Assert.ReferenceEquals(factory, be.MessageHandlerFactory);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        public void HttpMessagePluginBindingElement_Method1()
        {
            var be = new HttpMessageHandlerBindingElement();
            Assert.IsFalse(be.CanBuildChannelFactory<IReplyChannel>(null));
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        public void HttpMessagePluginBindingElement_Method2()
        {
            var be = new HttpMessageHandlerBindingElement();
            Assert.IsFalse(be.CanBuildChannelFactory<IRequestChannel>(null));
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [ExpectedException(typeof(NotSupportedException))]
        public void HttpMessagePluginBindingElement_Method3()
        {
            var be = new HttpMessageHandlerBindingElement();
            be.BuildChannelFactory<IReplyChannel>(null);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void HttpMessagePluginBindingElement_Method4()
        {
            var be = new HttpMessageHandlerBindingElement();
            Assert.IsTrue(be.CanBuildChannelListener<IReplyChannel>(null));
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void HttpMessagePluginBindingElement_Method5()
        {
            var be = new HttpMessageHandlerBindingElement();
            Assert.IsFalse(be.CanBuildChannelListener<IRequestChannel>(null));
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void HttpMessagePluginBindingElement_Method6()
        {
            var be = new HttpMessageHandlerBindingElement();
            var bc = new BindingContext(new CustomBinding(be), new BindingParameterCollection());
            var channel = be.BuildChannelListener<IReplyChannel>(bc);
        }

        [TestMethod]
        [TestCategory("ADP_Basics")]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [ExpectedException(typeof(NotSupportedException))]
        public void HttpMessagePluginBindingElement_Method7()
        {
            var be = new HttpMessageHandlerBindingElement();
            var bc = new BindingContext(new CustomBinding(be), new BindingParameterCollection());
            var channel = be.BuildChannelListener<IInputChannel>(bc);
        }
    }
}
