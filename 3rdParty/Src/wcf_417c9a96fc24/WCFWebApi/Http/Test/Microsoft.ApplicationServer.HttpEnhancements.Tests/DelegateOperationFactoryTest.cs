using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Microsoft.ApplicationServer.HttpEnhancements.Tests
{
    using System.Collections.ObjectModel;
    using System.ServiceModel.Description;

    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Http.Dispatcher;

    [TestClass]
    public class DelegateOperationFactoryTest
    {
        [TestMethod]
        public void WhenCreatedThenFormattersAreSet()
        {
            var formatters = new List<MediaTypeFormatter>();
            var formatter = new TestMediaTypeFormatter();
            formatters.Add(formatter);
            var factory = new DelegateOperationHandlerFactory(formatters, null, null);
            Assert.IsTrue(formatters.Contains(formatter));
        }

        [TestMethod]
        public void WhenCreatedThenRequestHandlersAreSet()
        {
            var handlers = new List<OperationHandlerItem>();
            var factory = new DelegateOperationHandlerFactory(Enumerable.Empty<MediaTypeFormatter>(), handlers, null);
            Assert.AreEqual(handlers, factory.createRequestHandlers);
        }

        [TestMethod]
        public void WhenCreatedThenResponseHandlersAreSet()
        {
            var handlers = new List<OperationHandlerItem>();
            var factory = new DelegateOperationHandlerFactory(Enumerable.Empty<MediaTypeFormatter>(), null, handlers);
            Assert.AreEqual(handlers, factory.createResponseHandlers);
        }

        [TestMethod]
        public void WhenCreateRequestHandlersIsCalledThenConditionIsCheckedOnHandlerItem()
        {
            bool conditionChecked = false;
            var handlerItem = new OperationHandlerItem(c => { }, (e, o) =>
            {
                conditionChecked = true;
                return true;
            });
            var handlers = new List<OperationHandlerItem> { handlerItem };
            var factory = new DelegateOperationHandlerFactory(Enumerable.Empty<MediaTypeFormatter>(), handlers, null);
            var contract = ContractDescription.GetContract(typeof (DummyService));
            factory.CreateRequestHandlers(new HttpEndpoint(contract), new HttpOperationDescription());
            Assert.IsTrue(conditionChecked);
        }

        [TestMethod]
        public void WhenCreateResponseHandlersIsCalledThenConditionIsCheckedOnHandlerItem()
        {
            bool conditionChecked = false;
            var handlerItem = new OperationHandlerItem(c => { }, (e, o) =>
            {
                conditionChecked = true;
                return true;
            });
            var handlers = new List<OperationHandlerItem> { handlerItem };
            var factory = new DelegateOperationHandlerFactory(Enumerable.Empty<MediaTypeFormatter>(), null, handlers);
            var contract = ContractDescription.GetContract(typeof(DummyService));
            factory.CreateResponseHandlers(new HttpEndpoint(contract), new HttpOperationDescription());
            Assert.IsTrue(conditionChecked);
        }

        [TestMethod]
        public void WhenCreateRequestHandlersIsCalledThenActionIsCalledOnHandlerItem()
        {
            bool actionCalled = false;
            var handlerItem = new OperationHandlerItem(c => actionCalled = true, (e, o) => true);
            var handlers = new List<OperationHandlerItem> { handlerItem };
            var factory = new DelegateOperationHandlerFactory(Enumerable.Empty<MediaTypeFormatter>(), handlers, null);
            var contract = ContractDescription.GetContract(typeof(DummyService));
            factory.CreateRequestHandlers(new HttpEndpoint(contract), new HttpOperationDescription());
            Assert.IsTrue(actionCalled);
        }


        [TestMethod]
        public void WhenCreateResponseHandlerIsCalledThenActionIsCalledOnHandlerItem()
        {
            bool actionCalled = false;
            var handlerItem = new OperationHandlerItem(c => actionCalled = true, (e, o) => true);
            var handlers = new List<OperationHandlerItem> { handlerItem };
            var factory = new DelegateOperationHandlerFactory(Enumerable.Empty<MediaTypeFormatter>(),null,handlers);
            var contract = ContractDescription.GetContract(typeof(DummyService));
            factory.CreateResponseHandlers(new HttpEndpoint(contract), new HttpOperationDescription());
            Assert.IsTrue(actionCalled);
        }


        [ServiceContract]
        public class DummyService
        {
            
        }
    }


}