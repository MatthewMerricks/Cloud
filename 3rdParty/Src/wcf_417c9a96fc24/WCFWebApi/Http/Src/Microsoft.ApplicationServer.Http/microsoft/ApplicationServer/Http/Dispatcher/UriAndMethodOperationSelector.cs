// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel.Web;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.ServiceModel;

    /// <summary>
    /// Defines the operation selector that matches the incoming requests to operations by means of UriTemplateTableMatch. Matching is done on the HTTP verb and the request Uri.
    /// </summary>
    public class UriAndMethodOperationSelector : HttpOperationSelector
    {
        /// <summary>
        /// Denotes a match-all HttpMethod
        /// </summary>
        public const string WildcardMethod = "*";

        private static HttpMethod wildCardHttpMethod = new HttpMethod(WildcardMethod);

        private TrailingSlashMode trailingSlashMode;
        private Uri baseAddress;
        private UriTemplateTable wildcardTable; // this is one of the methodSpecificTables, special-cased for faster access
        private string catchAllOperationName = String.Empty; // user UT=* Method=* operation, else unhandled invoker
        private Dictionary<HttpMethod, UriTemplateTable> methodSpecificTables; // indexed by the http method name
        private int baseAddressSegmentCount;
        private bool baseAddressDoesNotHaveTrailingSlash;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationSelector"/> class with a 
        /// list of <see cref="HttpOperationSelectionData"/> 
        /// </summary>
        /// <param name="baseAddress">BaseAddress used for mapping the requestUri to the entries in operationList</param>
        /// <param name="operations"> 
        /// The <see cref="HttpOperationSelectionData"/> instances that describe the operations of the service.
        /// </param>
        public UriAndMethodOperationSelector(Uri baseAddress, IEnumerable<HttpOperationDescription> operations)
            : this(baseAddress, operations, TrailingSlashMode.AutoRedirect)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationSelector"/> class with a 
        /// list of <see cref="HttpOperationSelectionData"/> and <see cref="TrailingSlashMode"/>.
        /// </summary>
        /// <param name="baseAddress">BaseAddress used for mapping the requestUri to the entries in operationList</param>
        /// <param name="operations"> 
        /// The <see cref="HttpOperationSelectionData"/> instances that describe the operations of the service.
        /// </param>
        /// <param name="trailingSlashMode">
        /// The setting for the 
        /// <see cref="TrailingSlashMode">TrailingSlashMode</see>.
        /// </param>
        public UriAndMethodOperationSelector(Uri baseAddress, IEnumerable<HttpOperationDescription> operations, TrailingSlashMode trailingSlashMode)
        {
            if (baseAddress == null)
            {
                throw Fx.Exception.ArgumentNull("baseAddress");
            }

            if (operations == null)
            {
                throw Fx.Exception.ArgumentNull("operations");
            }

            this.baseAddress = baseAddress;
            if (!this.baseAddress.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
            {
                this.baseAddressDoesNotHaveTrailingSlash = true;
                this.baseAddressSegmentCount = this.baseAddress.Segments.Length;
            }

            this.trailingSlashMode = trailingSlashMode;
            this.methodSpecificTables = GenerateMethodSpecificTables(operations, this.baseAddress, out this.catchAllOperationName);
            this.methodSpecificTables.TryGetValue(wildCardHttpMethod, out this.wildcardTable);
        }

        /// <summary>
        /// Gets <see cref="TrailingSlashMode "/> used for matching incoming request
        /// </summary>
        public TrailingSlashMode TrailingSlashMode
        {
            get
            {
                return this.trailingSlashMode;
            }
        }

        internal Uri HelpPageUri { get; set; }

        /// <summary>
        /// Select a service operation based on the HTTP verb and the request Uri of the incoming request message. 
        /// </summary>
        /// <param name="request">The incoming <see cref="HttpRequestMessage"/>.</param>
        /// <param name="operationName">When this method returns true, operationName will be the name of the operation to be associated with the message.</param>
        /// <param name="matchDiffersByTrailingSlash">Boolean value indicating whether the match differs from the message.RequestUri by a trailing slash</param>
        /// <returns>boolean value indicating whether we found a explicit matching operation for the request. The request Uri could be an exact match or differing by a trailing slash.This method does not look for a wild card method match.</returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#", Justification = "This API needs to return multiple things")]
        public bool TrySelectOperation(HttpRequestMessage request, out string operationName, out bool matchDiffersByTrailingSlash)
        {
            if (request == null)
            {
                throw Fx.Exception.ArgumentNull("request");
            }

            return this.OnTrySelectOperation(request, out operationName, out matchDiffersByTrailingSlash);
        }

        /// <summary>
        /// Selects a service operation based on the HTTP verb and the request Uri of the incoming request message. 
        /// </summary>
        /// <param name="request">The incoming <see cref="HttpRequestMessage"/>.</param>
        /// <returns>The name of the operation to be associated with the message.</returns>
        protected override sealed string OnSelectOperation(HttpRequestMessage request)
        {
            Fx.Assert(request != null, "The base class ensures that the 'request' parameter will not be null.");

            bool matchDifferByTrailingSlash;
            string operationName = null;
            HttpResponseMessage errorResponse = null;

            if (request.RequestUri != null)
            {
                if (this.TrySelectOperation(request, out operationName, out matchDifferByTrailingSlash))
                {
                    if (matchDifferByTrailingSlash && this.trailingSlashMode == TrailingSlashMode.AutoRedirect)
                    {
                        Uri newLocation = BuildUriDifferingByTrailingSlash(request);
                        errorResponse = StandardHttpResponseMessageBuilder.CreateTemporaryRedirectResponse(request, request.RequestUri, newLocation);
                    }
                }
                else if (!CanUriMatch(this.wildcardTable, request.RequestUri, out operationName))
                {
                    IEnumerable<HttpMethod> allowedMethods = this.RetrieveAllowedMethodsIfAny(request);

                    if (allowedMethods != null)
                    {
                        errorResponse = StandardHttpResponseMessageBuilder.CreateMethodNotAllowedResponse(request, allowedMethods, this.HelpPageUri);
                    }
                }
            }

            if (operationName == null && errorResponse == null)
            {
                errorResponse = StandardHttpResponseMessageBuilder.CreateNotFoundResponse(request, this.HelpPageUri);
            }

            if (errorResponse != null)
            {
                throw Fx.Exception.AsError(new HttpResponseException(errorResponse));
            }

            return operationName;
        }

        /// <summary>
        /// Called by <see cref="TrySelectOperation"/> to select a service operation based on the HTTP verb and the request Uri of the incoming request message.
        /// </summary>
        /// <param name="request">The incoming <see cref="HttpRequestMessage"/>.</param>
        /// <param name="operationName">When this method returns true, operationName will be the name of the operation to be associated with the message.</param>
        /// <param name="matchDifferByTrailingSlash">Boolean value indicating whether the match differs from the message.RequestUri by a trailing slash</param>
        /// <returns>boolean value indicating whether we found a explicit matching operation for the request. The request Uri could be an exact match or differing by a trailing slash.This method does not look for a wild card method match.</returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#", Justification = "This API needs to return multiple things")]
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#", Justification = "This API needs to return multiple things")]
        protected virtual bool OnTrySelectOperation(HttpRequestMessage request, out string operationName, out bool matchDifferByTrailingSlash)
        {
            if (request == null)
            {
                throw Fx.Exception.ArgumentNull("request");
            }

            operationName = null;
            matchDifferByTrailingSlash = false;
            if (request.RequestUri == null)
            {
                return false;
            }

            UriTemplateTable methodSpecificTable;
            bool methodMatchesExactly = this.methodSpecificTables.TryGetValue(request.Method, out methodSpecificTable);

            if (methodMatchesExactly)
            {
                Uri requestUri = request.RequestUri;
                bool uriMatched = CanUriMatch(methodSpecificTable, requestUri, out operationName);
                if (uriMatched)
                {
                    // If the requestUri differs from the baseAddress by a trailing slash, the UriTemplateTable will always 
                    //  add a trailing slash. This results in false positives when the requestUri is differing from baseUri by a trailing slash
                    if (this.baseAddressDoesNotHaveTrailingSlash)
                    {
                        if (this.baseAddressSegmentCount == requestUri.Segments.Length &&
                            requestUri.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
                        {
                            matchDifferByTrailingSlash = true;
                        }
                    }

                    return true;
                }

                // Determine if there is a match if the trailing slash is ignored
                Uri requestUriDifferingByTrailingSlash = BuildUriDifferingByTrailingSlash(request);
                if (CanUriMatch(methodSpecificTable, requestUriDifferingByTrailingSlash, out operationName))
                {
                    matchDifferByTrailingSlash = String.Equals(this.baseAddress.AbsolutePath, requestUri.AbsolutePath, StringComparison.OrdinalIgnoreCase) ? false : true;
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<HttpMethod, UriTemplateTable> GenerateMethodSpecificTables(IEnumerable<HttpOperationDescription> operations, Uri baseAddress, out string catchAllOperationName)
        {
            Fx.Assert(operations != null, "The 'operations' parameter should not be null.");

            catchAllOperationName = string.Empty;
            Dictionary<HttpMethod, UriTemplateTable> methodSpecificTables = new Dictionary<HttpMethod, UriTemplateTable>();

            Dictionary<OperationRoutingKey, string> alreadyHaves = new Dictionary<OperationRoutingKey, string>();
            foreach (HttpOperationDescription operation in operations)
            {
                UriTemplate uriTemplate = operation.GetUriTemplate();
                string uriTemplateString = uriTemplate.ToString();
                HttpMethod httpMethod = operation.GetHttpMethod();

                if (uriTemplate.IsWildcardPath() && httpMethod.Method == WildcardMethod)
                {
                    if (catchAllOperationName != string.Empty)
                    {
                        throw Fx.Exception.AsError(
                           new InvalidOperationException(
                            Http.SR.MultipleOperationsWithSameMethodAndUriTemplate(uriTemplateString, httpMethod.Method)));
                    }

                    catchAllOperationName = operation.Name;
                }

                OperationRoutingKey operationKey = new OperationRoutingKey(uriTemplate, httpMethod);
                if (alreadyHaves.ContainsKey(operationKey))
                {
                    throw Fx.Exception.AsError(
                        new InvalidOperationException(
                            Http.SR.MultipleOperationsWithSameMethodAndUriTemplate(uriTemplateString, httpMethod.Method)));
                }

                alreadyHaves.Add(operationKey, operation.Name);

                UriTemplateTable methodSpecificTable;
                if (!methodSpecificTables.TryGetValue(httpMethod, out methodSpecificTable))
                {
                    methodSpecificTable = new UriTemplateTable(baseAddress);
                    methodSpecificTables.Add(httpMethod, methodSpecificTable);
                }

                methodSpecificTable.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(uriTemplate, operation.Name));
            }

            foreach (UriTemplateTable table in methodSpecificTables.Values)
            {
                table.MakeReadOnly(true);
            }

            return methodSpecificTables;
        }

        private static Uri BuildUriDifferingByTrailingSlash(HttpRequestMessage request)
        {
            UriBuilder uriBuilder = new UriBuilder(request.RequestUri);
            
            uriBuilder.Path = uriBuilder.Path.EndsWith("/", StringComparison.Ordinal) ?
                uriBuilder.Path.Remove(uriBuilder.Path.Length - 1) :
                uriBuilder.Path = uriBuilder.Path + "/";

            return uriBuilder.Uri;
        }

        private static bool CanUriMatch(UriTemplateTable methodSpecificTable, Uri requestUri, out string operationName)
        {
            Fx.Assert(requestUri != null, "The 'requestUri' parameter should not be null.");

            operationName = null;

            if (methodSpecificTable != null)
            {
                UriTemplateMatch result = methodSpecificTable.MatchSingle(requestUri);
                if (result != null)
                {
                    operationName = result.Data as string;
                    Fx.Assert(operationName != null, "The 'operationName' variable should never be null.");
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<HttpMethod> RetrieveAllowedMethodsIfAny(HttpRequestMessage request)
        {
            HashSet<HttpMethod> allowedMethods = null;
            foreach (KeyValuePair<HttpMethod, UriTemplateTable> pair in this.methodSpecificTables)
            {
                if (pair.Key != request.Method && pair.Key != wildCardHttpMethod)
                {
                    UriTemplateTable table = pair.Value;

                    if (table.MatchSingle(request.RequestUri) != null)
                    {
                        if (allowedMethods == null)
                        {
                            allowedMethods = new HashSet<HttpMethod>();
                        }

                        if (!allowedMethods.Contains(pair.Key))
                        {
                            allowedMethods.Add(pair.Key);
                        }
                    }
                }
            }

            return allowedMethods;
        }

        /// <summary>
        ///  Class used to enforce that no two operations have the same UriTemplate and Method.
        /// </summary>
        private class OperationRoutingKey
        {
            private static UriTemplateEquivalenceComparer uriTemplateEquivalenceComparerInstance = new UriTemplateEquivalenceComparer();

            private HttpMethod method;
            private UriTemplate uriTemplate;

            internal OperationRoutingKey(UriTemplate uriTemplate, HttpMethod method)
            {
                this.uriTemplate = uriTemplate;
                this.method = method;
            }

            /// <summary>
            /// Override to do custom comparison
            /// </summary>
            /// <param name="obj">The object to compare.</param>
            /// <returns><c>true</c> if they are equal.</returns>
            public override bool Equals(object obj)
            {
                OperationRoutingKey other = obj as OperationRoutingKey;
                Fx.Assert(other != null, "The 'obj' parameter should always be an OperationRoutingKey.");

                return this.uriTemplate.IsEquivalentTo(other.uriTemplate) && this.method == other.method;
            }

            /// <summary>
            /// Override to do custom hash
            /// </summary>
            /// <returns>Custom hash code.</returns>
            public override int GetHashCode()
            {
                //// ALTERED_FOR_PORT
                //// return UriTemplateEquivalenceComparer.Instance.GetHashCode(this.uriTemplate);
                return uriTemplateEquivalenceComparerInstance.GetHashCode(this.uriTemplate);
            }
        }
    }
}
