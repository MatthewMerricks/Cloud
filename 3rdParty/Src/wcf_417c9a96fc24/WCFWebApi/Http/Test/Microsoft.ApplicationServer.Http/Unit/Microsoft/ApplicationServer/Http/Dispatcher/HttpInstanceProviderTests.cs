// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Services;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.Dispatcher.Moles;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpInstanceProviderTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.CreateInstance calls protected OnCreateInstance")]
        public void CreateInstance_Calls_OnCreateInstance()
        {
            EmptyService instance = new EmptyService();
            InstanceContext context = new InstanceContext(instance);
            HttpRequestMessage requestMessage = new HttpRequestMessage();

            InstanceContext seenContext = null;
            HttpRequestMessage seenMessage = null;
            SHttpInstanceProvider provider = new SHttpInstanceProvider();
            provider.OnGetInstanceInstanceContextHttpRequestMessage =
                (ic, m) =>
                {
                    seenContext = ic;
                    seenMessage = m;
                    return "hello";
                };

            object actualInstance = provider.GetInstance(context, requestMessage);

            Assert.AreSame(context, seenContext, "Did not receive context in OnCreateInstance");
            Assert.AreSame(requestMessage, seenMessage, "Did not get Message");
            Assert.AreEqual("hello", actualInstance, "Did not pass back instance created");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.CreateInstance (simple form) calls protected OnCreateInstance")]
        public void CreateInstance_Simple_Calls_OnCreateInstance()
        {
            EmptyService instance = new EmptyService();
            InstanceContext context = new InstanceContext(instance);

            InstanceContext seenContext = null;
            SHttpInstanceProvider provider = new SHttpInstanceProvider();
            provider.OnGetInstanceInstanceContext = 
                (ic) =>
                {
                    seenContext = ic;
                    return "hello";
                };

            object actualInstance = provider.GetInstance(context);

            Assert.AreSame(context, seenContext, "Did not receive context in OnCreateInstance");
            Assert.AreEqual("hello", actualInstance, "Did not pass back instance created");
        }


        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.ReleaseInstance calls protected OnReleaseInstance")]
        public void ReleaseInstance_Calls_OnReleaseInstance()
        {
            EmptyService instance = new EmptyService();
            InstanceContext context = new InstanceContext(instance);

            InstanceContext seenContext = null;
            object seenInstance = null;
            SHttpInstanceProvider provider = new SHttpInstanceProvider();
            provider.OnReleaseInstanceInstanceContextObject =
                (ic, i) =>
                {
                    seenContext = ic;
                    seenInstance = i;
                };

            provider.ReleaseInstance(context, instance);

            Assert.AreSame(context, seenContext, "Did not receive context in OnCreateInstance");
            Assert.AreSame(instance, seenInstance, "Did not pass back instance created");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.CreateInstance returns custom instance")]
        public void CreateInstance_Returns_Custom_Instance()
        {
            EmptyService instance = new EmptyService();
            InstanceContext context = new InstanceContext(instance);
            Message message = new HttpRequestMessage().ToMessage();
            SHttpInstanceProvider provider = new SHttpInstanceProvider();
            provider.OnGetInstanceInstanceContextHttpRequestMessage = (ctx, msg) => instance;

            object returnedInstance = ((IInstanceProvider)provider).GetInstance(context, message);
            Assert.AreEqual(instance, returnedInstance, "Instance provider should have returned the same instance we provided to the mock");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.CreateInstance allows null instance")]
        public void CreateInstance_Allows_Null_Instance()
        {
            EmptyService instance = new EmptyService();
            InstanceContext context = new InstanceContext(instance);
            Message message = new HttpRequestMessage().ToMessage();

            SHttpInstanceProvider provider = new SHttpInstanceProvider();
            provider.OnGetInstanceInstanceContextHttpRequestMessage = (ctx, msg) => { return null; };

            object returnedInstance = ((IInstanceProvider)provider).GetInstance(context, message);
            Assert.IsNull(returnedInstance, "Instance provider should have returned a null instance");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.CreateInstance with null InstanceContext throws")]
        public void CreateInstance_Null_InstanceContext_Throws()
        {
            EmptyService instance = new EmptyService();
            InstanceContext context = null;
            IInstanceProvider provider = new SHttpInstanceProvider();
            Message message = new HttpRequestMessage().ToMessage();
            ExceptionAssert.ThrowsArgumentNull("instanceContext", () => provider.GetInstance(context, message));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.CreateInstance with null Message throws")]
        public void CreateInstance_Null_Message_Throws()
        {
            EmptyService instance = new EmptyService();
            InstanceContext context = new InstanceContext(instance);
            IInstanceProvider provider = new SHttpInstanceProvider();
            Message message = null;
            ExceptionAssert.ThrowsArgumentNull("message", () => provider.GetInstance(context, message));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.CreateInstance with null request throws")]
        public void CreateInstance_Null_Request_Throws()
        {
            EmptyService instance = new EmptyService();
            InstanceContext context = new InstanceContext(instance);
            HttpInstanceProvider provider = new SHttpInstanceProvider();
            ExceptionAssert.ThrowsArgumentNull("request", () => provider.GetInstance(context, null));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.CreateInstance with null instanceContext throws")]
        public void CreateInstance_HttpInstanceProvider_Null_InstanceContext_Throws()
        {
            HttpInstanceProvider provider = new SHttpInstanceProvider();
            ExceptionAssert.ThrowsArgumentNull("instanceContext", () => provider.GetInstance(null, new HttpRequestMessage()));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.CreateInstance (simple form) with null instanceContext throws")]
        public void CreateInstance_Simple_HttpInstanceProvider_Null_InstanceContext_Throws()
        {
            HttpInstanceProvider provider = new SHttpInstanceProvider();
            ExceptionAssert.ThrowsArgumentNull("instanceContext", () => provider.GetInstance(null));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.ReleaseInstance with null instanceContext throws")]
        public void ReleaseInstance_HttpInstanceProvider_Null_InstanceContext_Throws()
        {
            HttpInstanceProvider provider = new SHttpInstanceProvider();
            ExceptionAssert.ThrowsArgumentNull("instanceContext", () => provider.ReleaseInstance(null, "anInstance"));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.ReleaseInstance with null instance throws")]
        public void ReleaseInstance_HttpInstanceProvider_Null_Instance_Throws()
        {
            EmptyService instance = new EmptyService();
            InstanceContext context = new InstanceContext(instance);
            HttpInstanceProvider provider = new SHttpInstanceProvider();
            ExceptionAssert.ThrowsArgumentNull("instance", () => provider.ReleaseInstance(context, null));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpInstanceProvider.CreateInstance with non-http message throws")]
        public void CreateInstance_Non_Http_Message_Throws()
        {
            EmptyService instance = new EmptyService();
            InstanceContext context = new InstanceContext(instance);
            IInstanceProvider provider = new SHttpInstanceProvider();
            Message message = Message.CreateMessage(MessageVersion.None, "notUsed");
            ExceptionAssert.Throws<InvalidOperationException>(
                SR.HttpInstanceProviderNullRequest(typeof(SHttpInstanceProvider).Name, typeof(HttpRequestMessage).Name, "GetInstance"),
                () =>provider.GetInstance(context, message));
        }
    }
}
