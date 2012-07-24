// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.ApplicationServer.Http.Dispatcher.Moles;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test.Types;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class HttpOperationHandlerTests : UnitTest<HttpOperationHandler>
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpOperationHandler is public and abstract.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "HttpOperationHandler should be public.");
            Assert.IsTrue(t.IsAbstract, "HttpOperationHandler should be abstract.");
            Assert.IsTrue(t.IsClass, "HttpOperationHandler should be a class.");
        }

        #endregion Type

        #region Constructors
        #endregion Constructors

        #region Properties

        #region InputParameters

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("InputParameters invokes virtual OnGetInputParmeters.")]
        public void InputParametersCallsOnGetInputParmeters()
        {
            SHttpOperationHandler handler = new SHttpOperationHandler();
            bool wereCalled = false;
            handler.OnGetInputParameters01 = () => { wereCalled = true; return new HttpParameter[0]; };
            ReadOnlyCollection<HttpParameter> arguments = handler.InputParameters;
            Assert.IsTrue(wereCalled, "OnGetInputParameters was not called.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("InputParameters does not invoke virtual OnGetInputParmeters more than once.")]
        public void InputParametersDoesNotCallOnGetInputParmetersTwice()
        {
            SHttpOperationHandler handler = new SHttpOperationHandler();
            int callCount = 0;
            handler.OnGetInputParameters01 = () => { ++callCount; return new HttpParameter[0]; };
            ReadOnlyCollection<HttpParameter> arguments1 = handler.InputParameters;
            ReadOnlyCollection<HttpParameter> arguments2 = handler.InputParameters;
            Assert.AreEqual(1, callCount, "OnGetInputParameters was called more than once.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("InputParameters returns a ReadOnlyCollection<HttpParameter>.")]
        public void InputParametersReturnsReadOnlyCollection()
        {
            SHttpOperationHandler handler = new SHttpOperationHandler();
            handler.OnGetInputParameters01 = () => new HttpParameter[] { new HttpParameter("arg1", typeof(string)) };
            ReadOnlyCollection<HttpParameter> arguments = handler.InputParameters;
            Assert.IsNotNull(arguments, "InputParameters should never be null.");
            Assert.AreEqual(1, arguments.Count, "InputParameters.Count should have been 1.");
            HttpParameter hpd = arguments[0];
            Assert.AreEqual("arg1", hpd.Name, "Did not set inputParameters[0] corectly.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("InputParameters invokes virtual OnGetInputParmeters, accepts a null return and produces an empty collection.")]
        public void InputParametersAcceptsNullFromOnGetInputParmeters()
        {
            SHttpOperationHandler handler = new SHttpOperationHandler();
            bool wereCalled = false;
            handler.OnGetInputParameters01 = () => { wereCalled = true; return null; };
            ReadOnlyCollection<HttpParameter> arguments = handler.InputParameters;
            Assert.IsTrue(wereCalled, "OnGetInputParameters was not called.");
            Assert.AreEqual(0, arguments.Count, "Collection should have been empty.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("InputParameters preserves the order given by OnGetInputParameters.")]
        public void InputParametersPreservesOrderFromOnGetInputParameters()
        {
            HttpParameter[] parameters = new HttpParameter[] {
                new HttpParameter("arg1", typeof(string)),
                new HttpParameter("arg2", typeof(int))
            };

            SHttpOperationHandler handler = new SHttpOperationHandler();
            handler.OnGetInputParameters01 = () => parameters;
            ReadOnlyCollection<HttpParameter> arguments = handler.InputParameters;
            HttpParameterAssert.AreEqual(parameters, arguments, "Order was not preserved.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("InputParameters clones the parameters given by OnGetInputParameters.")]
        public void InputParametersClonesParametersFromOnGetInputParameters()
        {
            HttpParameter[] parameters = new HttpParameter[] {
                new HttpParameter("arg1", typeof(string)),
            };

            SHttpOperationHandler handler = new SHttpOperationHandler();
            handler.OnGetInputParameters01 = () => parameters;
            ReadOnlyCollection<HttpParameter> arguments = handler.InputParameters;
            bool isContentParameterOriginal = parameters[0].IsContentParameter;
            bool isContentParameterCloned = arguments[0].IsContentParameter;
            Assert.AreEqual(isContentParameterOriginal, isContentParameterCloned, "IsContentParameter property was not properly cloned.");
            parameters[0].IsContentParameter = !isContentParameterOriginal;
            Assert.AreEqual(isContentParameterOriginal, isContentParameterCloned, "IsContentParameter property on original should not have affected clone.");
        }

        #endregion InputParameters

        #region OutputParameters

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OutputParameters invokes virtual OnGetOutputParmeters.")]
        public void OutputParametersCallsOnGetInputParmeters()
        {
            SHttpOperationHandler handler = new SHttpOperationHandler();
            bool wereCalled = false;
            handler.OnGetOutputParameters01 = () => { wereCalled = true; return new HttpParameter[0]; };
            ReadOnlyCollection<HttpParameter> arguments = handler.OutputParameters;
            Assert.IsTrue(wereCalled, "OnGetOutputParameters was not called.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OutputParameters does not invoke virtual OnGetOutputParmeters more than once.")]
        public void OutputParametersDoesNotCallOnGetOutputParmetersTwice()
        {
            SHttpOperationHandler handler = new SHttpOperationHandler();
            int callCount = 0;
            handler.OnGetOutputParameters01 = () => { ++callCount; return new HttpParameter[0]; };
            ReadOnlyCollection<HttpParameter> arguments1 = handler.OutputParameters;
            ReadOnlyCollection<HttpParameter> arguments2 = handler.OutputParameters;
            Assert.AreEqual(1, callCount, "OnGetOutputParameters was called more than once.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OutputParameters returns a ReadOnlyCollection<HttpParameter>.")]
        public void OutputParametersReturnsReadOnlyCollection()
        {
            SHttpOperationHandler handler = new SHttpOperationHandler();
            handler.OnGetOutputParameters01 = () => new HttpParameter[] { new HttpParameter("arg1", typeof(string)) };
            ReadOnlyCollection<HttpParameter> arguments = handler.OutputParameters;
            Assert.IsNotNull(arguments, "OutputParameters should never be null.");
            Assert.AreEqual(1, arguments.Count, "OutputParameters.Count should have been 1.");
            HttpParameter hpd = arguments[0];
            Assert.AreEqual("arg1", hpd.Name, "Did not set OutputParameters[0] corectly.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OutputParameters invokes virtual OnGetOutputParmeters, accepts a null return and produces an empty collection.")]
        public void OutputParametersAcceptsNullFromOnGetOutputParmeters()
        {
            SHttpOperationHandler handler = new SHttpOperationHandler();
            bool wereCalled = false;
            handler.OnGetOutputParameters01 = () => { wereCalled = true; return null; };
            ReadOnlyCollection<HttpParameter> arguments = handler.OutputParameters;
            Assert.IsTrue(wereCalled, "OnGetOutputParameters was not called.");
            Assert.AreEqual(0, arguments.Count, "Collection should have been empty.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OutputParameters preserves the order given by OnGetInputParameters.")]
        public void OutputParametersPreservesOrderFromOnGetOutputParameters()
        {
            HttpParameter[] parameters = new HttpParameter[] {
                new HttpParameter("arg1", typeof(string)),
                new HttpParameter("arg2", typeof(int))
            };

            SHttpOperationHandler handler = new SHttpOperationHandler();
            handler.OnGetOutputParameters01 = () => parameters;
            ReadOnlyCollection<HttpParameter> arguments = handler.OutputParameters;
            HttpParameterAssert.AreEqual(parameters, arguments, "Order was not preserved.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OutputParameters clones the parameters given by OnGetOutputParameters.")]
        public void OutputParametersClonesParametersFromOnGetInputParameters()
        {
            HttpParameter[] parameters = new HttpParameter[] {
                new HttpParameter("arg1", typeof(string)),
            };

            SHttpOperationHandler handler = new SHttpOperationHandler();
            handler.OnGetOutputParameters01 = () => parameters;
            ReadOnlyCollection<HttpParameter> arguments = handler.OutputParameters;
            bool isContentParameterOriginal = parameters[0].IsContentParameter;
            bool isContentParameterCloned = arguments[0].IsContentParameter;
            Assert.AreEqual(isContentParameterOriginal, isContentParameterCloned, "IsContentParameter property was not properly cloned.");
            parameters[0].IsContentParameter = !isContentParameterOriginal;
            Assert.AreEqual(isContentParameterOriginal, isContentParameterCloned, "IsContentParameter property on original should not have affected clone.");
        }

        #endregion OutputParameters

        #endregion Properties

        #region Methods

        #region Handle(object[])

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) throws with null Input.")]
        public void HandleThrowsWithNullInput()
        {
            HttpParameter[] parameters = new HttpParameter[] {
                new HttpParameter("arg1", typeof(string)),
            };

            SHttpOperationHandler handler = new SHttpOperationHandler() { CallBase = true };
            handler.OnGetInputParameters01 = () => parameters;
            handler.OnGetOutputParameters01 = () => parameters;

            ExceptionAssert.ThrowsArgumentNull("input", () => handler.Handle(null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) throws with Input shorter than expected.")]
        public void HandleThrowsWithTooSmallInput()
        {
            HttpParameter[] parameters = new HttpParameter[] {
                new HttpParameter("arg1", typeof(string)),
            };

            SHttpOperationHandler handler = new SHttpOperationHandler() { CallBase = true };
            handler.OnGetInputParameters01 = () => parameters;
            handler.OnGetOutputParameters01 = () => parameters;

            string errorMessage = SR.HttpOperationHandlerReceivedWrongNumberOfValues(
                                    typeof(HttpOperationHandler).Name,
                                    handler.ToString(),
                                    handler.OperationName,
                                    parameters.Length,
                                    0);

            ExceptionAssert.Throws<InvalidOperationException>(errorMessage, () => handler.Handle(new object[0]));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) throws with Input longer than expected.")]
        public void HandleThrowsWithTooLargeInput()
        {
            HttpParameter[] parameters = new HttpParameter[] {
                new HttpParameter("arg1", typeof(string)),
            };

            SHttpOperationHandler handler = new SHttpOperationHandler() { CallBase = true };
            handler.OnGetInputParameters01 = () => parameters;
            handler.OnGetOutputParameters01 = () => parameters;

            string errorMessage = SR.HttpOperationHandlerReceivedWrongNumberOfValues(
                                    typeof(HttpOperationHandler).Name,
                                    handler.ToString(),
                                    handler.OperationName,
                                    parameters.Length,
                                    2);

            ExceptionAssert.Throws<InvalidOperationException>(errorMessage, () => handler.Handle(new object[2]));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) throws with Input that cannot be converted to expected types.")]
        public void HandleThrowsWitUnconvertableInput()
        {
            HttpParameter[] parameters = new HttpParameter[] {
                new HttpParameter("arg1", typeof(string)),
            };

            SHttpOperationHandler handler = new SHttpOperationHandler() { CallBase = true };
            handler.OnGetInputParameters01 = () => parameters;
            handler.OnGetOutputParameters01 = () => parameters; 

            string errorMessage = SR.HttpOperationHandlerReceivedWrongType(
                                    typeof(HttpOperationHandler).Name,
                                    handler.ToString(),
                                    handler.OperationName,
                                    parameters[0].Type.Name,
                                    parameters[0].Name,
                                    typeof(PocoType).Name);

            ExceptionAssert.Throws<InvalidOperationException>(errorMessage, () => handler.Handle(new object[] { new PocoType() }));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) converts string Input to required type.")]
        public void HandleConvertsStringInput()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AllSingleInstances,
                "Handle string input failed",
                (type, obj) =>
                {
                    Type convertType = obj.GetType();
                    if (HttpParameterAssert.CanConvertToStringAndBack(convertType))
                    {
                        HttpParameter hpd = new HttpParameter("aName", convertType);
                        HttpParameter[] parameters = new HttpParameter[] { hpd };

                        SHttpOperationHandler handler = new SHttpOperationHandler();
                        handler.OnGetInputParameters01 = () => parameters;
                        handler.OnGetOutputParameters01 = () => parameters;
                        handler.OnHandleObjectArray = (oArray) => oArray;

                        object[] result = handler.Handle(new object[] { obj.ToString() });
                        Assert.IsNotNull(result, "Null result returned from Handle.");
                        Assert.AreEqual(1, result.Length, "Handle returned wrong length array.");
                        Assert.AreEqual(convertType, result[0].GetType(), "Value did not convert to right type.");
                        Assert.AreEqual(obj.ToString(), result[0].ToString(), "Object did not convert to the right value.");
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle().")]
        public void HandleCallsOnHandle()
        {
            HttpParameter[] parameters = new HttpParameter[] {
                new HttpParameter("arg1", typeof(string)),
            };

            SHttpOperationHandler handler = new SHttpOperationHandler();
            bool called = false;
            handler.OnGetInputParameters01 = () => parameters;
            handler.OnGetOutputParameters01 = () => parameters;
            handler.OnHandleObjectArray = (oArray) => { called = true; return oArray; };

            handler.Handle(new object[] { "fred" });
            Assert.IsTrue(called, "Handle did not call OnHandle.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) returns empty array of the correct size when OnHandle() returns null.")]
        public void HandleReturnsEmptyArrayIfOnHandleReturnsNull()
        {
            HttpParameter[] parameters = new HttpParameter[] {
                new HttpParameter("arg1", typeof(string)),
            };

            SHttpOperationHandler handler = new SHttpOperationHandler();
            handler.OnGetInputParameters01 = () => parameters;
            handler.OnGetOutputParameters01 = () => parameters;
            handler.OnHandleObjectArray = (oArray) => null;

            object[] result = handler.Handle(new object[] { "fred" });
            Assert.IsNotNull(result, "Handle returned null.");
            Assert.AreEqual(1, result.Length, "Handle returned wrong length array.");
            Assert.IsNull(result[0], "Handle did not return empty array.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) throws when OnHandle() returns an array smaller than promised.")]
        public void HandleThrowsIfOnHandleReturnsTooSmallArray()
        {
            HttpParameter[] parameters = new HttpParameter[] {
                new HttpParameter("arg1", typeof(string)),
            };

            SHttpOperationHandler handler = new SHttpOperationHandler() { CallBase = true };
            handler.OnGetInputParameters01 = () => parameters;
            handler.OnGetOutputParameters01 = () => parameters;
            handler.OnHandleObjectArray = (oArray) => new object[0];

            string errorMessage = SR.HttpOperationHandlerProducedWrongNumberOfValues(
                                    typeof(HttpOperationHandler).Name,
                                    handler.ToString(),
                                    handler.OperationName,
                                    1,
                                    0);

            ExceptionAssert.Throws<InvalidOperationException>(
                errorMessage,
                () => handler.Handle(new object[] { "fred" })
                );
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) throws when OnHandle() returns an array larger than promised.")]
        public void HandleThrowsIfOnHandleReturnsTooLargeArray()
        {
            HttpParameter[] parameters = new HttpParameter[] {
                new HttpParameter("arg1", typeof(string)),
            };

            SHttpOperationHandler handler = new SHttpOperationHandler() { CallBase = true };
            handler.OnGetInputParameters01 = () => parameters;
            handler.OnGetOutputParameters01 = () => parameters;
            handler.OnHandleObjectArray = (oArray) => new object[2];

            string errorMessage = SR.HttpOperationHandlerProducedWrongNumberOfValues(
                                    typeof(HttpOperationHandler).Name,
                                    handler.ToString(),
                                    handler.OperationName,
                                    1,
                                    2);

            ExceptionAssert.Throws<InvalidOperationException>(
                errorMessage,
                () => handler.Handle(new object[] { "fred" })
                );
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) throws when OnHandle() returns an array containing types that cannot be converted to what it promised.")]
        public void HandleThrowsIfOnHandleReturnsArrayContainingNonconvertableTypes()
        {
            HttpParameter hpd = new HttpParameter("arg1", typeof(PocoType));
            HttpParameter[] parameters = new HttpParameter[] { hpd };

            SHttpOperationHandler handler = new SHttpOperationHandler() { CallBase = true };
            handler.OnGetInputParameters01 = () => parameters;
            handler.OnGetOutputParameters01 = () => parameters;
            handler.OnHandleObjectArray = (oArray) => new object[] { "notAPocoType" };

            string errorMessage = SR.HttpOperationHandlerReceivedWrongType(
                                    typeof(HttpOperationHandler).Name,
                                    handler.ToString(),
                                    handler.OperationName,
                                    hpd.Type.Name,
                                    hpd.Name,
                                    typeof(string).Name);

            ExceptionAssert.Throws<InvalidOperationException>(
                errorMessage,
                () => handler.Handle(new object[] { "fred" })
                );
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) converts string values returned by OnHandle.")]
        public void HandleConvertsStringValuesReturnedFromOnHandle()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AllSingleInstances,
                "Handle failed",
                (type, obj) =>
                {
                    Type convertType = obj.GetType();
                    if (HttpParameterAssert.CanConvertToStringAndBack(convertType))
                    {
                        HttpParameter hpd = new HttpParameter("aName", convertType);
                        HttpParameter[] parameters = new HttpParameter[] { hpd };

                        SHttpOperationHandler handler = new SHttpOperationHandler();
                        handler.OnGetInputParameters01 = () => parameters;
                        handler.OnGetOutputParameters01 = () => parameters;
                        handler.OnHandleObjectArray = (oArray) => new object[] { obj.ToString() };

                        object[] result = handler.Handle(new object[] { obj });
                        Assert.IsNotNull(result, "Null result returned from Handle.");
                        Assert.AreEqual(1, result.Length, "Handle returned wrong length array.");
                        Assert.AreEqual(convertType, result[0].GetType(), "Value did not convert to right type.");
                        Assert.AreEqual(obj.ToString(), result[0].ToString(), "Object did not convert to the right value.");
                    }
                });
        }


        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) propagates any exception thrown from OnHandle.")]
        public void HandlePropagatesExceptionFromOnHandle()
        {
            HttpParameter hpd = new HttpParameter("arg1", typeof(int));
            HttpParameter[] parameters = new HttpParameter[] { hpd };

            SHttpOperationHandler handler = new SHttpOperationHandler();
            handler.OnGetInputParameters01 = () => parameters;
            handler.OnGetOutputParameters01 = () => parameters;
            handler.OnHandleObjectArray = (oArray) => { throw new NotSupportedException("myMessage"); };

            ExceptionAssert.Throws<NotSupportedException>(
                "myMessage",
                () => handler.Handle(new object[] { 5 })
                );
        }

        #endregion Handle(object[])

        #endregion Methods
    }
}
