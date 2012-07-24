// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// A static class that provides HTTP related types and functionality
    /// around checking types against the set of HTTP related types.
    /// </summary>
    internal static class HttpTypeHelper 
    {
        internal static readonly Type HttpRequestMessageType = typeof(HttpRequestMessage);
        internal static readonly Type HttpRequestHeadersType = typeof(HttpRequestHeaders);
        internal static readonly Type UriType = typeof(Uri);
        internal static readonly Type HttpMethodType = typeof(HttpMethod);
        internal static readonly Type HttpContentType = typeof(HttpContent);
        internal static readonly Type HttpResponseMessageType = typeof(HttpResponseMessage);
        internal static readonly Type HttpResponseHeadersType = typeof(HttpResponseHeaders);
        internal static readonly Type HttpStatusCodeType = typeof(HttpStatusCode);
        internal static readonly Type HttpRequestMessageGenericType = typeof(HttpRequestMessage<>);
        internal static readonly Type HttpResponseMessageGenericType = typeof(HttpResponseMessage<>);
        internal static readonly Type ObjectContentGenericType = typeof(ObjectContent<>);

        internal static bool IsHttpContent(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            return HttpContentType.IsAssignableFrom(type);
        }

        internal static bool IsHttpResponse(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            return HttpResponseMessageType.IsAssignableFrom(type);
        }

        internal static bool IsHttpRequest(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            return HttpRequestMessageType.IsAssignableFrom(type);
        }

        internal static bool IsHttpResponseOrContent(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            return HttpContentType.IsAssignableFrom(type) || 
                   HttpResponseMessageType.IsAssignableFrom(type);
        }

        internal static bool IsHttpRequestOrContent(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            return HttpContentType.IsAssignableFrom(type) || 
                   HttpRequestMessageType.IsAssignableFrom(type);
        }

        internal static bool IsHttp(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            return HttpContentType.IsAssignableFrom(type) || 
                   HttpRequestMessageType.IsAssignableFrom(type) || 
                   HttpResponseMessageType.IsAssignableFrom(type);
        }

        internal static bool IsHttpContentGenericTypeDefinition(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            if (type.IsGenericTypeDefinition &&
                ObjectContentGenericType.IsAssignableFrom(type))
            {
                return true;
            }

            return false;
        }

        internal static bool IsHttpRequestGenericTypeDefinition(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            if (type.IsGenericTypeDefinition &&
                HttpRequestMessageGenericType.IsAssignableFrom(type))
            {
                return true;
            }

            return false;
        }

        internal static bool IsHttpResponseGenericTypeDefinition(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            if (type.IsGenericTypeDefinition &&
                HttpResponseMessageGenericType.IsAssignableFrom(type))
            {
                return true;
            }

            return false;
        }

        internal static bool IsHttpRequestOrContentGenericTypeDefinition(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            if (type.IsGenericTypeDefinition)
            {
                if (HttpRequestMessageGenericType.IsAssignableFrom(type) ||
                    ObjectContentGenericType.IsAssignableFrom(type))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsHttpResponseOrContentGenericTypeDefinition(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            if (type.IsGenericTypeDefinition)
            {
                if (HttpResponseMessageGenericType.IsAssignableFrom(type) ||
                    ObjectContentGenericType.IsAssignableFrom(type))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsHttpGenericTypeDefinition(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            if (type.IsGenericTypeDefinition)
            {
                if (HttpRequestMessageGenericType.IsAssignableFrom(type) ||
                    HttpResponseMessageGenericType.IsAssignableFrom(type) ||
                    ObjectContentGenericType.IsAssignableFrom(type))
                {
                    return true;
                }
            }

            return false;
        }

        internal static Type GetHttpContentInnerTypeOrNull(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                if (IsHttpContentGenericTypeDefinition(genericTypeDefinition))
                {
                    Type[] typeArgs = type.GetGenericArguments();
                    if (typeArgs.Length > 1)
                    {
                        throw Fx.Exception.AsError(
                            new InvalidOperationException(
                                SR.MultipleTypeParametersForHttpContentType(type.Name)));
                    }

                    return typeArgs[0];
                }
            }

            return null;
        }

        internal static Type GetHttpRequestInnerTypeOrNull(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                if (IsHttpRequestGenericTypeDefinition(genericTypeDefinition))
                {
                    Type[] typeArgs = type.GetGenericArguments();
                    if (typeArgs.Length > 1)
                    {
                        throw Fx.Exception.AsError(
                            new InvalidOperationException(
                                SR.MultipleTypeParametersForHttpContentType(type.Name)));
                    }

                    return typeArgs[0];
                }
            }

            return null;
        }

        internal static Type GetHttpResponseInnerTypeOrNull(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                if (IsHttpResponseGenericTypeDefinition(genericTypeDefinition))
                {
                    Type[] typeArgs = type.GetGenericArguments();
                    if (typeArgs.Length > 1)
                    {
                        throw Fx.Exception.AsError(
                            new InvalidOperationException(
                                SR.MultipleTypeParametersForHttpContentType(type.Name)));
                    }

                    return typeArgs[0];
                }
            }

            return null;
        }

        internal static Type GetHttpRequestOrContentInnerTypeOrNull(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                if (IsHttpRequestOrContentGenericTypeDefinition(genericTypeDefinition))
                {
                    Type[] typeArgs = type.GetGenericArguments();
                    if (typeArgs.Length > 1)
                    {
                        throw Fx.Exception.AsError(
                            new InvalidOperationException(
                                SR.MultipleTypeParametersForHttpContentType(type.Name)));
                    }

                    return typeArgs[0];
                }
            }

            return null;
        }

        internal static Type GetHttpResponseOrContentInnerTypeOrNull(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                if (IsHttpResponseOrContentGenericTypeDefinition(genericTypeDefinition))
                {
                    Type[] typeArgs = type.GetGenericArguments();
                    if (typeArgs.Length > 1)
                    {
                        throw Fx.Exception.AsError(
                            new InvalidOperationException(
                                SR.MultipleTypeParametersForHttpContentType(type.Name)));
                    }

                    return typeArgs[0];
                }
            }

            return null;
        }

        internal static Type GetHttpInnerTypeOrNull(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                if (IsHttpGenericTypeDefinition(genericTypeDefinition))
                {
                    Type[] typeArgs = type.GetGenericArguments();
                    if (typeArgs.Length > 1)
                    {
                        // TODO: Throw exception because there is more than one type argument so we
                        //  don't know which argument is the body content.
                        throw new InvalidOperationException("Exception Message");
                    }

                    return typeArgs[0];
                }
            }

            return null;
        }

        internal static Type MakeHttpRequestMessageOf(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            Type[] typeParams = new Type[] { type };
            return HttpTypeHelper.HttpRequestMessageGenericType.MakeGenericType(typeParams);
        }

        internal static Type MakeHttpResponseMessageOf(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            Type[] typeParams = new Type[] { type };
            return HttpTypeHelper.HttpResponseMessageGenericType.MakeGenericType(typeParams);
        }

        internal static Type MakeObjectContentOf(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            Type[] typeParams = new Type[] { type };
            return HttpTypeHelper.ObjectContentGenericType.MakeGenericType(typeParams);
        }
    }
}
