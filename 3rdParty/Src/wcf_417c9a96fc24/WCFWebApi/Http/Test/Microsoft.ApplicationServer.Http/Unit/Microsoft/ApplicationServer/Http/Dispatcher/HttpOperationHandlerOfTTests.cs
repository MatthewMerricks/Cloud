// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Dispatcher.Moles;
    using System.Collections.ObjectModel;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.Moles.Framework.Stubs;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class HttpOperationHandlerOfTTests : UnitTest<HttpOperationHandler<object, object>>
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpOperationHandlerOfT is public and abstract.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "HttpOperationHandlerOfT should be public.");
            Assert.IsTrue(t.IsAbstract, "HttpOperationHandlerOfT should be abstract");
        }

        #endregion Type

        #region Constructors

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpOperationHandlerOfT throws if the outputParameterName parameter is null.")]
        public void ConstructorThrowsWithNullOutputParameterName()
        {
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler01<int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler02<int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler03<int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler04<int, int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler05<int, int, int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler06<int, int, int, int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler07<int, int, int, int, int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler08<int, int, int, int, int, int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler09<int, int, int, int, int, int, int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler10<int, int, int, int, int, int, int, int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler11<int, int, int, int, int, int, int, int, int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler12<int, int, int, int, int, int, int, int, int, int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler13<int, int, int, int, int, int, int, int, int, int, int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler14<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler15<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(null));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler16<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpOperationHandlerOfT throws if the outputParameterName parameter is an empty or whitespace string.")]
        public void ConstructorThrowsWithEmptyOutputParameterName()
        {
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler01<int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler02<int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler03<int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler04<int, int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler05<int, int, int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler06<int, int, int, int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler07<int, int, int, int, int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler08<int, int, int, int, int, int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler09<int, int, int, int, int, int, int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler10<int, int, int, int, int, int, int, int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler11<int, int, int, int, int, int, int, int, int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler12<int, int, int, int, int, int, int, int, int, int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler13<int, int, int, int, int, int, int, int, int, int, int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler14<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler15<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(" "));
            ExceptionAssert.ThrowsArgumentNull("outputParameterName", () => new SHttpOperationHandler16<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>(" "));
        }

        #endregion Constructors

        #region Properties

        #region InputParameters

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("InputParameters returns the HttpParameters determined by reflecting over the generic HttpOperationHandlerOfT.")]
        public void InputParametersReturnsReflectedHttpParameters()
        {
            List<Type> types = new List<Type>();

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (!types.Contains(type))
                    {
                        types.Add(type);

                        for (int i = 2; i <= 17; i++)
                        {
                            if (types.Count - i < 0)
                            {
                                break;
                            }

                            Type[] typeArray = types.Skip(types.Count - i).ToArray();
                            HttpOperationHandler genericHandler = GetGenericHandlerForTypes(typeArray);

                            for (int j = 0; j < genericHandler.InputParameters.Count; j++)
                            {
                                HttpParameter parameter = genericHandler.InputParameters[j];
                                Assert.AreEqual(typeArray[j], parameter.Type, "The HttpParameter.Type should have been the same type as from the array.");
                                if (i == 2)
                                {
                                    Assert.AreEqual("input", parameter.Name, "The HttpParameter.Name should have been 'input'.");
                                }
                                else
                                {
                                    string expectedName = "input" + (j + 1).ToString();
                                    Assert.AreEqual(expectedName, parameter.Name, string.Format("The HttpParameter.Name should have been '{0}'.", expectedName));
                                }
                            }
                        }
                    }
                });
        }

        #endregion InputParameters

        #region OutputParameters

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OutputParameters returns the HttpParameters determined by reflecting over the generic HttpOperationHandlerOfT.")]
        public void OutputParametersReturnsReflectedHttpParameters()
        {
            List<Type> types = new List<Type>();

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (!types.Contains(type))
                    {
                        types.Add(type);

                        for (int i = 2; i <= 17; i++)
                        {
                            if (types.Count - i < 0)
                            {
                                break;
                            }

                            Type[] typeArray = types.Skip(types.Count - i).ToArray();
                            HttpOperationHandler genericHandler = GetGenericHandlerForTypes(typeArray);

                            for (int j = 0; j < genericHandler.OutputParameters.Count; j++)
                            {
                                HttpParameter parameter = genericHandler.OutputParameters[j];
                                Assert.AreEqual("output", parameter.Name, "The HttpParameter.Name should have been 'input'.");
                                Assert.AreEqual(typeArray.Last(), parameter.Type, "The HttpParameter.Type should have been the last type from the array.");
                            }
                        }
                    }
                });
        }

        #endregion OutputParameters

        #endregion Properties

        #region Methods

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric1()
        {
            SHttpOperationHandler01<int, int> handler = new SHttpOperationHandler01<int, int>("output");
            handler.OnHandleT = (in1) => in1;

            object[] output = handler.Handle(new object[]{ 1 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(1, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric02()
        {
            SHttpOperationHandler02<int, int, int> handler = new SHttpOperationHandler02<int, int, int>("output");
            handler.OnHandleT1T2 = (in1, in2) => in1 + in2;

            object[] output = handler.Handle(new object[] { 1, 2 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(3, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric03()
        {
            SHttpOperationHandler03<int, int, int, int> handler =
                new SHttpOperationHandler03<int, int, int, int>("output");
            handler.OnHandleT1T2T3 = (in1, in2, in3) => in1 + in2 + in3;

            object[] output = handler.Handle(new object[] { 1, 2, 3 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(6, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric04()
        {
            SHttpOperationHandler04<int, int, int, int, int> handler =
                new SHttpOperationHandler04<int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4 = (in1, in2, in3, in4) =>
                in1 + in2 + in3 + in4;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(10, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric05()
        {
            SHttpOperationHandler05<int, int, int, int, int, int> handler =
                new SHttpOperationHandler05<int, int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4T5 = (in1, in2, in3, in4, in5) =>
                in1 + in2 + in3 + in4 + in5;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4, 5 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(15, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric06()
        {
            SHttpOperationHandler06<int, int, int, int, int, int, int> handler =
                new SHttpOperationHandler06<int, int, int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4T5T6 = (in1, in2, in3, in4, in5, in6) =>
                in1 + in2 + in3 + in4 + in5 + in6;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4, 5, 6 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(21, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric07()
        {
            SHttpOperationHandler07<int, int, int, int, int, int, int, int> handler =
                new SHttpOperationHandler07<int, int, int, int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4T5T6T7 = (in1, in2, in3, in4, in5, in6, in7) =>
                in1 + in2 + in3 + in4 + in5 + in6 + in7;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4, 5, 6, 7 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(28, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric08()
        {
            SHttpOperationHandler08<int, int, int, int, int, int, int, int, int> handler =
                new SHttpOperationHandler08<int, int, int, int, int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4T5T6T7T8 = (in1, in2, in3, in4, in5, in6, in7, in8) =>
                in1 + in2 + in3 + in4 + in5 + in6 + in7 + in8;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(36, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric09()
        {
            SHttpOperationHandler09<int, int, int, int, int, int, int, int, int, int> handler =
                new SHttpOperationHandler09<int, int, int, int, int, int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4T5T6T7T8T9 = (in1, in2, in3, in4, in5, in6, in7, in8, in9) =>
                in1 + in2 + in3 + in4 + in5 + in6 + in7 + in8 + in9;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(45, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric10()
        {
            SHttpOperationHandler10<int, int, int, int, int, int, int, int, int, int, int> handler =
                new SHttpOperationHandler10<int, int, int, int, int, int, int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4T5T6T7T8T9T10 = (in1, in2, in3, in4, in5, in6, in7, in8, in9, in10) =>
                in1 + in2 + in3 + in4 + in5 + in6 + in7 + in8 + in9 + in10;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(55, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric11()
        {
            SHttpOperationHandler11<int, int, int, int, int, int, int, int, int, int, int, int> handler =
                new SHttpOperationHandler11<int, int, int, int, int, int, int, int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4T5T6T7T8T9T10T11 = (in1, in2, in3, in4, in5, in6, in7, in8, in9, in10, in11) =>
                in1 + in2 + in3 + in4 + in5 + in6 + in7 + in8 + in9 + in10 + in11;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(66, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric12()
        {
            SHttpOperationHandler12<int, int, int, int, int, int, int, int, int, int, int, int, int> handler =
                new SHttpOperationHandler12<int, int, int, int, int, int, int, int, int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4T5T6T7T8T9T10T11T12 = (in1, in2, in3, in4, in5, in6, in7, in8, in9, in10, in11, in12) =>
                in1 + in2 + in3 + in4 + in5 + in6 + in7 + in8 + in9 + in10 + in11 + in12;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(78, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric13()
        {
            SHttpOperationHandler13<int, int, int, int, int, int, int, int, int, int, int, int, int, int> handler =
                new SHttpOperationHandler13<int, int, int, int, int, int, int, int, int, int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4T5T6T7T8T9T10T11T12T13 = (in1, in2, in3, in4, in5, in6, in7, in8, in9, in10, in11, in12, in13) =>
                in1 + in2 + in3 + in4 + in5 + in6 + in7 + in8 + in9 + in10 + in11 + in12 + in13;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(91, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric14()
        {
            SHttpOperationHandler14<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int> handler =
                new SHttpOperationHandler14<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4T5T6T7T8T9T10T11T12T13T14 = (in1, in2, in3, in4, in5, in6, in7, in8, in9, in10, in11, in12, in13, in14) =>
                in1 + in2 + in3 + in4 + in5 + in6 + in7 + in8 + in9 + in10 + in11 + in12 + in13 + in14;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 });
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(105, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric15()
        {
            SHttpOperationHandler15<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int> handler =
                new SHttpOperationHandler15<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4T5T6T7T8T9T10T11T12T13T14T15 = (in1, in2, in3, in4, in5, in6, in7, in8, in9, in10, in11, in12, in13, in14, in15) =>
                in1 + in2 + in3 + in4 + in5 + in6 + in7 + in8 + in9 + in10 + in11 + in12 + in13 + in14 + in15;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15});
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(120, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Handle(object[]) calls OnHandle for the generic HttpOperationHandlerOfT.")]
        public void HandleCallsOnHandleOfGeneric16()
        {
            SHttpOperationHandler16<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int> handler = 
                new SHttpOperationHandler16<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>("output");
            handler.OnHandleT1T2T3T4T5T6T7T8T9T10T11T12T13T14T15T16 = (in1, in2, in3, in4, in5, in6, in7, in8, in9, in10, in11, in12, in13, in14, in15, in16) =>
                in1 + in2 + in3 + in4 + in5 + in6 + in7 + in8 + in9 + in10 + in11 + in12 + in13 + in14 + in15 + in16;

            object[] output = handler.Handle(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16});
            Assert.AreEqual(1, output.Length, "The Handle method should have returned an array of length 1.");
            Assert.AreEqual(136, (int)output[0], "The Handle method should have returned the sum of the input values.");
        }

        #endregion Methods

        #region Test Helpers

        public static HttpOperationHandler GetGenericHandlerForTypes(Type[] parameterTypes)
        {
            Type handlerType = null;
            switch (parameterTypes.Length) 
            {
                case 2: handlerType = typeof(SHttpOperationHandler01<,>); break;
                case 3: handlerType = typeof(SHttpOperationHandler02<,,>); break;
                case 4: handlerType = typeof(SHttpOperationHandler03<,,,>); break;
                case 5: handlerType = typeof(SHttpOperationHandler04<,,,,>); break;
                case 6: handlerType = typeof(SHttpOperationHandler05<,,,,,>); break;
                case 7: handlerType = typeof(SHttpOperationHandler06<,,,,,,>); break;
                case 8: handlerType = typeof(SHttpOperationHandler07<,,,,,,,>); break;
                case 9: handlerType = typeof(SHttpOperationHandler08<,,,,,,,,>); break;
                case 10: handlerType = typeof(SHttpOperationHandler09<,,,,,,,,,>); break;
                case 11: handlerType = typeof(SHttpOperationHandler10<,,,,,,,,,,>); break;
                case 12: handlerType = typeof(SHttpOperationHandler11<,,,,,,,,,,,>); break;
                case 13: handlerType = typeof(SHttpOperationHandler12<,,,,,,,,,,,,>); break;
                case 14: handlerType = typeof(SHttpOperationHandler13<,,,,,,,,,,,,,>); break;
                case 15: handlerType = typeof(SHttpOperationHandler14<,,,,,,,,,,,,,,>); break;
                case 16: handlerType = typeof(SHttpOperationHandler15<,,,,,,,,,,,,,,,>); break;
                case 17: handlerType = typeof(SHttpOperationHandler16<,,,,,,,,,,,,,,,,>); break;
                default:
                    Assert.Fail("Test Error: The type array can not be used to create a generic HttpOperationHandler");
                    break;
            }

            return GenericTypeAssert.InvokeConstructor<HttpOperationHandler>(handlerType, parameterTypes, new object[] { "output" });
        }

        #endregion Test Helpers
    }
}
