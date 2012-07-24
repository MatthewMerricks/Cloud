// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System.Linq;

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;

    /// <summary>
    /// Use this attribute at the contract level or operation level to turn on the QueryComposition feature
    /// Alternatively, you can provide the custom query composer type using this attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class QueryCompositionAttribute : Attribute, IContractBehavior, IOperationBehavior
    {
        internal static readonly Type EnumerableType = typeof(IEnumerable);
        internal static readonly Type GenericEnumerableType = typeof(IEnumerable<>);
        //FIX: GB - IQueryable
        internal static readonly Type GenericQueryableType = typeof (IQueryable<>);

        private static Type queryComposerTypeConstant = typeof(IQueryComposer);

        private Type queryComposerType;
        private bool enabled;

        /// <summary>
        /// Constructs a QueryComposition attribute
        /// </summary>
        /// <param name="queryComposerType">The custom query composer type</param>
        public QueryCompositionAttribute(Type queryComposerType)
            : this()
        {
            // Validate queryComposerType
            if (queryComposerType == null)
            {
                throw new ArgumentException("The type of QueryComposer cannot be null.");
            }

            if (!queryComposerType.IsClass)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The type of QueryComposer {0} must be a class type.", queryComposerType));
            }

            // verify if the provided queryComposerType implements IQueryComposer
            Type interfaceType = null;
            foreach (Type interfaceInType in queryComposerType.GetInterfaces())
            {
                if (interfaceInType == queryComposerTypeConstant)
                {
                    interfaceType = interfaceInType;
                    break;
                }
            }

            if (interfaceType == null)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The QueryComposer type {0} must implement the interface IQueryComposer.", queryComposerType));
            }

            this.queryComposerType = queryComposerType;
        }

        /// <summary>
        /// Construct a default QueryComposition attribute which uses the default query composer
        /// </summary>
        public QueryCompositionAttribute()
        {
            this.enabled = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this feature is on or off
        /// </summary>
        public bool Enabled
        {
            get
            {
                return this.enabled;
            }

            set
            {
                this.enabled = value;
            }
        }

        /// <summary>
        /// Gets the QueryComposer Type
        /// </summary>
        public Type QueryComposerType
        {
            get
            {
                return this.queryComposerType;
            }
        }

        void IOperationBehavior.AddBindingParameters(OperationDescription operationDescription, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
            // do nothing
        }

        void IOperationBehavior.ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
            // do nothing
        }

        void IOperationBehavior.ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            this.AddQueryComposer(dispatchOperation, operationDescription, true);
        }

        void IOperationBehavior.Validate(OperationDescription operationDescription)
        {
            // do nothing
        }

        void IContractBehavior.AddBindingParameters(ContractDescription contractDescription, ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        void IContractBehavior.ApplyClientBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        void IContractBehavior.ApplyDispatchBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, DispatchRuntime dispatchRuntime)
        {
            if (contractDescription == null)
            {
                throw new ArgumentNullException("contractDescription");
            }

            foreach (OperationDescription od in contractDescription.Operations)
            {
                if (!od.Behaviors.Contains(typeof(QueryCompositionAttribute)))
                {
                    // add the operation behavior
                    od.Behaviors.Add(this);
                }
            }
        }

        void IContractBehavior.Validate(ContractDescription contractDescription, ServiceEndpoint endpoint)
        {
        }

        private void AddQueryComposer(DispatchOperation dispatchOperation, OperationDescription operationDescription, bool isAttributeSetOnOperation)
        {
            // QueryComposer is added for 2-way operations when the returnType is IEnumerable, IEnumerable<>, IQueryable or IQueryable<>
            // if enabled and the user did not specify the queryComposerType, add the UrlQueryComposerType
            if (dispatchOperation.IsOneWay)
            {
                if (isAttributeSetOnOperation && this.enabled)
                {
                    // throw if the QueryCompositionAttribute was set on the Operation
                    throw new InvalidOperationException(String.Format(
                        CultureInfo.InvariantCulture,
                        "QueryComposition attribute was set on one-way operation {0}. Please remove this attribute from this operation.",
                         dispatchOperation.Name));
                }

                return;
            }
            else
            {
                // Verify if the returnType type is IEnumerable, IEnumerable<>, IQueryable or IQueryable<>
                bool isQueryCompositionSupportedOnReturnType = false;
                Type returnType = null;
                if (operationDescription.Messages[1] != null && operationDescription.Messages[1].Body != null && operationDescription.Messages[1].Body.ReturnValue != null)
                {
                    returnType = operationDescription.Messages[1].Body.ReturnValue.Type;
                    if (returnType != null)
                    {
                        if (returnType == EnumerableType)
                        {
                            isQueryCompositionSupportedOnReturnType = true;
                        }
                        else if (returnType.IsGenericType)
                        {
                            Type genericTypeDefinition = returnType.GetGenericTypeDefinition();

                            if (genericTypeDefinition == GenericEnumerableType)
                            {
                                isQueryCompositionSupportedOnReturnType = true;
                            } //FIX: GB - IQueryable
                            else if (genericTypeDefinition == GenericQueryableType)
                            {
                                isQueryCompositionSupportedOnReturnType = true;
                            }
                        }
                    }
                }

                // if the returnType is not IEnumerable, IEnumerable<>, we don't set the QueryComposer
                if (!isQueryCompositionSupportedOnReturnType)
                {
                    // throw if the QueryCompositionAttribute was set on the Operation
                    if (isAttributeSetOnOperation && this.enabled)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                CultureInfo.InvariantCulture,
                                "QueryComposition attribute was set on operation {0}, but the return type is not defined as IEnumerable. Please remove this attribute from this operation or change the operation return type to IEnumerable",
                                dispatchOperation.Name));
                    }

                    return;
                }

                if (this.enabled)
                {
                    IQueryComposer queryComposer = null;
                    if (this.queryComposerType == null)
                    {
                        queryComposer = new UrlQueryComposer();
                    }
                    else
                    {
                        queryComposer = (IQueryComposer)Activator.CreateInstance(this.queryComposerType);
                    }

                    dispatchOperation.Invoker = new QueryCompositionOperationInvoker(dispatchOperation.Invoker, queryComposer);
                }
            }
        }
    }
}