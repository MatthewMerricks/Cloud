// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.Dispatcher.Moles;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpOperationSelectorTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationSelector.SelectOperation returns custom operation name")]
        public void SelectOperation_Returns_Custom_Operation_Name()
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
            Message message = httpRequestMessage.ToMessage();

            SHttpOperationSelector selector = new SHttpOperationSelector();
            selector.OnSelectOperationHttpRequestMessage =
                (localHttpRequestMessag) =>
                {
                    Assert.AreSame(httpRequestMessage, localHttpRequestMessag, "The 'OnSelectOperation' method should have been called with the same HttpRequestMessage instance.");
                    return "CustomOperation";
                };
            
            string returnedOperation = ((IDispatchOperationSelector)selector).SelectOperation(ref message);
            Assert.AreEqual("CustomOperation", returnedOperation, "SelectOperation should have returned the custom operation name.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationSelector.SelectOperation throws ArgumentNullException for null message")]
        public void SelectOperation_Null_Message_Throws()
        {
            IDispatchOperationSelector selector = new SHttpOperationSelector();
            Message message = null;
            ExceptionAssert.ThrowsArgumentNull("message", () => selector.SelectOperation(ref message));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationSelector.SelectOperation(HttpRequestMessage) throws ArgumentNullException for null message")]
        public void SelectOperation_Null_HttpRequestMessage_Throws()
        {
            HttpOperationSelector selector = new SHttpOperationSelector();
            HttpRequestMessage message = null;
            ExceptionAssert.ThrowsArgumentNull("message", () => selector.SelectOperation(message));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationSelector.SelectOperation throws InvalidOperationException for non-http message")]
        public void SelectOperation_Non_Http_Message_Throws()
        {
            IDispatchOperationSelector selector = new SHttpOperationSelector();
            Message message = Message.CreateMessage(MessageVersion.None, "notUsed");
            ExceptionAssert.Throws<InvalidOperationException>(
                SR.HttpOperationSelectorNullRequest(typeof(SHttpOperationSelector).Name, typeof(HttpRequestMessage).Name, "SelectOperation"),
                () => selector.SelectOperation(ref message));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationSelector.SelectOperation throws if null operation is returned")]
        public void SelectOperation_Null_Return_Throws()
        {
            Message message = new HttpRequestMessage().ToMessage();

            SHttpOperationSelector selector = new SHttpOperationSelector();
            selector.OnSelectOperationHttpRequestMessage = (localMessage) => null;

            ExceptionAssert.Throws<InvalidOperationException>(
                SR.HttpOperationSelectorNullOperation(typeof(SHttpOperationSelector).Name),
                () => ((IDispatchOperationSelector)selector).SelectOperation(ref message));
        }
    }
}
