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
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Represents the description of a service operation when using the
    /// <see cref="HttpBinding">HttpBinding</see> and the <see cref="HttpBehavior"/>.
    /// </summary>
    public class HttpOperationDescription
    {
        internal const string DefaultNamespace = "http://tempuri.org/";

        private OperationDescription operationDescription;
        private Collection<Attribute> attributes;
        private IList<HttpParameter> inputParameters;
        private HttpParameterCollection outputParameters;
        private HttpParameter returnValue;
        private KeyedByTypeCollection<IOperationBehavior> behaviors;
        private ContractDescription declaringContract;
        private Collection<Type> knownTypes;
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationDescription"/> class.
        /// </summary>
        /// <remarks>This constructor creates an empty instance that must be
        /// populated via its public properties before use.   To create an
        /// instance from an existing <see cref="OperationDescription"/>, use the
        /// extension method <see cref="HttpOperationDescriptionExtensionMethods.ToHttpOperationDescription"/>.
        /// </remarks>
        public HttpOperationDescription()
        {
            Fx.Assert(!this.IsSynchronized, "Default ctor is not synchronized");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationDescription"/> class.
        /// </summary>
        /// <param name="name">The name of the operation.</param>
        /// <param name="declaringContract">The declaring contract to which the operation will belong.</param>
        /// <remarks>
        /// This constructor creates an empty instance that must be
        /// populated via its public properties before use.   To create an
        /// instance from an existing <see cref="OperationDescription"/>, use the
        /// extension method <see cref="HttpOperationDescriptionExtensionMethods.ToHttpOperationDescription"/>.
        /// </remarks>
        public HttpOperationDescription(string name, ContractDescription declaringContract)
        {
            this.operationDescription = new OperationDescription(name, declaringContract);

            if (declaringContract != null)
            {
                declaringContract.Operations.Add(this.operationDescription);
            }

            Fx.Assert(this.IsSynchronized, "This ctor must be synchronized");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationDescription"/> class
        /// using an existing <see cref="OperationDescription"/>.
        /// </summary>
        /// <remarks>
        /// An instance created using this constructor will synchronize changes made to the
        /// instance properties back to the original <see cref="OperationDescription"/>.
        /// </remarks>
        /// <param name="operationDescription">An existing <see cref="OperationDescription"/>
        /// instance on which to base this new <see cref="HttpOperationDescription"/>.</param>
        internal HttpOperationDescription(OperationDescription operationDescription)
        {
            Fx.Assert(operationDescription != null, "operationDescription should not be null");
            this.operationDescription = operationDescription;
            Fx.Assert(this.IsSynchronized, "This ctor must be synchronized");
        }

        /// <summary>
        /// Gets or sets the name of the operation.
        /// </summary>
        /// <remarks>
        /// Attempting to set the name for an instance created from an existing
        /// <see cref="OperationDescription"/> will throw <see cref="NotSupportedException"/>.
        /// </remarks>
        public string Name
        {
            get
            {
                string name = this.IsSynchronized ? this.operationDescription.Name : this.name;
                if (name == null)
                {
                    return SR.UnknownName;
                }

                return name;
            }

            set
            {
                if (this.IsSynchronized)
                {
                     throw Fx.Exception.AsError(
                         new NotSupportedException(SR.HttpDescriptionPropertyImmutable("Name", typeof(OperationDescription).Name)));
                }

                this.name = value;
            }
        }

        /// <summary>
        /// Gets the operation behaviors associated with the operation.
        /// </summary>
        public KeyedByTypeCollection<IOperationBehavior> Behaviors
        {
            get
            {
                if (this.IsSynchronized)
                {
                    return this.operationDescription.Behaviors;
                }
                else
                {
                    if (this.behaviors == null)
                    {
                        this.behaviors = new KeyedByTypeCollection<IOperationBehavior>();
                    }

                    return this.behaviors;
                }
            }
        }

        /// <summary>
        /// Gets or sets the contract to which the operation belongs.
        /// </summary>
        public ContractDescription DeclaringContract
        {
            get
            {
                return this.IsSynchronized ? this.operationDescription.DeclaringContract : this.declaringContract;
            }

            set
            {
                if (this.IsSynchronized)
                {
                    this.operationDescription.DeclaringContract = value;
                }
                else
                {
                    this.declaringContract = value;
                }
            }
        }

        /// <summary>
        /// Gets the known types associated with the operation description.
        /// </summary>
        public Collection<Type> KnownTypes
        {
            get
            {
                if (this.IsSynchronized)
                {
                    return this.operationDescription.KnownTypes;
                }
                else
                {
                    if (this.knownTypes == null)
                    {
                        this.knownTypes = new Collection<Type>();
                    }

                    return this.knownTypes;
                }
            }
        }

        /// <summary>
        /// Gets the custom attributes associated with the operation.
        /// </summary>
        public Collection<Attribute> Attributes
        {
            get
            {
                if (this.IsSynchronized)
                {
                    IEnumerable<Attribute> attrs = (this.operationDescription.SyncMethod != null)
                                   ? this.operationDescription.SyncMethod.GetCustomAttributes(true).Cast<Attribute>()
                                   : (this.operationDescription.BeginMethod != null)
                                       ? this.operationDescription.BeginMethod.GetCustomAttributes(true).Cast<Attribute>()
                                       : Enumerable.Empty<Attribute>();
                    return new Collection<Attribute>(attrs.ToList());
                }

                if (this.attributes == null)
                {
                    this.attributes = new Collection<Attribute>();
                }

                return this.attributes;
            }
        }

        /// <summary>
        ///  Gets or sets the description of the value returned by the operation.
        /// </summary>
        /// <value>
        /// This value may be <c>null</c>.  If the current instance is synchronized
        /// with respect to an <see cref="OperationDescription"/>, it will be modified
        /// to reflect the new value.
        /// </value>
        public HttpParameter ReturnValue
        {
            get
            {
                if (this.IsSynchronized)
                {
                    return ((this.operationDescription.Messages.Count > 1) &&
                            (this.operationDescription.Messages[1].Body.ReturnValue != null))
                                ? new HttpParameter(this.operationDescription.Messages[1].Body.ReturnValue)
                                : null;
                }

                return this.returnValue;
            }

            set
            {
                if (this.IsSynchronized)
                {
                    CreateMessageDescriptionIfNecessary(this.operationDescription, messageIndex: 1);
                    MessagePartDescription messagePartDescription = null;
                    if (value != null)
                    {
                        value.SynchronizeToMessagePartDescription(this.operationDescription.Messages[1]);
                        messagePartDescription = value.MessagePartDescription;
                    }

                    this.operationDescription.Messages[1].Body.ReturnValue = messagePartDescription;
                }
                else
                {
                    this.returnValue = value;
                }
            }
        }

        /// <summary>
        /// Gets the collection of input parameters used by this operation.
        /// </summary>
        public IList<HttpParameter> InputParameters
        {
            get
            {
                if (this.IsSynchronized)
                {
                    return new HttpParameterCollection(this.operationDescription, isOutputCollection: false);
                }

                if (this.inputParameters == null)
                {
                    this.inputParameters = new HttpParameterCollection();
                }

                return this.inputParameters;
            }
        }

        /// <summary>
        ///  Gets the collection of output parameters used by this operation.
        /// </summary>
        public IList<HttpParameter> OutputParameters
        {
            get
            {
                if (this.IsSynchronized)
                {
                    return new HttpParameterCollection(this.operationDescription, isOutputCollection: true);
                }

                if (this.outputParameters == null)
                {
                    this.outputParameters = new HttpParameterCollection();
                }

                return this.outputParameters;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current instance is synchronized with
        /// respect to an <see cref="OperationDescription"/>.
        /// </summary>
        private bool IsSynchronized
        {
            get
            {
                return this.operationDescription != null;
            }
        }

        /// <summary>
        /// Retrieves an <see cref="OperationDescription"/> that matches the current instance.
        /// </summary>
        /// <remarks>
        /// This method will throw <see cref="NotImplementedException"/> if a matching
        /// <see cref="OperationDescription"/> is not available.
        /// </remarks>
        /// <returns>The <see cref="OperationDescription"/>.   It will not be <c>null</c>.</returns>
        public OperationDescription ToOperationDescription()
        {
            OperationDescription operationDescription = this.operationDescription;
            if (operationDescription == null)
            {
                throw Fx.Exception.AsError(
                    new NotImplementedException(SR.HttpOperationDescriptionNullOperationDescription(typeof(OperationDescription).Name)));
            }

            return operationDescription;
        }

        /// <summary>
        /// Ensures that a <see cref="MessageDescription"/> exists for the given <paramref name="messageIndex"/>
        /// within the specified <see cref="OperationDescription"/>.
        /// A default one will be created if it does not yet exist.
        /// </summary>
        /// <param name="operationDescription">The <see cref="OperationDescription"/> to check and modify.</param>
        /// <param name="messageIndex">The index within the <see cref="MessageDescriptionCollection"/> that must exist.</param>
        internal static void CreateMessageDescriptionIfNecessary(OperationDescription operationDescription, int messageIndex)
        {
            Fx.Assert(operationDescription != null, "OperationDescription cannot be null");
            Fx.Assert(messageIndex >= 0 && messageIndex <= 1, "MessageIndex must be 0 or 1");

            if (operationDescription.Messages.Count <= messageIndex)
            {
                // Messages[0] is input and must be created in all cases
                if (operationDescription.Messages.Count == 0)
                {
                    MessageDescription messageDescription = new MessageDescription(string.Empty, MessageDirection.Input);
                    EnsureWrapperNamespace(operationDescription, messageDescription);
                    operationDescription.Messages.Add(messageDescription);
                }

                // Messages[1] is always output
                if (messageIndex > 0 && operationDescription.Messages.Count <= 1)
                {
                    MessageDescription messageDescription = new MessageDescription(string.Empty, MessageDirection.Output);
                    EnsureWrapperNamespace(operationDescription, messageDescription);
                    operationDescription.Messages.Add(messageDescription);
                }
            }

            // Post-condition guarantee
            Fx.Assert(operationDescription.Messages.Count > messageIndex, "Expected Messages[messageIndex] to exist");
        }

        private static void EnsureWrapperNamespace(OperationDescription operationDescription, MessageDescription messageDescription)
        {
            Fx.Assert(operationDescription != null, "OperationDescription cannot be null");
            Fx.Assert(messageDescription != null, "MessageDescription cannot be null");

            if (string.IsNullOrWhiteSpace(messageDescription.Body.WrapperNamespace))
            {
                ContractDescription contract = operationDescription.DeclaringContract;
                messageDescription.Body.WrapperNamespace = contract != null ?
                    contract.Namespace :
                    DefaultNamespace;
            }
        }
    }
}
