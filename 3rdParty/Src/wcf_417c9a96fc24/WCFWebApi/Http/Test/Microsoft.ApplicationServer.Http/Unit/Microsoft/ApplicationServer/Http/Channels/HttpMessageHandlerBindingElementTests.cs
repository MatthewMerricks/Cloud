// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Channels
{
    using System;
    using System.ServiceModel.Channels;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Mocks;

    [TestClass]
    public class HttpMessageHandlerBindingElementTests
    {
        #region Type Tests

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerBindingElement is public non-abstract class.")]
        public void HttpMessageHandlerBindingElement_Is_A_Public_Non_Abstract_Class()
        {
            Type type = typeof(HttpMessageHandlerBindingElement);
            Assert.IsTrue(type.IsPublic, "HttpMessageHandlerBindingElement should be abstract.");
            Assert.IsFalse(type.IsAbstract, "HttpMessageHandlerBindingElement should not be abstract.");
            Assert.IsTrue(type.IsClass, "HttpMessageHandlerBindingElement should be a class.");
            Assert.IsFalse(type.IsGenericType, "HttpMessageHandlerBindingElement should not be a generic type."); ;          
            Assert.IsFalse(type.IsSealed, "HttpMessageHandlerBindingElement should not be sealed.");
            Assert.IsFalse(typeof(IDisposable).IsAssignableFrom(type), "HttpMessageHandlerBindingElement does not implement IDisposable."); ;
            Assert.IsTrue(typeof(BindingElement).IsAssignableFrom(type), "HttpMessageHandlerBindingElement does derive from BindingElement.");
        }

        #endregion Type Tests

        #region Constructor Tests

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerBindingElement has a parameterless constructor.")]
        public void HttpMessageHandlerBindingElement_Has_Parameterless_Constructor()
        {
            HttpMessageHandlerBindingElement bindingElement = new HttpMessageHandlerBindingElement();
            Assert.IsTrue(typeof(BindingElement).IsAssignableFrom(bindingElement.GetType()), "HttpMessageHandlerBindingElement should be a BindingElement");
        }

        #endregion Constructor Tests

        #region HttpMessageHandlerFactory Property Tests

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerBindingElement.HttpMessageHandlerFactory property is Get and Set.")]
        public void HttpMessageHandlerFactory_Is_Get_Set()
        {
            HttpMessageHandlerBindingElement bindingElement = new HttpMessageHandlerBindingElement();

            HttpMessageHandlerFactory factory = new HttpMessageHandlerFactory();
            bindingElement.MessageHandlerFactory = factory;
            Assert.AreSame(factory, bindingElement.MessageHandlerFactory, "The HttpMessageHandlerFactory should refer to the factory that was set.");
        }

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerBindingElement.HttpMessageHandlerFactory property is null be default.")]
        public void HttpMessageHandlerFactory_Is_Null_By_Default()
        {
            HttpMessageHandlerBindingElement bindingElement = new HttpMessageHandlerBindingElement();
            Assert.IsNull(bindingElement.MessageHandlerFactory, "The HttpMessageHandlerFactory should be null by default.");
        }

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerBindingElement.HttpMessageHandlerFactory property can be set to null.")]
        public void HttpMessageHandlerFactory_Can_Be_Set_To_Null()
        {
            HttpMessageHandlerBindingElement bindingElement = new HttpMessageHandlerBindingElement();

            HttpMessageHandlerFactory factory = new HttpMessageHandlerFactory();
            bindingElement.MessageHandlerFactory = factory;
            bindingElement.MessageHandlerFactory = null;
            Assert.IsNull(bindingElement.MessageHandlerFactory, "The HttpMessageHandlerFactory should be null as it was set to null.");
        }

        #endregion HttpMessageHandlerFactory Property Tests

        #region BuildChannelFactory Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerBindingElement.BuildChannelFactory throws regardless of channel shape.")]
        public void BuildChannelFactory_Throws()
        {
            HttpMessageHandlerBindingElement bindingElement = new HttpMessageHandlerBindingElement();

            ExceptionAssert.Throws<NotSupportedException>(
                SR.HttpMessageHandlerChannelFactoryNotSupported(typeof(HttpMessageHandlerBindingElement).Name, typeof(IChannelFactory<IReplyChannel>).Name),
                () =>
                {
                    bindingElement.BuildChannelFactory<IReplyChannel>(MockBindingContext.Create());
                });
        }

        #endregion BuildChannelFactory Tests

        #region CanBuildChannelFactory Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerBindingElement.CanBuildChannelFactory always returns 'false' regardless of channel shape.")]
        public void CanBuildChannelFactory_Always_Returns_False()
        {
            HttpMessageHandlerBindingElement bindingElement = new HttpMessageHandlerBindingElement();
            Assert.IsFalse(bindingElement.CanBuildChannelFactory<IReplyChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelFactory should always return 'false'.");
            Assert.IsFalse(bindingElement.CanBuildChannelFactory<IReplySessionChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelFactory should always return 'false'.");
            Assert.IsFalse(bindingElement.CanBuildChannelFactory<IRequestChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelFactory should always return 'false'.");
            Assert.IsFalse(bindingElement.CanBuildChannelFactory<IRequestSessionChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelFactory should always return 'false'.");
            Assert.IsFalse(bindingElement.CanBuildChannelFactory<IOutputChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelFactory should always return 'false'.");
            Assert.IsFalse(bindingElement.CanBuildChannelFactory<IOutputSessionChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelFactory should always return 'false'.");
            Assert.IsFalse(bindingElement.CanBuildChannelFactory<IInputChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelFactory should always return 'false'.");
            Assert.IsFalse(bindingElement.CanBuildChannelFactory<IInputSessionChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelFactory should always return 'false'.");
            Assert.IsFalse(bindingElement.CanBuildChannelFactory<IDuplexChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelFactory should always return 'false'.");
            Assert.IsFalse(bindingElement.CanBuildChannelFactory<IDuplexSessionChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelFactory should always return 'false'.");
        }

        #endregion CanBuildChannelFactory Tests

        #region BuildChannelListener Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerBindingElement.BuildChannelListener returns an HttpMessageHandlerChannelListener for IReplyChannel.")]
        public void BuildChannelListener_Returns_ChannelListener_For_IReplyChannel()
        {
            HttpMessageHandlerBindingElement bindingElement = new HttpMessageHandlerBindingElement();
            IChannelListener<IReplyChannel> listener = bindingElement.BuildChannelListener<IReplyChannel>(MockBindingContext.Create());
            Assert.IsNotNull(listener, "HttpMessageHandlerBindingElement.BuildChannelListener should have returned an instance.");
            Assert.IsInstanceOfType(listener, typeof(HttpMessageHandlerChannelListener), "HttpMessageHandlerBindingElement.BuildChannelListener should have returned an HttpMessageHandlerChannelListener.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerBindingElement.BuildChannelListener throws for non-IReplyChannel.")]
        public void BuildChannelListener_Throws_For_Non_IReplyChannel()
        {
            HttpMessageHandlerBindingElement bindingElement = new HttpMessageHandlerBindingElement();

            ExceptionAssert.Throws<NotSupportedException>(
                SR.HttpMessageHandlerChannelInvalidChannelShape(typeof(IReplySessionChannel).Name, typeof(HttpMessageHandlerBindingElement).Name, typeof(IReplyChannel).Name),
                () =>
                {
                    bindingElement.BuildChannelListener<IReplySessionChannel>(MockBindingContext.Create());
                });

            ExceptionAssert.Throws<NotSupportedException>(
                SR.HttpMessageHandlerChannelInvalidChannelShape(typeof(IRequestChannel).Name, typeof(HttpMessageHandlerBindingElement).Name, typeof(IReplyChannel).Name),
                () =>
                {
                    bindingElement.BuildChannelListener<IRequestChannel>(MockBindingContext.Create());
                });

            ExceptionAssert.Throws<NotSupportedException>(
                SR.HttpMessageHandlerChannelInvalidChannelShape(typeof(IRequestSessionChannel).Name, typeof(HttpMessageHandlerBindingElement).Name, typeof(IReplyChannel).Name),
                () =>
                {
                    bindingElement.BuildChannelListener<IRequestSessionChannel>(MockBindingContext.Create());
                });

            ExceptionAssert.Throws<NotSupportedException>(
                SR.HttpMessageHandlerChannelInvalidChannelShape(typeof(IOutputChannel).Name, typeof(HttpMessageHandlerBindingElement).Name, typeof(IReplyChannel).Name),
                () =>
                {
                    bindingElement.BuildChannelListener<IOutputChannel>(MockBindingContext.Create());
                });

            ExceptionAssert.Throws<NotSupportedException>(
                SR.HttpMessageHandlerChannelInvalidChannelShape(typeof(IOutputSessionChannel).Name, typeof(HttpMessageHandlerBindingElement).Name, typeof(IReplyChannel).Name),
                () =>
                {
                    bindingElement.BuildChannelListener<IOutputSessionChannel>(MockBindingContext.Create());
                });

            ExceptionAssert.Throws<NotSupportedException>(
                SR.HttpMessageHandlerChannelInvalidChannelShape(typeof(IInputChannel).Name, typeof(HttpMessageHandlerBindingElement).Name, typeof(IReplyChannel).Name),
                () =>
                {
                    bindingElement.BuildChannelListener<IInputChannel>(MockBindingContext.Create());
                });

            ExceptionAssert.Throws<NotSupportedException>(
                SR.HttpMessageHandlerChannelInvalidChannelShape(typeof(IInputSessionChannel).Name, typeof(HttpMessageHandlerBindingElement).Name, typeof(IReplyChannel).Name),
                () =>
                {
                    bindingElement.BuildChannelListener<IInputSessionChannel>(MockBindingContext.Create());
                });

            ExceptionAssert.Throws<NotSupportedException>(
                SR.HttpMessageHandlerChannelInvalidChannelShape(typeof(IDuplexChannel).Name, typeof(HttpMessageHandlerBindingElement).Name, typeof(IReplyChannel).Name),
                () =>
                {
                    bindingElement.BuildChannelListener<IDuplexChannel>(MockBindingContext.Create());
                });

            ExceptionAssert.Throws<NotSupportedException>(
                SR.HttpMessageHandlerChannelInvalidChannelShape(typeof(IDuplexSessionChannel).Name, typeof(HttpMessageHandlerBindingElement).Name, typeof(IReplyChannel).Name),
                () =>
                {
                    bindingElement.BuildChannelListener<IDuplexSessionChannel>(MockBindingContext.Create());
                });
        }

        #endregion BuildChannelListener Tests

        #region CanBuildChannelListener Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerBindingElement.CanBuildChannelListener only returns 'true' for IReplyChannel.")]
        public void CanBuildChannelListener_Only_Returns_True_For_IReplyChannel()
        {
            HttpMessageHandlerBindingElement bindingElement = new HttpMessageHandlerBindingElement();
            Assert.IsTrue(bindingElement.CanBuildChannelListener<IReplyChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelListener should have returned 'true'.");
            Assert.IsFalse(bindingElement.CanBuildChannelListener<IReplySessionChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelListener should always return 'false' for non IReplyChannel shapes.");
            Assert.IsFalse(bindingElement.CanBuildChannelListener<IRequestChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelListener should always return 'false' for non IReplyChannel shapes.");
            Assert.IsFalse(bindingElement.CanBuildChannelListener<IRequestSessionChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelListener should always return 'false' for non IReplyChannel shapes.");
            Assert.IsFalse(bindingElement.CanBuildChannelListener<IOutputChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelListener should always return 'false' for non IReplyChannel shapes.");
            Assert.IsFalse(bindingElement.CanBuildChannelListener<IOutputSessionChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelListener should always return 'false' for non IReplyChannel shapes.");
            Assert.IsFalse(bindingElement.CanBuildChannelListener<IInputChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelListener should always return 'false' for non IReplyChannel shapes.");
            Assert.IsFalse(bindingElement.CanBuildChannelListener<IInputSessionChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelListener should always return 'false' for non IReplyChannel shapes.");
            Assert.IsFalse(bindingElement.CanBuildChannelListener<IDuplexChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelListener should always return 'false' for non IReplyChannel shapes.");
            Assert.IsFalse(bindingElement.CanBuildChannelListener<IDuplexSessionChannel>(MockBindingContext.Create()), "HttpMessageHandlerBindingElement.CanBuildChannelListener should always return 'false' for non IReplyChannel shapes.");
        }

        #endregion CanBuildChannelListener Tests

        #region Clone Method Tests

        [TestMethod]
        [TestCategory("ADP_Basics"), TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpMessageHandlerBindingElement.Clone creates a new BindingElement.")]
        public void Clone_Creates_New_HttpMessageHandlerBindingElement()
        {
            HttpMessageHandlerBindingElement bindingElement = new HttpMessageHandlerBindingElement();
            HttpMessageHandlerBindingElement bindingElementClone = bindingElement.Clone() as HttpMessageHandlerBindingElement;
            Assert.IsNotNull(bindingElementClone, "Clone should have created a new HttpMessageHandlerBindingElement instance.");
            Assert.AreNotSame(bindingElement, bindingElementClone, "The cloned instance should be different from the original HttpMessageHandlerBindingElement instance.");
        }

        #endregion Clone Method Tests
    }
}