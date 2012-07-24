// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Description;

    /// <summary>
    /// Class used to generate the input and output collections of <see cref="HttpParameter"/> instances 
    /// for the generic versions of <see cref="HttpOperationHandler"/> based on the method signatures of
    /// the <see cref="HttpOperationHandler.OnHandle"/> method.
    /// </summary>
    internal class ReflectionHttpParameterBuilder
    {
        private static readonly Type HttpOperationHandlerType = typeof(HttpOperationHandler);

        private MethodInfo handleMethod;
        private Type[] genericParameterTypes;
        private Type outputParameterType;
        private Type[] inputParameterTypes;
        private Type httpOperationHandlerType;

        internal ReflectionHttpParameterBuilder(Type handlerType)
        {
            this.httpOperationHandlerType = handlerType;
        }

        private MethodInfo HandleMethod
        {
            get
            {
                if (this.handleMethod == null)
                {
                    this.handleMethod = FindHandleMethod(this.httpOperationHandlerType, this.OutputParameterType, this.InputParameterTypes);
                }

                return this.handleMethod;
            }
        }

        private Type[] GenericParameterTypes
        {
            get
            {
                if (this.genericParameterTypes == null)
                {
                    this.genericParameterTypes = GetGenericParameters(this.httpOperationHandlerType);
                }

                return this.genericParameterTypes;
            }
        }

        private Type OutputParameterType
        {
            get
            {
                if (this.outputParameterType == null)
                {
                    Type[] genericParameters = this.GenericParameterTypes;
                    this.outputParameterType = genericParameters[genericParameters.Length - 1];
                }

                return this.outputParameterType;
            }
        }

        private Type[] InputParameterTypes
        {
            get
            {
                if (this.inputParameterTypes == null)
                {
                    Type[] genericParameters = this.GenericParameterTypes;
                    this.inputParameterTypes = new Type[genericParameters.Length - 1];
                    Array.Copy(genericParameters, this.inputParameterTypes, genericParameters.Length - 1);
                }

                return this.inputParameterTypes;
            }
        }

        internal IEnumerable<HttpParameter> BuildInputParameterCollection()
        {
            MethodInfo executeMethod = this.HandleMethod;
            if (executeMethod == null)
            {
                return Enumerable.Empty<HttpParameter>();
            }

            ParameterInfo[] parameterInfos = executeMethod.GetParameters();
            HttpParameter[] arguments = new HttpParameter[parameterInfos.Length];
            for (int i = 0; i < parameterInfos.Length; ++i)
            {
                ParameterInfo parameter = parameterInfos[i];
                HttpParameter pad = new HttpParameter(parameter.Name, parameter.ParameterType);
                arguments[i] = pad;
            }

            return arguments;
        }

        internal IEnumerable<HttpParameter> BuildOutputParameterCollection(string outputParameterName)
        {
            HttpParameter parameter = new HttpParameter(outputParameterName, this.OutputParameterType);
            return new HttpParameter[] { parameter };
        }

        private static Type[] GetGenericParameters(Type handlerType)
        {
            Fx.Assert(handlerType != null, "The 'handlerType' parameter should not be null.");

            Type genericBase = null;
            for (Type baseType = handlerType; baseType != typeof(object); baseType = baseType.BaseType)
            {
                Fx.Assert(baseType != HttpOperationHandlerType, "This should only have been called with a generic form of HttpOperationHandler.");
                if (baseType.IsGenericType && baseType.BaseType == HttpOperationHandlerType)
                {
                    genericBase = baseType;

                    break;
                }
            }

            Fx.Assert(genericBase != null, "Should have found a generic HttpOperationHandler base class");
            return genericBase.GetGenericArguments();
        }

        private static MethodInfo FindHandleMethod(Type handlerType, Type outputParameterType, params Type[] inputParameterTypes)
        {
            Fx.Assert(handlerType != null, "The 'handlerType' parameter should not be null.");
            Fx.Assert(outputParameterType != null, "The 'outputParameterType' parameter should not be null.");
            Fx.Assert(inputParameterTypes != null, "The 'inputParameterTypes' parameter should not be null.");

            foreach (MethodInfo method in handlerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (String.Equals("OnHandle", method.Name, StringComparison.OrdinalIgnoreCase) &&
                    outputParameterType.IsAssignableFrom(method.ReturnType))
                {
                    ParameterInfo[] methodParameters = method.GetParameters();
                    if (methodParameters.Length == inputParameterTypes.Length)
                    {
                        bool methodMatch = true;
                        for (int i = 0; i < methodParameters.Length; ++i)
                        {
                            if (!methodParameters[i].ParameterType.IsAssignableFrom(inputParameterTypes[i]))
                            {
                                methodMatch = false;
                                break;
                            }
                        }

                        if (methodMatch)
                        {
                            return method;
                        }    
                    }
                }
            }

            return null;
        }
    }
}