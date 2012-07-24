// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Description;

    /// <summary>
    /// A <see cref="HttpOperationHandler"/> that matches an input <see cref="Uri"/> against a 
    /// given <see cref="UriTemplate"/> and outputs <see cref="UriTemplate"/> variable
    /// values as strings.
    /// </summary>
    public class UriTemplateHandler : HttpOperationHandler
    {
        /// <summary>
        /// Initializes a new instance of a <see cref="UriTemplateHandler"/> with the
        /// given <paramref name="baseAddress"/> and <paramref name="uriTemplate"/>.
        /// </summary>
        /// <param name="baseAddress">
        /// The base address to use when matching a <see cref="Uri"/> against the given <paramref name="uriTemplate"/>
        /// </param> 
        /// <param name="uriTemplate">
        /// The <see cref="UriTemplate"/> to match against input <see cref="Uri">Uris</see>.
        /// </param>
        public UriTemplateHandler(Uri baseAddress, UriTemplate uriTemplate)
        {
            if (baseAddress == null)
            {
                throw Fx.Exception.ArgumentNull("baseAddress");
            }

            if (uriTemplate == null)
            {
                throw Fx.Exception.ArgumentNull("uriTemplate");
            }

            this.BaseAddress = baseAddress;
            this.UriTemplate = uriTemplate;
        }

        /// <summary>
        /// Gets the base address to use when matching a <see cref="Uri"/> against the <paramref name="UriTemplateHttpOperationHandler.UriTemplate"/>.
        /// </summary>
        public Uri BaseAddress { get; private set; }

        /// <summary>
        /// Gets the <see cref="UriTemplate"/> to match against input <see cref="Uri"/> instances.
        /// </summary>
        public UriTemplate UriTemplate { get; private set; }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing the
        /// input values for this <see cref="UriTemplateHandler"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="UriTemplateHandler"/> always returns a single input of
        /// <see cref="HttpParameter.RequestMessage"/>.
        /// </remarks>
        /// <returns>A collection that consists of just the <see cref="HttpParameter.RequestMessage"/>.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return new HttpParameter[] { HttpParameter.RequestMessage };
        }

        /// <summary>
        /// Retrieves the collection of <see cref="HttpParameter"/> instances describing the
        /// output values of this <see cref="UriTemplateHandler"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="UriTemplateHandler"/> always returns output <see cref="HttpParameter"/> 
        /// instances in which the <see cref="HttpParameter.Name"/> is the <see cref="UriTemplateHandler.UriTemplate"/>
        /// variable and the <see cref="HttpParameter.Type"/> is of type <see cref="String"/>.
        /// </remarks>
        /// <returns>The collection of <see cref="HttpParameter"/> instances.</returns>
        protected override sealed IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            int numberOfVariables = this.UriTemplate.PathSegmentVariableNames.Count + this.UriTemplate.QueryValueVariableNames.Count;
            
            HttpParameter[] parameters = new HttpParameter[numberOfVariables];
            int i = 0;

            foreach (string name in this.UriTemplate.PathSegmentVariableNames)
            {
                parameters[i] = new HttpParameter(name, TypeHelper.StringType);
                i++;
            }

            foreach (string name in this.UriTemplate.QueryValueVariableNames)
            {
                parameters[i] = new HttpParameter(name, TypeHelper.StringType);
                i++;
            }

            return parameters;
        }

        /// <summary>
        /// Called to execute this <see cref="UriTemplateHandler"/>.
        /// </summary>
        /// <param name="input">
        /// The input values to handle, corresponding to the <see cref="HttpParameter"/> 
        /// returned by <see cref="OnGetInputParameters"/>
        /// </param>
        /// <returns>
        /// The <see cref="UriTemplate"/> variable values from matching the <see cref="UriTemplate"/>
        /// agains the request <see cref="Uri"/>.
        /// </returns>
        protected override sealed object[] OnHandle(object[] input)
        {
            Fx.Assert(input != null, "The 'input' parameter should not be null.");
            Fx.Assert(input.Length == 1, "There should be one element in the 'input' array");

            HttpRequestMessage requestMessage = input[0] as HttpRequestMessage;
            if (requestMessage == null)
            {
                throw Fx.Exception.ArgumentNull(HttpParameter.RequestMessage.Name);
            }

            Uri uri = requestMessage.RequestUri;
            int numberOfParameters = this.OutputParameters.Count;
            object[] output = new object[numberOfParameters];
            if (uri != null)
            {
                UriTemplateMatch match = this.UriTemplate.Match(this.BaseAddress, uri);

                if (match == null)
                {
                    throw Fx.Exception.AsError(
                        new InvalidOperationException(
                            SR.UriTemplateDoesNotMatchUri(
                                uri.ToString(),
                                this.UriTemplate.ToString())));
                }
                
                for (int i = 0; i < numberOfParameters; i++)
                {
                    output[i] = match.BoundVariables[i];
                }
            }

            return output;
        }
    }
}