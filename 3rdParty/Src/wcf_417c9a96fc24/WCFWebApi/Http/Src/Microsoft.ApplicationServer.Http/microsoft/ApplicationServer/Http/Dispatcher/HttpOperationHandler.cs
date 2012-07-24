// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.ServiceModel;
    using System.Text;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Description;

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances
    /// that execute based on declared input and output <see cref="HttpParameter">HttpParameters</see>. 
    /// </summary>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler
    {
        internal static readonly Type HttpOperationHandlerType = typeof(HttpOperationHandler);

        private const char OpenBracket = '[';
        private const char CloseBracket = ']';
        private const string OutParameterIdentifier = "out ";
        private const string ParameterSeperator = ", ";
        private const string OnGetInputParametersMethodName = "OnGetInputParameters";
        private const string OnGetOutputParametersMethodName = "OnGetOutputParameters";

        private static readonly IEnumerable<HttpParameter> emptyHttpParameters = Enumerable.Empty<HttpParameter>();
 
        private ReadOnlyCollection<HttpParameter> inArguments;
        private ReadOnlyCollection<HttpParameter> outArguments;
        private string toString;
        private string operationName;
        private bool staticOperationNameIsSet;

        /// <summary>
        /// Gets the collection of input <see cref="HttpParameter">HttpParameters</see> 
        /// that the <see cref="HttpOperationHandler"/> expects to handle.
        /// </summary>
        public ReadOnlyCollection<HttpParameter> InputParameters
        {
            get
            {
                if (this.inArguments == null)
                {
                    this.inArguments = this.GetValidatedHttpParameters(this.OnGetInputParameters(), OnGetInputParametersMethodName);
                }

                return this.inArguments;
            }
        }

        /// <summary>
        /// Gets the collection of output <see cref="HttpParameter">HttpParameters</see> 
        /// that the <see cref="HttpOperationHandler"/> returns.
        /// </summary>
        public ReadOnlyCollection<HttpParameter> OutputParameters
        {
            get
            {
                if (this.outArguments == null)
                {
                    this.outArguments = this.GetValidatedHttpParameters(this.OnGetOutputParameters(), OnGetOutputParametersMethodName);
                }

                return this.outArguments;
            }
        }

        internal string OperationName
        {
            get
            {
                if (this.staticOperationNameIsSet)
                {
                    return this.operationName;
                }

                return OperationContext.Current.GetCurrentOperationName();
            }

            set
            {
                if (!this.staticOperationNameIsSet && this.operationName == null)
                {
                    this.operationName = value;
                    this.staticOperationNameIsSet = true;
                }
                else if (!string.Equals(value, this.operationName, StringComparison.Ordinal))
                {
                    // This handler has been applied to more than one operation so we
                    //  no longer can identify the particular operation that it is executing for
                    this.operationName = SR.UnknownName;
                    this.staticOperationNameIsSet = false;
                }
            }
        }

        /// <summary>
        /// The <see cref="HttpOperationHandler"/> handles the given <paramref name="input"/> values and returns 
        /// a the output <see cref="HttpParameter"/> instances.
        /// </summary>
        /// <param name="input">
        /// The input values that the <see cref="HttpOperationHandler"/> should handle. 
        /// The values should agree in order and type with the input <see cref="HttpParameter">
        /// HttpParameters</see> given by the <see cref="HttpOperationHandler.InputParameters"/> property.
        /// </param>
        /// <returns>
        /// An array that provides the output values.
        /// </returns>
        public object[] Handle(object[] input)
        {
            if (input == null)
            {
                throw Fx.Exception.ArgumentNull("input");
            }

            input = this.ValidateAndConvertInput(input);
            object[] output = this.OnHandle(input);
            return this.ValidateOutput(output);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance and includes
        /// the type name of the <see cref="HttpOperationHandler"/> and a list of the
        /// input and output <see cref="HttpParameter"/> names.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (this.toString == null)
            {
                StringBuilder stringBuilder = new StringBuilder(this.GetType().Name);
                bool openBracketWritten = false;

                foreach (HttpParameter parameter in this.InputParameters)
                {
                    if (!openBracketWritten)
                    {
                        stringBuilder.Append(OpenBracket);
                        openBracketWritten = true;
                    }
                    else
                    {
                        stringBuilder.Append(ParameterSeperator);
                    }

                    stringBuilder.Append(parameter.Name);
                }
                
                foreach (HttpParameter parameter in this.OutputParameters)
                {
                    if (!openBracketWritten)
                    {
                        stringBuilder.Append(OpenBracket);
                        openBracketWritten = true;
                    }
                    else
                    {
                        stringBuilder.Append(ParameterSeperator);
                    }

                    stringBuilder.Append(OutParameterIdentifier);
                    stringBuilder.Append(parameter.Name);
                }

                if (openBracketWritten)
                {
                    stringBuilder.Append(CloseBracket);
                }

                this.toString = stringBuilder.ToString();
            }

            return this.toString;
        }

        /// <summary>
        /// Implemented in a derived class to return the input <see cref="HttpParameter">HttpParameters</see>
        /// that the <see cref="HttpOperationHandler"/> expects to be provided whenever the <see cref="HttpOperationHandler.Handle"/> method 
        /// is called.  The <see cref="HttpParameter">HttpParameters</see> must be returned in the same order
        /// the the <see cref="HttpOperationHandler.Handle"/> method will expect them in the input object array.
        /// </summary>
        /// <remarks>
        /// <see cref="OnGetInputParameters"/> is only called once and the <see cref="HttpParameter">HttpParameters</see>
        /// are cached in a read-only collection.
        /// </remarks>
        /// <returns>
        /// The input <see cref="HttpParameter">HttpParameters</see> that the <see cref="HttpOperationHandler"/>
        /// expects.
        /// </returns>
        protected abstract IEnumerable<HttpParameter> OnGetInputParameters();

        /// <summary>
        /// Implemented in a derived class to return the ouput <see cref="HttpParameter">HttpParameters</see>
        /// that the <see cref="HttpOperationHandler"/> will provided whenever the <see cref="HttpOperationHandler.Handle"/> method 
        /// is called.  The <see cref="HttpParameter">HttpParameters</see> must be returned in the same order
        /// the the <see cref="HttpOperationHandler.Handle"/> method will provide then in the output object array.
        /// </summary>
        /// <remarks>
        /// <see cref="OnGetOutputParameters"/> is only called once and the <see cref="HttpParameter">HttpParameters</see>
        /// are cached in a read-only collection.
        /// </remarks>
        /// <returns>
        /// The output <see cref="HttpParameter">HttpParameters</see> that the <see cref="HttpOperationHandler"/>
        /// will provide.
        /// </returns>
        protected abstract IEnumerable<HttpParameter> OnGetOutputParameters();

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input">
        /// The input values that the <see cref="HttpOperationHandler"/> should be handled. 
        /// The values should agree in order and type with the input <see cref="HttpParameter">
        /// HttpParameters</see> given by the <see cref="HttpOperationHandler.InputParameters"/> property.
        /// </param>
        /// <returns>
        /// An array that provides the output values.
        /// </returns>
        protected abstract object[] OnHandle(object[] input);

        private ReadOnlyCollection<HttpParameter> GetValidatedHttpParameters(IEnumerable<HttpParameter> parameters, string methodName)
        {
            if (parameters == null)
            {
                return new ReadOnlyCollection<HttpParameter>(emptyHttpParameters.ToArray());
            }

            List<HttpParameter> parameterList = parameters.ToList();
            for (int index = 0; index < parameterList.Count; index++)
            {
                if (parameterList[index] == null)
                {
                    throw Fx.Exception.AsError(
                        new InvalidOperationException(
                            SR.NullValueInArrayParameterFromGetParameters(
                                HttpOperationHandler.HttpOperationHandlerType.Name,
                                this.GetType().Name,
                                this.OperationName,
                                methodName,
                                index)));
                }

                parameterList[index] = parameterList[index].Clone();
            }

            return new ReadOnlyCollection<HttpParameter>(parameterList);
        }

        private object[] ValidateAndConvertInput(object[] values)
        {
            Fx.Assert(values != null, "The 'values' parameter should not be null.");

            int parameterCount = this.InputParameters.Count;
            int valueCount = values.Length;

            if (valueCount != parameterCount)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.HttpOperationHandlerReceivedWrongNumberOfValues(
                            HttpOperationHandlerType.Name,
                            this.ToString(),
                            this.OperationName,
                            parameterCount,
                            valueCount)));
            }

            int i = 0;
            object[] newValues = new object[valueCount];

            try
            {
                for (i = 0; i < valueCount; ++i)
                {
                    newValues[i] = this.InputParameters[i].ValueConverter.Convert(values[i]);
                }
            }
            catch (Exception innerException)
            {
                if (Fx.IsFatal(innerException))
                {
                    throw;
                }

                if (!this.InputParameters[i].ValueConverter.IsInstanceOf(values[i]))
                {
                    throw Fx.Exception.AsError(
                            new InvalidOperationException(
                                SR.HttpOperationHandlerReceivedWrongType(
                                    HttpOperationHandlerType.Name,
                                    this.ToString(),
                                    this.OperationName,
                                    this.InputParameters[i].Type.Name,
                                    this.InputParameters[i].Name,
                                    values[i].GetType().Name)));
                }
                else if (this.InputParameters[i].ValueConverter.CanConvertFromString &&
                    values[i].GetType() == TypeHelper.StringType)
                {
                    throw Fx.Exception.AsError(
                            new InvalidOperationException(
                                SR.HttpOperationHandlerFailedToConvertInputString(
                                    HttpOperationHandlerType.Name,
                                    this.ToString(),
                                    this.OperationName,
                                    this.InputParameters[i].Type.Name,
                                    this.InputParameters[i].Name,
                                    innerException.Message),
                                innerException));
                }
                else
                {
                    throw Fx.Exception.AsError(
                            new InvalidOperationException(
                                SR.HttpOperationHandlerFailedToGetInnerContent(
                                    HttpOperationHandlerType.Name,
                                    this.ToString(),
                                    this.OperationName,
                                    this.InputParameters[i].Type.Name,
                                    this.InputParameters[i].Name,
                                    values[i].GetType().Name,
                                    innerException.Message),
                                innerException));
                }
            }

            return newValues;
        }

        private object[] ValidateOutput(object[] values)
        {
            int parameterCount = this.OutputParameters.Count;

            if (values == null)
            {
                return new object[parameterCount];
            }

            int valueCount = values.Length;
            if (valueCount != parameterCount)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.HttpOperationHandlerProducedWrongNumberOfValues(
                            HttpOperationHandlerType.Name,
                            this.ToString(),
                            this.OperationName,
                            parameterCount,
                            values.Length)));
            }

            int i = 0;
            for (i = 0; i < valueCount; ++i)
            {
                if (!this.OutputParameters[i].ValueConverter.IsInstanceOf(values[i]))
                {
                    throw Fx.Exception.AsError(
                           new InvalidOperationException(
                               SR.HttpOperationHandlerProducedWrongType(
                                   HttpOperationHandlerType.Name,
                                   this.ToString(),
                                   this.OperationName,
                                   this.OutputParameters[i].Type.Name,
                                   this.OutputParameters[i].Name,
                                   values[i].GetType().Name)));
                }
            }

            return values;
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T">The type of the input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input">
        /// The input value that the <see cref="HttpOperationHandler"/> should handle. 
        /// </param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(T input);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] { this.OnHandle((T)input[0]) };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle((T1)input[0], (T2)input[1])
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2])
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3])
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="T5">The type of the fifth input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, T5, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input5">The fifth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4,
                                        T5 input5);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3],
                            (T5)input[4]) 
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="T5">The type of the fifth input value.</typeparam>
    /// <typeparam name="T6">The type of the sixth input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, T5, T6, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input5">The fifth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input6">The sixth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4,
                                        T5 input5,
                                        T6 input6);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3],
                            (T5)input[4],
                            (T6)input[5]) 
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="T5">The type of the fifth input value.</typeparam>
    /// <typeparam name="T6">The type of the sixth input value.</typeparam>
    /// <typeparam name="T7">The type of the seventh input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, T5, T6, T7, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input5">The fifth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input6">The sixth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input7">The seventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4,
                                        T5 input5,
                                        T6 input6,
                                        T7 input7);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3],
                            (T5)input[4],
                            (T6)input[5],
                            (T7)input[6]) 
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="T5">The type of the fifth input value.</typeparam>
    /// <typeparam name="T6">The type of the sixth input value.</typeparam>
    /// <typeparam name="T7">The type of the seventh input value.</typeparam>
    /// <typeparam name="T8">The type of the eighth input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, T5, T6, T7, T8, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input5">The fifth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input6">The sixth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input7">The seventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input8">The eighth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4,
                                        T5 input5,
                                        T6 input6,
                                        T7 input7,
                                        T8 input8);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3],
                            (T5)input[4],
                            (T6)input[5],
                            (T7)input[6],
                            (T8)input[7])
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="T5">The type of the fifth input value.</typeparam>
    /// <typeparam name="T6">The type of the sixth input value.</typeparam>
    /// <typeparam name="T7">The type of the seventh input value.</typeparam>
    /// <typeparam name="T8">The type of the eighth input value.</typeparam>
    /// <typeparam name="T9">The type of the ninth input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input5">The fifth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input6">The sixth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input7">The seventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input8">The eighth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input9">The ninth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4,
                                        T5 input5,
                                        T6 input6,
                                        T7 input7,
                                        T8 input8,
                                        T9 input9);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3],
                            (T5)input[4],
                            (T6)input[5],
                            (T7)input[6],
                            (T8)input[7],
                            (T9)input[8])
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="T5">The type of the fifth input value.</typeparam>
    /// <typeparam name="T6">The type of the sixth input value.</typeparam>
    /// <typeparam name="T7">The type of the seventh input value.</typeparam>
    /// <typeparam name="T8">The type of the eighth input value.</typeparam>
    /// <typeparam name="T9">The type of the ninth input value.</typeparam>
    /// <typeparam name="T10">The type of the tenth input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input5">The fifth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input6">The sixth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input7">The seventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input8">The eighth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input9">The ninth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input10">The tenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4,
                                        T5 input5,
                                        T6 input6,
                                        T7 input7,
                                        T8 input8,
                                        T9 input9,
                                        T10 input10);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3],
                            (T5)input[4],
                            (T6)input[5],
                            (T7)input[6],
                            (T8)input[7],
                            (T9)input[8],
                            (T10)input[9])
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="T5">The type of the fifth input value.</typeparam>
    /// <typeparam name="T6">The type of the sixth input value.</typeparam>
    /// <typeparam name="T7">The type of the seventh input value.</typeparam>
    /// <typeparam name="T8">The type of the eighth input value.</typeparam>
    /// <typeparam name="T9">The type of the ninth input value.</typeparam>
    /// <typeparam name="T10">The type of the tenth input value.</typeparam>
    /// <typeparam name="T11">The type of the eleventh input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input5">The fifth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input6">The sixth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input7">The seventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input8">The eighth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input9">The ninth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input10">The tenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input11">The eleventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4,
                                        T5 input5,
                                        T6 input6,
                                        T7 input7,
                                        T8 input8,
                                        T9 input9,
                                        T10 input10,
                                        T11 input11);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3],
                            (T5)input[4],
                            (T6)input[5],
                            (T7)input[6],
                            (T8)input[7],
                            (T9)input[8],
                            (T10)input[9],
                            (T11)input[10])
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="T5">The type of the fifth input value.</typeparam>
    /// <typeparam name="T6">The type of the sixth input value.</typeparam>
    /// <typeparam name="T7">The type of the seventh input value.</typeparam>
    /// <typeparam name="T8">The type of the eighth input value.</typeparam>
    /// <typeparam name="T9">The type of the ninth input value.</typeparam>
    /// <typeparam name="T10">The type of the tenth input value.</typeparam>
    /// <typeparam name="T11">The type of the eleventh input value.</typeparam>
    /// <typeparam name="T12">The type of the twelfth input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input5">The fifth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input6">The sixth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input7">The seventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input8">The eighth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input9">The ninth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input10">The tenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input11">The eleventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input12">The twelfth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4,
                                        T5 input5,
                                        T6 input6,
                                        T7 input7,
                                        T8 input8,
                                        T9 input9,
                                        T10 input10,
                                        T11 input11,
                                        T12 input12);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3],
                            (T5)input[4],
                            (T6)input[5],
                            (T7)input[6],
                            (T8)input[7],
                            (T9)input[8],
                            (T10)input[9],
                            (T11)input[10],
                            (T12)input[11])
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="T5">The type of the fifth input value.</typeparam>
    /// <typeparam name="T6">The type of the sixth input value.</typeparam>
    /// <typeparam name="T7">The type of the seventh input value.</typeparam>
    /// <typeparam name="T8">The type of the eighth input value.</typeparam>
    /// <typeparam name="T9">The type of the ninth input value.</typeparam>
    /// <typeparam name="T10">The type of the tenth input value.</typeparam>
    /// <typeparam name="T11">The type of the eleventh input value.</typeparam>
    /// <typeparam name="T12">The type of the twelfth input value.</typeparam>
    /// <typeparam name="T13">The type of the thirteenth input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input5">The fifth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input6">The sixth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input7">The seventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input8">The eighth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input9">The ninth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input10">The tenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input11">The eleventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input12">The twelfth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input13">The thirteenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4,
                                        T5 input5,
                                        T6 input6,
                                        T7 input7,
                                        T8 input8,
                                        T9 input9,
                                        T10 input10,
                                        T11 input11,
                                        T12 input12,
                                        T13 input13);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3],
                            (T5)input[4],
                            (T6)input[5],
                            (T7)input[6],
                            (T8)input[7],
                            (T9)input[8],
                            (T10)input[9],
                            (T11)input[10],
                            (T12)input[11],
                            (T13)input[12])
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="T5">The type of the fifth input value.</typeparam>
    /// <typeparam name="T6">The type of the sixth input value.</typeparam>
    /// <typeparam name="T7">The type of the seventh input value.</typeparam>
    /// <typeparam name="T8">The type of the eighth input value.</typeparam>
    /// <typeparam name="T9">The type of the ninth input value.</typeparam>
    /// <typeparam name="T10">The type of the tenth input value.</typeparam>
    /// <typeparam name="T11">The type of the eleventh input value.</typeparam>
    /// <typeparam name="T12">The type of the twelfth input value.</typeparam>
    /// <typeparam name="T13">The type of the thirteenth input value.</typeparam>
    /// <typeparam name="T14">The type of the fourteenth input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input5">The fifth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input6">The sixth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input7">The seventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input8">The eighth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input9">The ninth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input10">The tenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input11">The eleventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input12">The twelfth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input13">The thirteenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input14">The fourteenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4,
                                        T5 input5,
                                        T6 input6,
                                        T7 input7,
                                        T8 input8,
                                        T9 input9,
                                        T10 input10,
                                        T11 input11,
                                        T12 input12,
                                        T13 input13,
                                        T14 input14);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3],
                            (T5)input[4],
                            (T6)input[5],
                            (T7)input[6],
                            (T8)input[7],
                            (T9)input[8],
                            (T10)input[9],
                            (T11)input[10],
                            (T12)input[11],
                            (T13)input[12],
                            (T14)input[13])
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="T5">The type of the fifth input value.</typeparam>
    /// <typeparam name="T6">The type of the sixth input value.</typeparam>
    /// <typeparam name="T7">The type of the seventh input value.</typeparam>
    /// <typeparam name="T8">The type of the eighth input value.</typeparam>
    /// <typeparam name="T9">The type of the ninth input value.</typeparam>
    /// <typeparam name="T10">The type of the tenth input value.</typeparam>
    /// <typeparam name="T11">The type of the eleventh input value.</typeparam>
    /// <typeparam name="T12">The type of the twelfth input value.</typeparam>
    /// <typeparam name="T13">The type of the thirteenth input value.</typeparam>
    /// <typeparam name="T14">The type of the fourteenth input value.</typeparam>
    /// <typeparam name="T15">The type of the fifteenth input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input5">The fifth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input6">The sixth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input7">The seventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input8">The eighth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input9">The ninth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input10">The tenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input11">The eleventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input12">The twelfth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input13">The thirteenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input14">The fourteenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input15">The fifteenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4,
                                        T5 input5,
                                        T6 input6,
                                        T7 input7,
                                        T8 input8,
                                        T9 input9,
                                        T10 input10,
                                        T11 input11,
                                        T12 input12,
                                        T13 input13,
                                        T14 input14,
                                        T15 input15);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3],
                            (T5)input[4],
                            (T6)input[5],
                            (T7)input[6],
                            (T8)input[7],
                            (T9)input[8],
                            (T10)input[9],
                            (T11)input[10],
                            (T12)input[11],
                            (T13)input[12],
                            (T14)input[13],
                            (T15)input[14])
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }

    /// <summary>
    /// An abstract base class used to create custom <see cref="HttpOperationHandler"/> instances 
    /// that handle a single declared input and a single declared output. 
    /// </summary>
    /// <typeparam name="T1">The type of the first input value.</typeparam>
    /// <typeparam name="T2">The type of the second input value.</typeparam>
    /// <typeparam name="T3">The type of the third input value.</typeparam>
    /// <typeparam name="T4">The type of the fourth input value.</typeparam>
    /// <typeparam name="T5">The type of the fifth input value.</typeparam>
    /// <typeparam name="T6">The type of the sixth input value.</typeparam>
    /// <typeparam name="T7">The type of the seventh input value.</typeparam>
    /// <typeparam name="T8">The type of the eighth input value.</typeparam>
    /// <typeparam name="T9">The type of the ninth input value.</typeparam>
    /// <typeparam name="T10">The type of the tenth input value.</typeparam>
    /// <typeparam name="T11">The type of the eleventh input value.</typeparam>
    /// <typeparam name="T12">The type of the twelfth input value.</typeparam>
    /// <typeparam name="T13">The type of the thirteenth input value.</typeparam>
    /// <typeparam name="T14">The type of the fourteenth input value.</typeparam>
    /// <typeparam name="T15">The type of the fifteenth input value.</typeparam>
    /// <typeparam name="T16">The type of the sixteenth input value.</typeparam>
    /// <typeparam name="TOutput">The type of the output value.</typeparam>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class contains generic forms")]
    public abstract class HttpOperationHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TOutput> : HttpOperationHandler
    {
        private string outputParameterName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationHandler"/> class.
        /// </summary>
        /// <param name="outputParameterName">The name to use for the output <see cref="HttpParameter"/>.</param>
        protected HttpOperationHandler(string outputParameterName)
        {
            if (string.IsNullOrWhiteSpace(outputParameterName))
            {
                throw Fx.Exception.ArgumentNull("outputParameterName");
            }

            this.outputParameterName = outputParameterName;
        }

        /// <summary>
        /// Implemented in a derived class to provide the handling logic of the custom <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <param name="input1">The first input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input2">The second input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input3">The third input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input4">The fourth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input5">The fifth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input6">The sixth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input7">The seventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input8">The eighth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input9">The ninth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input10">The tenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input11">The eleventh input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input12">The twelfth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input13">The thirteenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input14">The fourteenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input15">The fifteenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <param name="input16">The sixteenth input value that the <see cref="HttpOperationHandler"/> should handle.</param>
        /// <returns>The output value returned.</returns>
        public abstract TOutput OnHandle(
                                        T1 input1,
                                        T2 input2,
                                        T3 input3,
                                        T4 input4,
                                        T5 input5,
                                        T6 input6,
                                        T7 input7,
                                        T8 input8,
                                        T9 input9,
                                        T10 input10,
                                        T11 input11,
                                        T12 input12,
                                        T13 input13,
                                        T14 input14,
                                        T15 input15,
                                        T16 input16);

        /// <summary>
        /// Called to handle the given <paramref name="input"/> values.
        /// </summary>
        /// <param name="input">The input values which correspond in length and
        /// type to the <see cref="HttpParameter"/> collection returned
        /// from <see cref="HttpOperationHandler.OnGetInputParameters()"/>.</param>
        /// <returns>The output value.</returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            return new object[] 
            { 
                this.OnHandle(
                            (T1)input[0],
                            (T2)input[1],
                            (T3)input[2],
                            (T4)input[3],
                            (T5)input[4],
                            (T6)input[5],
                            (T7)input[6],
                            (T8)input[7],
                            (T9)input[8],
                            (T10)input[9],
                            (T11)input[10],
                            (T12)input[11],
                            (T13)input[12],
                            (T14)input[13],
                            (T15)input[14],
                            (T16)input[15])
            };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the inputs of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType()).BuildInputParameterCollection();
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing
        /// the output of the <see cref="HttpOperationHandler"/>.
        /// </summary>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            return new ReflectionHttpParameterBuilder(this.GetType())
                        .BuildOutputParameterCollection(this.outputParameterName);
        }
    }
}