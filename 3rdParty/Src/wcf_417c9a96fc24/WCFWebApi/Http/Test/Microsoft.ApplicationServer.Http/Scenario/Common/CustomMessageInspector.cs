// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using Microsoft.ApplicationServer.Http.Dispatcher;

    internal class CustomMessageInspector : HttpMessageInspector
    {
        protected override object OnAfterReceiveRequest(HttpRequestMessage request)
        {
            IEnumerable<string> throwAway;
            return request.Headers.TryGetValues("NamesOnly", out throwAway);
        }

        protected override void OnBeforeSendReply(HttpResponseMessage response, object correlationState)
        {
            bool namesOnly = (bool)correlationState;

            if (namesOnly && response.Content != null)
            {
                StringBuilder builder = new StringBuilder(); 
                string[] contentSplit = response.Content.ReadAsString().Split(new string[]{ Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string customerText in contentSplit)
                {
                    string[] customerSplitText = customerText.Split('=', ';');
                    builder.Append(customerSplitText[3].Trim() + ", ");
                }

                if (builder.Length > 2)
                {
                    builder.Length -= 2;
                }

                response.Content = new StringContent(builder.ToString());
            }
        }
    }
}
