// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Common;

    internal static class ClientContractDescriptionHelper
    {
        private static readonly Type httpRequestMessageType = typeof(HttpRequestMessage);
        private static readonly Type httpResponseMessageType = typeof(HttpResponseMessage);

        public static OperationDescription GetEquivalentOperationDescription(OperationDescription operation)
        {   
            Fx.Assert(operation != null, "OperationDescription cannnot be null");

            OperationDescription copy = CreateEmptyOperationDescription(operation);
            HttpOperationDescription httpDescription = copy.ToHttpOperationDescription();
            HttpOperationDescription originalHttpDescription = operation.ToHttpOperationDescription();
            UriTemplate template = originalHttpDescription.GetUriTemplate();
            List<string> templateVariables = new List<string>(template.PathSegmentVariableNames.Concat(template.QueryValueVariableNames));

            IEnumerable<HttpParameter> originalRequestBodyParameters = originalHttpDescription.InputParameters.Where(param => param.IsContentParameter);
            IEnumerable<HttpParameter> originalResponseBodyParameters = originalHttpDescription.OutputParameters.Where(param => param.IsContentParameter);
            IEnumerable<HttpParameter> originalUriTemplateParameters = originalHttpDescription.InputParameters.Where(
                (param) =>
                {
                    foreach (string templateVariable in templateVariables)
                    {
                        if (string.Equals(templateVariable, param.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    return false;
                });

            httpDescription.ReturnValue = originalHttpDescription.ReturnValue;

            // add UriTemplate parameters
            foreach (HttpParameter parameter in originalUriTemplateParameters)
            {
                httpDescription.InputParameters.Add(parameter);
            }

            // add body parameters
            foreach (HttpParameter parameter in originalRequestBodyParameters)
            {
                httpDescription.InputParameters.Add(parameter);
            }

            int index = httpDescription.InputParameters.Count;
            int requestBodyParametersCount = originalRequestBodyParameters.Count<HttpParameter>();
            int responseBodyParametersCount = originalResponseBodyParameters.Count<HttpParameter>();

            if (requestBodyParametersCount == 0 || responseBodyParametersCount == 0)
            {
                // Special case if any input parameter is HttpRequestMessage or HttpResponseMessage
                foreach (HttpParameter inputParameter in originalHttpDescription.InputParameters)
                {
                    if (requestBodyParametersCount == 0 && originalHttpDescription.GetHttpMethod() != HttpMethod.Get && httpRequestMessageType.IsAssignableFrom(inputParameter.Type))
                    {
                        // add the HttpRequestmessage as a body parameter of type Message
                        HttpParameter parameter = new HttpParameter(new MessagePartDescription(inputParameter.Name, string.Empty) { Type = typeof(Message), Index = index++ });
                        httpDescription.InputParameters.Add(parameter);
                    }

                    if (!operation.IsOneWay && responseBodyParametersCount == 0 && httpResponseMessageType.IsAssignableFrom(inputParameter.Type))
                    {
                        // add the HttpResponsemessage as a return value of type Message
                        httpDescription.ReturnValue = new HttpParameter(new MessagePartDescription(inputParameter.Name, string.Empty) { Type = typeof(Message) });
                    }
                }
            }

            foreach (HttpParameter parameter in originalResponseBodyParameters)
            {
                // cannot do a byRef comparison here
                if (parameter.Type != originalHttpDescription.ReturnValue.Type || !string.Equals(parameter.Name, originalHttpDescription.ReturnValue.Name, StringComparison.OrdinalIgnoreCase))
                {
                    httpDescription.OutputParameters.Add(parameter);
                }
            }

            if (responseBodyParametersCount == 0)
            {
                foreach (HttpParameter outputParameter in originalHttpDescription.OutputParameters)
                {
                    // special case HttpResponseMessage when it is set as an out parameter
                    if (httpResponseMessageType.IsAssignableFrom(outputParameter.Type))
                    {
                        httpDescription.ReturnValue = new HttpParameter(new MessagePartDescription(outputParameter.Name, string.Empty) { Type = typeof(Message) });
                    }
                }
            }

            if (templateVariables.Count > originalUriTemplateParameters.Count<HttpParameter>())
            {
                // this means that we have some UriTemplate variables that are not explicitly bound to an input parameter
                foreach (HttpParameter parameter in originalUriTemplateParameters)
                {
                    templateVariables.Remove(parameter.Name);
                }

                foreach (string variable in templateVariables)
                {
                    HttpParameter parameter = new HttpParameter(new MessagePartDescription(variable, operation.DeclaringContract.Namespace) { Type = typeof(string), Index = index++ });
                    httpDescription.InputParameters.Add(parameter);
                }
            }

            return httpDescription.ToOperationDescription();
        }

        private static OperationDescription CreateEmptyOperationDescription(OperationDescription other)
        {
            OperationDescription copy = ClientContractDescriptionHelper.GetCopyOfOperationDescription(other);
            HttpOperationDescription description = copy.ToHttpOperationDescription();

            // delete all parameters
            description.InputParameters.Clear();
            description.OutputParameters.Clear();

            return copy;
        }

        private static OperationDescription GetCopyOfOperationDescription(OperationDescription other)
        {
            OperationDescription operationDecription = new OperationDescription(other.Name, other.DeclaringContract)
            {
                BeginMethod = other.BeginMethod,
                EndMethod = other.EndMethod,
                IsInitiating = other.IsInitiating,
                IsTerminating = other.IsTerminating,
                ProtectionLevel = other.ProtectionLevel,
                SyncMethod = other.SyncMethod,
            };
            
            // copy Behaviors, Known Types, Faults
            foreach (IOperationBehavior behavior in other.Behaviors)
            {
                operationDecription.Behaviors.Add(behavior);
            }

            foreach (Type knownType in other.KnownTypes)
            {
                operationDecription.KnownTypes.Add(knownType);
            }

            foreach (FaultDescription fault in other.Faults)
            {
                operationDecription.Faults.Add(fault);
            }

            // copy the Messages from the original OperationDescription
            foreach (MessageDescription messageDescription in other.Messages)
            {
                MessageDescription newMessageDescription = new MessageDescription(messageDescription.Action, messageDescription.Direction);
                operationDecription.Messages.Add(newMessageDescription);
            }

            return operationDecription;
        }
    }
}
