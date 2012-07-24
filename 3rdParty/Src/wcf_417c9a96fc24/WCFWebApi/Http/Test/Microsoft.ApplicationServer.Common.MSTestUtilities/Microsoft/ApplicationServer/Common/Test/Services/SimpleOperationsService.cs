// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System;
    using System.ServiceModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// A service with operations that have different 
    /// combinations of inputs, outputs and return values.
    /// </summary>
    [ServiceContract]
    public class SimpleOperationsService
    {
        [OperationContract]
        [Description("An operation that takes zero input parameter and returns void.")]
        public void ZeroInputsAndReturnsVoid()
        {
        }

        [OperationContract]
        [Description("An operation that takes zero input parameter and has a return value.")]
        public double ZeroInputsAndReturnValue()
        {
            return 0.5;
        }

        [OperationContract]
        [Description("An operation that takes one input parameter and has a return value.")]
        public string OneInputAndReturnValue(int parameter1) 
        { 
            return parameter1.ToString(); 
        }

        [OperationContract]
        [Description("An operation that takes three input parameters and has a return value.")]
        public string ThreeInputsAndReturnValue(int parameter1, double parameter2, string parameter3)
        { 
            return parameter1.ToString() + parameter2.ToString() + parameter3; 
        }

        [OperationContract]
        [Description("An operation that takes one input parameter, has one out parameter and returns void.")]
        public void OneInputOneOutputAndReturnsVoid(int parameter1, out double parameter2)
        {
            parameter2 = parameter1;
        }

        [OperationContract]
        [Description("An operation that takes one input parameter, has one out parameter and a return value.")]
        public string OneInputOneOutputAndReturnValue(char parameter1, out double parameter2) 
        { 
            parameter2 = 0.0; 
            return null; 
        }

        [OperationContract]
        [Description("An operation that takes one input parameter, has two out parameters and a return value.")]
        public string OneInputTwoOutputAndReturnValue(int parameter1, out double parameter2, out char parameter3) 
        { 
            parameter2 = parameter1; 
            parameter3 = 'c';  
            return null; 
        }

        [OperationContract]
        [Description("An operation that takes two input parameters, has one out parameter and a return value.")]
        public string TwoInputOneOutputAndReturnValue(int parameter1a, char parameter2a, out double parameter3a)
        {
            parameter3a = 0.0;
            return null;
        }

        [OperationContract(IsOneWay = true)]
        [Description("An operation that takes one input parameter and is one way.")]
        public void OneInputOneWay(int parameter1)
        {
        }
    }
}
