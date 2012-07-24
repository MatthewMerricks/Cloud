// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.ApplicationServer.Http.Description;

    internal class CustomOperationSelector : HttpOperationSelector
    {
        public const string CatchAllOperationName = "CatchAll";

        private bool hasCatchAllOperation;
        private Dictionary<Tuple<string, string>, string> uriPathAndMethodToOperationMapping;

        public CustomOperationSelector(Uri baseAddress, IEnumerable<HttpOperationDescription> httpOperations)
        {
            if (baseAddress == null)
            {
                throw new ArgumentNullException("baseAddress");
            }

            if (httpOperations == null)
            {
                throw new ArgumentNullException("httpOperations");
            }

            this.uriPathAndMethodToOperationMapping = new Dictionary<Tuple<string,string>,string>();

            foreach (HttpOperationDescription httpOperation in httpOperations)
            {
                string operationName = httpOperation.Name;

                if (string.Equals(operationName, CatchAllOperationName, StringComparison.Ordinal))
                {
                    this.hasCatchAllOperation = true;
                }
                else
                {
                    this.uriPathAndMethodToOperationMapping.Add(
                        CreateMethodUriTuple(baseAddress, operationName),
                        operationName);
                }
            }
        }

        protected override string OnSelectOperation(HttpRequestMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (message.RequestUri == null)
            {
                throw new InvalidOperationException("The message must have a URI.");
            }

            if (message.Method == null)
            {
                throw new InvalidOperationException("The message must have a method.");
            }

            Tuple<string, string> methodPathTuple = new Tuple<string, string>(message.Method.ToString(), message.RequestUri.AbsolutePath);

            string operationName = null;
            if (!this.uriPathAndMethodToOperationMapping.TryGetValue(methodPathTuple, out operationName))
            {
                return (this.hasCatchAllOperation) ? CatchAllOperationName : string.Empty;
            }

            return operationName;
        }

        private static Tuple<string, string> CreateMethodUriTuple(Uri baseAddress, string operationName)
        {
            string[] operationNameSplit = operationName.SplitOnUpperCase().ToArray();
            string method = operationNameSplit[0].ToUpperInvariant();
            string path = string.Join("/", operationNameSplit.Skip(1));
            string absolutePath = baseAddress.AbsolutePath;
            if (path != string.Empty &&
                !absolutePath.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                absolutePath = absolutePath + "/";
            }
            return new Tuple<string, string>(method, absolutePath + path);
        }
    }

}
