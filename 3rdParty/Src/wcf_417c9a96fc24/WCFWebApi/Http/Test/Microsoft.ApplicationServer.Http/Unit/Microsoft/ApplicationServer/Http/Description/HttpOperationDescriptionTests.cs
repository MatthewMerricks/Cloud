// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Mocks;
    using Microsoft.ApplicationServer.Common.Test.Services;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpOperationDescriptionTests
    {
        private const string TestNamespace = "http://tempuri.org/";

        #region Constructor Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription supports default ctor")]
        public void HttpOperationDescription_Default_Ctor()
        {
            HttpOperationDescription hod = new HttpOperationDescription();

            HttpParameter returnValue = hod.ReturnValue;
            Assert.IsNull(returnValue, "ReturnValue should initialize to null");

            Collection<Attribute> attributes = hod.Attributes;
            Assert.IsNotNull(attributes, "Attributes should initialize to non-null");
            Assert.AreEqual(0, attributes.Count, "Attributes should initialize to empty collection");

            IList<HttpParameter> inputs = hod.InputParameters;
            Assert.IsNotNull(inputs, "InputParameters should initialize to non-null");
            Assert.AreEqual(0, inputs.Count, "InputParameters should initialize to empty collection");

            IList<HttpParameter> outputs = hod.OutputParameters;
            Assert.IsNotNull(outputs, "OutputParameters should initialize to non-null");
            Assert.AreEqual(0, outputs.Count, "OutputParameters should initialize to empty collection");

            KeyedByTypeCollection<IOperationBehavior> behaviors = hod.Behaviors;
            Assert.IsNotNull(behaviors, "Behaviors should initialize to non-null");
            Assert.AreEqual(0, behaviors.Count, "Behaviors should initialize to empty");

            Collection<Type> knownTypes = hod.KnownTypes;
            Assert.IsNotNull(knownTypes, "knownTypes should initialize to non-null");
            Assert.AreEqual(0, knownTypes.Count, "knownTypes should initialize to empty");

            Assert.IsNull(hod.DeclaringContract, "DeclaringContract should initialize to null");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription is valid using empty OperationDescription")]
        public void HttpOperationDescription_From_Empty_OperationDescription_Supported()
        {
            OperationDescription od = new OperationDescription("name", new ContractDescription("name", "namespace"));
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");

            Assert.IsNull(hod.ReturnValue, "Return value should be null");

            Assert.IsNotNull(hod.InputParameters, "Input parameters should not be null");
            Assert.IsFalse(hod.InputParameters.Any(), "Input parameter list should be empty");

            Assert.IsNotNull(hod.OutputParameters, "Output parameters should not be null");
            Assert.IsFalse(hod.OutputParameters.Any(), "Output parameter list should be empty");

            Assert.IsNotNull(hod.Attributes, "Attributes should not be null");
            Assert.IsFalse(hod.Attributes.Any(), "Attributes list should be empty");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription can be created from simple types")]
        public void HttpOperationDescription_Can_Be_Created_From_Simple_Types()
        {
            HttpOperationDescription hod = new HttpOperationDescription();

            hod.ReturnValue = new HttpParameter("Return", typeof(string));
            hod.InputParameters.Add(new HttpParameter("InParameter", typeof(int)));
            hod.OutputParameters.Add(new HttpParameter("OutParameter", typeof(string))); 

            HttpParameter parmDesc = hod.ReturnValue;
            Assert.AreEqual("Return", parmDesc.Name, "ReturnValue.Name incorrect");
            Assert.AreEqual(typeof(string), parmDesc.Type, "ReturnValue.Type incorrect");

            IList<HttpParameter> coll = hod.InputParameters;
            Assert.AreEqual(1, coll.Count, "Input parameter collection should have 1 element");
            parmDesc = coll[0];
            Assert.AreEqual("InParameter", parmDesc.Name, "InputParameter.Name incorrect");
            Assert.AreEqual(typeof(int), parmDesc.Type, "InputParameter.Type incorrect");

            coll = hod.OutputParameters;
            Assert.AreEqual(1, coll.Count, "Output parameter collection should have 1 element");
            parmDesc = coll[0];
            Assert.AreEqual("OutParameter", parmDesc.Name, "OutParameter.Name incorrect");
            Assert.AreEqual(typeof(string), parmDesc.Type, "OutParameter.Type incorrect");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription properties are all mutable when default ctor is used")]
        public void HttpOperationDescription_All_Properties_Mutable()
        {
            HttpOperationDescription hod = new HttpOperationDescription();

            MethodInfo methodInfo = MethodInfo.GetCurrentMethod() as MethodInfo;

            ContractDescription cd = new ContractDescription("SampleContract");
            hod.DeclaringContract = cd;
            Assert.AreSame(cd, hod.DeclaringContract, "DeclaringContract was not settable");
            hod.DeclaringContract = null;
            Assert.IsNull(hod.DeclaringContract, "DeclaringContract was not resettable");

            Collection<Attribute> attributes = hod.Attributes;
            Attribute attr = new DescriptionAttribute("SampleAttr");
            attributes.Add(attr);
            Assert.AreEqual(1, hod.Attributes.Count, "Failed to add to Attributes");
            Assert.AreSame(attr, hod.Attributes[0], "Attribute added but not readable");

            Collection<Type> knownTypes = hod.KnownTypes;
            Type kt = this.GetType();
            knownTypes.Add(kt);
            Assert.AreEqual(1, hod.KnownTypes.Count, "Failed to add to KnownTypes");
            Assert.AreSame(kt, hod.KnownTypes[0], "KnownType added but not readable");

            KeyedByTypeCollection<IOperationBehavior> behaviors = hod.Behaviors;
            IOperationBehavior opBehavior = new MockOperationBehavior();
            behaviors.Add(opBehavior);
            Assert.AreEqual(1, hod.Behaviors.Count, "Failed to add to Behaviors");
            Assert.AreSame(opBehavior, hod.Behaviors[0], "Behaviors added but not readable");
        }

        #endregion Constructor Tests

        #region Extension Method Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription extension method returns an HttpOperationDescription from a normal OperationDescription")]
        public void HttpOperationDescription_Extension_Method_Returns_HttpOperationDescription()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");
            this.AssertValidHttpOperationDescription(hod, od);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription extension method throws ArgumentNull for null this")]
        public void HttpOperationDescription_Extension_Method_Null_Throws()
        {
            OperationDescription od = null;
            ExceptionAssert.ThrowsArgumentNull("operation", () => od.ToHttpOperationDescription());
        }

        #endregion Extension Method Tests

        #region HttpOperation unsynchronized tests
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription unsynchronized ReturnValue can be set to any value")]
        public void HttpOperationDescription_Unsynchronized_ReturnValue_Setter()
        {
            HttpOperationDescription hod = new HttpOperationDescription();

            HttpParameter returnValue = new HttpParameter("TheReturn", typeof(string));
            hod.ReturnValue = returnValue;

            Assert.AreSame(returnValue, hod.ReturnValue, "Failed to set return value");

            hod.ReturnValue = null;
            Assert.IsNull(hod.ReturnValue, "Failed to reset return value");
        }

        #endregion HttpOperation unsynchronized tests

        #region HttpOperationDescription from OperationDescription Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription returns attributes from operation method")]
        public void HttpOperationDescription_Returns_Attributes()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "ZeroInputsAndReturnsVoid");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");
            IEnumerable<Attribute> attributes = hod.Attributes;
            Assert.IsNotNull(attributes, "Attributes were null");
            Assert.IsTrue(attributes.OfType<DescriptionAttribute>().Any(), "Did not discover [Description] attribute");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription return parameter matches method")]
        public void HttpOperationDescription_Return_Parameter_Matches_Method()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");
            HttpParameter returnParameter = hod.ReturnValue;
            Assert.IsNotNull(returnParameter, "Return parameter was null");
            Assert.AreEqual(typeof(string), returnParameter.Type, "Return parameter type should have been string");

            Assert.AreEqual("OneInputAndReturnValueResult", returnParameter.Name, "Return parameter name match operation + Result");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription return parameter matches a void method")]
        public void HttpOperationDescription_Return_Parameter_Matches_Void_Method()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "ZeroInputsAndReturnsVoid");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");
            HttpParameter returnParameter = hod.ReturnValue;
            Assert.IsNotNull(returnParameter, "Return parameter was null");
            Assert.AreEqual(typeof(void), returnParameter.Type, "Return parameter type should have been void");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription InputParameters matches method")]
        public void HttpOperationDescription_InputParameter_Matches_Method()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");
            IList<HttpParameter> parameters = hod.InputParameters;
            Assert.IsNotNull(parameters, "InputParameters should not be null");
            Assert.AreEqual(1, parameters.Count, "Expected only one parameter");
            Assert.AreEqual(typeof(int), parameters[0].Type, "Expected int parameter");
            Assert.AreEqual("parameter1", parameters[0].Name, "Expected parameter name match");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription multiple InputParameters matches method and preserves order")]
        public void HttpOperationDescription_Multiple_InputParameter_Matches_Method()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "ThreeInputsAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");
            IList<HttpParameter> parameters = hod.InputParameters;
            Assert.IsNotNull(parameters, "InputParameters should not be null");
            Assert.AreEqual(3, parameters.Count, "Expected multiple parameters");

            Assert.AreEqual(typeof(int), parameters[0].Type, "Expected int parameter1");
            Assert.AreEqual("parameter1", parameters[0].Name, "Expected parameter1 name match");

            Assert.AreEqual(typeof(double), parameters[1].Type, "Expected double parameter2");
            Assert.AreEqual("parameter2", parameters[1].Name, "Expected parameter2 name match");

            Assert.AreEqual(typeof(string), parameters[2].Type, "Expected string parameter3");
            Assert.AreEqual("parameter3", parameters[2].Name, "Expected parameter3 name match");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription OutputParameter matches method")]
        public void HttpOperationDescription_OutputParameter_Matches_Method()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputOneOutputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");
            IList<HttpParameter> parameters = hod.OutputParameters;
            Assert.IsNotNull(parameters, "OutputParameters should not be null");
            Assert.AreEqual(1, parameters.Count, "Expected only one parameter");
            Assert.AreEqual(typeof(double), parameters[0].Type, "Expected out parameter of type out double");
            Assert.AreEqual("parameter2", parameters[0].Name, "Expected parameter name match");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription multiple OutputParameters matches method and preserves order")]
        public void HttpOperationDescription_Multiple_OutputParameter_Matches_Method()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputTwoOutputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");
            IList<HttpParameter> inputParameters = hod.InputParameters;
            Assert.IsNotNull(inputParameters, "InputParameters should not be null");
            Assert.AreEqual(1, inputParameters.Count, "Expected single input parameters");

            IList<HttpParameter> outputParameters = hod.OutputParameters;
            Assert.IsNotNull(outputParameters, "OutputParameters should not be null");
            Assert.AreEqual(2, outputParameters.Count, "Expected 2 output parameters");

            Assert.AreEqual(typeof(int), inputParameters[0].Type, "Expected int parameter1");
            Assert.AreEqual("parameter1", inputParameters[0].Name, "Expected parameter1 name match");

            Assert.AreEqual( typeof(double), outputParameters[0].Type, "Parameter2 incorrect type");
            Assert.AreEqual("parameter2", outputParameters[0].Name, "Expected parameter2 name match");

            Assert.AreEqual(typeof(char), outputParameters[1].Type, "Parameter3 incorrect type");
            Assert.AreEqual("parameter3", outputParameters[1].Name, "Expected parameter3 name match");
        }

        #endregion HttpOperationDescription from OperationDescription Tests

        #region Update OperationDescription updates HttpOperationDescription Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription updating return value through OperationDescription updates in HttpOperationDescription")]
        public void HttpOperationDescription_Update_ReturnValue_From_OperationDescription()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();

            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");
            HttpParameter returnParameter = hod.ReturnValue;
            Assert.IsNotNull(returnParameter, "Return parameter was null");
            Assert.AreEqual(typeof(string), returnParameter.Type, "Return parameter type should have been string");

            // Update return MPD in-place
            MessagePartDescription mpd = od.Messages[1].Body.ReturnValue;
            mpd.Type = typeof(float);
            Assert.AreEqual(typeof(float), hod.ReturnValue.Type, "Updating OD ReturnValue in place should reflect in HOD");

            // Insert a new MPD
            mpd = new MessagePartDescription("NewName", "NewNamespace");
            mpd.Type = typeof(double);
            od.Messages[1].Body.ReturnValue = mpd;
            Assert.AreEqual(typeof(double), hod.ReturnValue.Type, "Inserting new OD ReturnValue  should reflect in HOD");

            // Remove the MPD
            od.Messages.RemoveAt(1);
            Assert.IsNull(hod.ReturnValue, "Removing return value message part should yield null ReturnValue");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription updating input parameters through OperationDescription updates in HttpOperationDescription")]
        public void HttpOperationDescription_Update_InputParameters_From_OperationDescription()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            OperationDescription od2 = GetOperationDescription(typeof(SimpleOperationsService), "OneInputOneOutputAndReturnValue");

            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");

            // Just confirm initial creation was correct
            IList<HttpParameter> inputParameters = hod.InputParameters;
            Assert.AreEqual(1, inputParameters.Count, "Expected one input parameter");
            Assert.AreEqual(typeof(int), inputParameters[0].Type, "Expected int type");

            // Replace the messages from 2nd OD
            od.Messages[0] = od2.Messages[0];

            inputParameters = hod.InputParameters;
            Assert.IsNotNull(inputParameters, "InputParameters should not be null after update");
            Assert.AreEqual(1, inputParameters.Count, "Expected one input parameter after update");
            Assert.AreEqual(typeof(char), inputParameters[0].Type, "Expected char type after update");
        }

        #endregion Update OperationDescription updates HttpOperationDescription Tests

        #region Update HttpOperationDescription updates OperationDescription Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription throws when updating return value through unsynchronized HttpParameter")]
        public void HttpOperationDescription_Update_ReturnValue_Throws_From_Unsynchronized()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescription mpd = od.Messages[1].Body.ReturnValue;

            // Get a synchronized HOD.
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");

            // Get a synchronized HPD
            HttpParameter hpd = hod.ReturnValue;
            Assert.IsNotNull(hpd, "Return parameter was null");
            Assert.AreEqual(typeof(string), hpd.Type, "Return parameter type should have been string");

            // Null set should succeed
            hod.ReturnValue = null;
            Assert.IsNull(hod.ReturnValue, "Could not set to null");

            // Null should propagate
            Assert.IsNull(od.Messages[1].Body.ReturnValue, "Null did not propagate");

            // Set back to valid value and see if propagates
            hod.ReturnValue = hpd;
            Assert.AreEqual(mpd, od.Messages[1].Body.ReturnValue, "Did not reset to synced value");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription updating return value through HttpOperationDescription with incomplete MesageDescription updates in OperationDescription")]
        public void HttpOperationDescription_Update_ReturnValue_From_Incomplete_HttpOperationDescription()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescription mpd = od.Messages[1].Body.ReturnValue;
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            HttpParameter hpd = hod.ReturnValue;

            // Clear out all of Messages[]
            od.Messages.Clear();

            Assert.IsNull(hod.ReturnValue, "ReturnValue should be null with no backing Messages[1]");

            // Setting a valid ReturnValue should auto-create Messages[1]
            hod.ReturnValue = hpd;

            Assert.IsNotNull(hod.ReturnValue, "ReturnValue was not set");
            Assert.AreSame(hpd.MessagePartDescription, hod.ReturnValue.MessagePartDescription, "ReturnValue not as expected");

            Assert.AreEqual(2, od.Messages.Count, "Setting ReturnValue should have created Messages[1]");
         }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription delete input parameters through HttpOperationDescription updates in OperationDescription")]
        public void HttpOperationDescription_Delete_InputParameters_From_HttpOperationDescription()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescription mpd = od.Messages[0].Body.Parts[0];  // input parameter 1 in the MPD

            // Get a synchronized HOD.
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");

            // Get a synchronized HPDCollection and from it a synchronized HPD
            IList<HttpParameter> hpdColl = hod.InputParameters;
            Assert.AreEqual(1, hpdColl.Count, "Expected HPD collection to have one input param");

            // Delete it from our collection
            hpdColl.RemoveAt(0);

            // Verify the backing OD has seen that change
            Assert.AreEqual(0, od.Messages[0].Body.Parts.Count, "Expected delete of HPD input parameter to delete it from OD");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription updating input parameters through incomplete HttpOperationDescription updates in OperationDescription")]
        public void HttpOperationDescription_Update_InputParameters_From_Incomplete_HttpOperationDescription()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescription mpd = od.Messages[0].Body.Parts[0];  // input parameter 1 in the MPD
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            IList<HttpParameter> hpdColl = hod.InputParameters;
            HttpParameter hpd = hpdColl[0];

            // Zap the Messages[]
            od.Messages.Clear();

            // Zapping the backing Messages should have rendered the input collection empty
            Assert.AreEqual(0, hpdColl.Count, "Resetting Messages did not reset count");
            Assert.AreEqual(0, hod.InputParameters.Count, "Resetting Messages did not reset count in HOD");

            // Mutating the InputParameter collection should auto-create the Messages
            hpdColl.Add(hpd);

            Assert.AreEqual(1, od.Messages.Count, "Messages[0] was not autocreated");
            Assert.AreEqual(1, hpdColl.Count, "Creating Messages did not set count");
            Assert.AreEqual(1, hod.InputParameters.Count, "Creating Messages did not set count in HOD");
         }


        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription inserting input parameters through HttpOperationDescription updates in OperationDescription")]
        public void HttpOperationDescription_Insert_InputParameters_From_HttpOperationDescription()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescriptionCollection mpdColl = od.Messages[0].Body.Parts;
            Assert.AreEqual(1, mpdColl.Count, "Test assumes we start with 1 input param");

            // Get a synchronized HOD and HODCollection
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            IList<HttpParameter> hpdColl = hod.InputParameters;

            // Get an HPD for the newly created MPD
            MessagePartDescription mpdNew = new MessagePartDescription("NewMPD", "NewMPDNS") { Type = typeof(double) };
            HttpParameter hpd = mpdNew.ToHttpParameter();

            // Add it to the input parameters
            hpdColl.Add(hpd);

            // Verify it appears in the MPD coll
            Assert.AreEqual(2, mpdColl.Count, "Adding new MPD to HPD collection should have updated MPD collection");
            Assert.AreEqual(2, od.Messages[0].Body.Parts.Count, "Adding new MPD should have updated Parts");
            Assert.AreEqual(typeof(double), od.Messages[0].Body.Parts[1].Type, "Adding new MPD failed due to type");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription delete output parameters through HttpOperationDescription updates in OperationDescription")]
        public void HttpOperationDescription_Delete_OutputParameters_From_HttpOperationDescription()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputOneOutputAndReturnValue");
            MessagePartDescription mpd = od.Messages[1].Body.Parts[0];  // input parameter 1 in the MPD

            // Get a synchronized HOD.
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            Assert.IsNotNull(hod, "Failed to create HttpOperationDescription");

            // Get a synchronized HPDCollection and from it a synchronized HPD
            IList<HttpParameter> hpdColl = hod.OutputParameters;
            Assert.AreEqual(1, hpdColl.Count, "Expected HPD collection to have one output param");

            // Delete it from our collection
            hpdColl.RemoveAt(0);

            // Verify the backing OD has seen that change
            Assert.AreEqual(0, od.Messages[1].Body.Parts.Count, "Expected delete of HPD input parameter to delete it from OD");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription updating output parameters through incomplete HttpOperationDescription updates in OperationDescription")]
        public void HttpOperationDescription_Update_OutputParameters_From_Incomplete_HttpOperationDescription()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputOneOutputAndReturnValue");
            MessagePartDescription mpd = od.Messages[1].Body.Parts[0];  // output parameter 1 in the MPD
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            IList<HttpParameter> hpdColl = hod.OutputParameters;
            HttpParameter hpd = hpdColl[0];

            // Zap the Messages[]
            od.Messages.Clear();

            // Zapping the backing Messages should have rendered the input collection empty
            Assert.AreEqual(0, hpdColl.Count, "Resetting Messages did not reset count");
            Assert.AreEqual(0, hod.InputParameters.Count, "Resetting Messages did not reset count in HOD");

            // Mutating the OutputParameter collection should auto-create the both in and out Messages
            hpdColl.Add(hpd);

            Assert.AreEqual(2, od.Messages.Count, "Messages[1] was not autocreated");
            Assert.AreEqual(1, hpdColl.Count, "Creating Messages did not set count");
            Assert.AreEqual(1, hod.OutputParameters.Count, "Creating Messages did not set count in HOD");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription inserting output parameters through HttpOperationDescription updates in OperationDescription")]
        public void HttpOperationDescription_Insert_OutputParameters_From_HttpOperationDescription()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputOneOutputAndReturnValue");
            MessagePartDescriptionCollection mpdColl = od.Messages[1].Body.Parts;
            Assert.AreEqual(1, mpdColl.Count, "Test assumes we start with 1 output param");

            // Get a synchronized HOD and HODCollection
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            IList<HttpParameter> hpdColl = hod.OutputParameters;

            // Get an HPD for the newly created MPD
            MessagePartDescription mpdNew = new MessagePartDescription("NewMPD", "NewMPDNS") { Type = typeof(byte) };
            HttpParameter hpd = mpdNew.ToHttpParameter();

            // Add it to the output parameters
            hpdColl.Add(hpd);

            // Verify it appears in the MPD coll
            Assert.AreEqual(2, mpdColl.Count, "Adding new MPD to HPD collection should have updated MPD collection");
            Assert.AreEqual(2, od.Messages[1].Body.Parts.Count, "Adding new MPD should have updated Parts");
            Assert.AreEqual(typeof(byte), od.Messages[1].Body.Parts[1].Type, "Adding new MPD failed due to type");
        }

        #endregion Update HttpOperationDescription updates OperationDescription Tests

        #region Data Driven Validation

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription data driven test validates multiple parameter signatures")]
        public void HttpOperationDescription_Data_Driven_Parameter_Signatures()
        {
            // Data-driven test.
            // Tests all OperationDescriptions in all the contract types listed.
            // Each is tested for:
            //  - HttpOperationDescription properties all match original OperationDescription properties
            //  - Input/Output/Return parameters all agree with Reflection on MethodInfos
            this.AssertAllHttpOperationsForEach(typeof(SimpleOperationsService));
        }

        #endregion Data Driven Validation

        #region Test Helpers

        private void AssertAllHttpOperationsForEach(params Type[] contractTypes)
        {
            foreach (Type contractType in contractTypes)
            {
                this.AssertAllHttpOperationDescriptions(contractType);
            }
        }
        private void AssertAllHttpOperationDescriptions(Type contractType)
        {
            ContractDescription cd = ContractDescription.GetContract(contractType);
            MethodInfo[] methods = contractType.GetMethods().Where(m => m.GetCustomAttributes(typeof(OperationContractAttribute), false).Any()).ToArray();
            Assert.AreEqual(methods.Length, cd.Operations.Count, "Number of operations did not match our MethodInfo count");
            for (int i = 0; i < methods.Length; ++i)
            {
                OperationDescription od = cd.Operations[i];
                HttpOperationDescription hod = this.AssertValidHttpOperationDescription(od);
                this.AssertHttpOperationDescriptionMatchesMethod(hod, methods[i]);
            }
        }

        // Asserts that an HttpOperationDescription created from the given OperationDescription
        // matches all public properties they have in common.   Also returns that HttpOperationDescription.
        private HttpOperationDescription AssertValidHttpOperationDescription(OperationDescription od)
        {
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            this.AssertValidHttpOperationDescription(hod, od);
            return hod;
        }

        // Asserts that the given HttpOperationDescription matches the given OperationDescription
        // in terms of all parameters, behaviors, faults, etc
        private void AssertValidHttpOperationDescription(HttpOperationDescription hod, OperationDescription od)
        {
            Assert.IsNotNull(hod, "Could not create HttpOperationDescription from " + od.GetType());

            Assert.AreEqual(od.Name, hod.Name, "Name mismatch");
        }

        // Verifies that a HttpOperationDescription built from an OperationDescription
        // matches the given MethodInfo for return type, parameters, and attributes
        private void AssertHttpOperationDescriptionMatchesMethod(HttpOperationDescription hod, MethodInfo method)
        {
            Assert.IsNotNull(hod, "HttpOperationDescription was null");
            Assert.AreEqual(hod.Name, method.Name, "Name mismatch");

            HttpParameter returnParameter = hod.ReturnValue;
            if (!hod.ToOperationDescription().IsOneWay)
            {
                Assert.AreEqual(returnParameter.Type, method.ReturnType, "Return type mismatch");
            }

            IList<HttpParameter> inputParameters = hod.InputParameters;
            ParameterInfo[] parameters = method.GetParameters().Where(p => !p.IsOut).ToArray();
            Assert.AreEqual(parameters.Length, inputParameters.Count, "Input parameter count mismatch");

            for (int i = 0; i < parameters.Length; ++i)
            {
                Assert.AreEqual(parameters[i].Name, inputParameters[i].Name, "Input parameter name mismatch");
                Assert.AreEqual(parameters[i].ParameterType, inputParameters[i].Type, "Input parameter type mismatch");
            }

            IList<HttpParameter> outputParameters = hod.OutputParameters;
            parameters = method.GetParameters().Where(p => p.IsOut).ToArray();
            Assert.AreEqual(parameters.Length, outputParameters.Count, "Output parameter count mismatch");

            for (int i = 0; i < parameters.Length; ++i)
            {
                Assert.AreEqual(parameters[i].Name, outputParameters[i].Name, "Output parameter name mismatch");

                // ServiceModel removes the ByRef part
                Type t = parameters[i].ParameterType;
                if (t.HasElementType && t.IsByRef)
                {
                    t = t.GetElementType();
                }
                Assert.AreEqual(t, outputParameters[i].Type, "Output parameter type mismatch");
            }

            IEnumerable<Attribute> hodAttributes = hod.Attributes;
            IEnumerable<Attribute> methodAttributes = method.GetCustomAttributes(false).Cast<Attribute>().ToArray();

            foreach (Attribute a in methodAttributes)
            {
                if (!hodAttributes.Contains(a))
                {
                    Assert.Fail("Did not find attribute " + a.GetType().Name + " on method " + method.Name);
                }
            }
        }

        internal static void AssertEqualEnumerables<T>(IEnumerable<T> e1, IEnumerable<T> e2, string message)
        {
            Assert.AreEqual(e1.Count(), e2.Count(), "Lists are different size");
            foreach (T t in e1)
            {
                if (!e2.Contains(t))
                {
                    Assert.Fail("For " + message + ", item " + t + " is not in the 2nd list");
                }
            }
        }

        public static OperationDescription GetOperationDescription(Type contractType, string methodName)
        {
            ContractDescription cd = ContractDescription.GetContract(contractType);
            OperationDescription od = cd.Operations.FirstOrDefault(o => o.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
            Assert.IsNotNull(od, "Failed to get operation description for " + methodName);
            return od;
        }

        #endregion Test Helpers
    }
}
