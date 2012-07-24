namespace Microsoft.ServiceModel.Web.Test
{
    using System;
    using System.Json;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using global::Microsoft.VisualStudio.TestTools.UnitTesting;

    [ServiceContract]
    public interface IGoodContractWebInvokeInput
    {
        [OperationContract, WebInvoke]
        void Operation(JsonValue input);
    }

    [ServiceContract]
    public interface IGoodContractWebInvokeOutput
    {
        [OperationContract, WebInvoke]
        JsonValue Operation();
    }

    [ServiceContract]
    public interface IGoodContractDefaultWebInvokeInput
    {
        [OperationContract]
        void Operation(JsonValue input);
    }

    [ServiceContract]
    public interface IGoodContractDefaultWebInvokeOutput
    {
        [OperationContract]
        JsonValue Operation();
    }

    [ServiceContract]
    public interface IGoodContractWebGetOutput
    {
        [OperationContract, WebGet]
        JsonValue Operation();
    }

    [ServiceContract]
    public interface IRefJsonValue
    {
        [OperationContract, WebInvoke]
        void Operation(ref JsonValue value);
    }

    [ServiceContract]
    public interface IOutJsonValue
    {
        [OperationContract, WebInvoke]
        void Operation(JsonValue input, out JsonValue output);
    }

    [TestClass]
    public class WebHttpBehavior3NegativeTests
    {
        [TestMethod]
        public void TestJsonValueWithDefaultBodyStyleWrapped()
        {
            Action<WebHttpBehavior3> modification = delegate(WebHttpBehavior3 behavior)
            {
                behavior.DefaultBodyStyle = WebMessageBodyStyle.Wrapped;
            };

            this.TestValidationErrorForGoodContracts<InvalidOperationException>(modification, true, true);
        }

        [TestMethod]
        public void TestJsonValueWithDefaultBodyStyleWrappedRequest()
        {
            Action<WebHttpBehavior3> modification = delegate(WebHttpBehavior3 behavior)
            {
                behavior.DefaultBodyStyle = WebMessageBodyStyle.Wrapped;
            };

            this.TestValidationErrorForGoodContracts<InvalidOperationException>(modification, true, false);
        }

        [TestMethod]
        public void TestJsonValueWithDefaultBodyStyleWrappedResponse()
        {
            Action<WebHttpBehavior3> modification = delegate(WebHttpBehavior3 behavior)
            {
                behavior.DefaultBodyStyle = WebMessageBodyStyle.WrappedResponse;
            };

            this.TestValidationErrorForGoodContracts<InvalidOperationException>(modification, false, true);
        }

        [TestMethod]
        public void TestRefJsonValue()
        {
            this.TestValidationError<NegativeTestService, IRefJsonValue, InvalidOperationException>(null);
        }

        [TestMethod]
        public void TestOutJsonValue()
        {
            this.TestValidationError<NegativeTestService, IOutJsonValue, InvalidOperationException>(null);
        }

        void TestValidationErrorForGoodContracts<TExpectedException>(Action<WebHttpBehavior3> behaviorChanges, bool testingRequest, bool testingResponse) where TExpectedException : Exception
        {
            if (testingRequest)
            {
                this.TestValidationError<NegativeTestService, IGoodContractDefaultWebInvokeInput, InvalidOperationException>(behaviorChanges);
                this.TestValidationError<NegativeTestService, IGoodContractWebInvokeInput, InvalidOperationException>(behaviorChanges);
            }

            if (testingResponse)
            {
                this.TestValidationError<NegativeTestService, IGoodContractDefaultWebInvokeOutput, InvalidOperationException>(behaviorChanges);
                this.TestValidationError<NegativeTestService, IGoodContractWebInvokeOutput, InvalidOperationException>(behaviorChanges);
                this.TestValidationError<NegativeTestService, IGoodContractWebGetOutput, InvalidOperationException>(behaviorChanges);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Reliability", "CA2000",
            Justification = "The host cannot be disposed, since it calls Close, and the host is Faulted (so Close will throw)")]
        void TestValidationError<TService, TInterface, TExpectedException>(Action<WebHttpBehavior3> behaviorChanges) where TExpectedException : Exception
        {
            string baseAddress = "http://localhost:8000/Service";
            ServiceHost host = new ServiceHost(typeof(TService), new Uri(baseAddress));
            WebHttpBehavior3 behavior = new WebHttpBehavior3();
            if (behaviorChanges != null)
            {
                behaviorChanges(behavior);
            }

            host.AddServiceEndpoint(typeof(TInterface), new WebHttpBinding(), "").Behaviors.Add(behavior);
            try
            {
                host.Open();
                Assert.Fail("Error, expected exception {0}, got none", typeof(TExpectedException).FullName);
            }
            catch (TExpectedException e)
            {
                Console.WriteLine("For interface {0}, caught: {1}", typeof(TInterface).Name, e);
            }
            finally
            {
                host.Abort();
            }
        }

        public sealed class NegativeTestService :
            IGoodContractWebInvokeInput,
            IGoodContractWebInvokeOutput,
            IGoodContractDefaultWebInvokeInput,
            IGoodContractDefaultWebInvokeOutput,
            IGoodContractWebGetOutput,
            IRefJsonValue,
            IOutJsonValue
        {
            void IGoodContractWebInvokeInput.Operation(JsonValue input)
            {
                throw new NotSupportedException("This should never be reached");
            }

            JsonValue IGoodContractWebInvokeOutput.Operation()
            {
                throw new NotSupportedException("This should never be reached");
            }

            void IGoodContractDefaultWebInvokeInput.Operation(JsonValue input)
            {
                throw new NotSupportedException("This should never be reached");
            }

            JsonValue IGoodContractDefaultWebInvokeOutput.Operation()
            {
                throw new NotSupportedException("This should never be reached");
            }

            JsonValue IGoodContractWebGetOutput.Operation()
            {
                throw new NotSupportedException("This should never be reached");
            }

            void IRefJsonValue.Operation(ref JsonValue value)
            {
                throw new NotSupportedException("This should never be reached");
            }

            void IOutJsonValue.Operation(JsonValue input, out JsonValue output)
            {
                throw new NotSupportedException("This should never be reached");
            }
        }
    }
}
