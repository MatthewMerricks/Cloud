// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class ArtifactService
    {
        // Demonstrates how string versions of gridX and gridY are transformed to GridPosition
        // via the custom GridPositionProcessor.
        [WebGet(UriTemplate = "/Artifacts/{gridX},{gridY}")]
        public Artifact[] GetArtifacts(GridPosition gridPosition)
        {
            return new Artifact[]
            {
                new Artifact() { Description = "Atlatl", GridPosition = gridPosition, FoundBy = "Rebecca" },
                new Artifact() { Description = "Arrow head", GridPosition = gridPosition, FoundBy = "John" }
            };
        }

        // Demonstrates direct use of HttpRequestMessage and HttpResponseMessage
        // These are created by the HttpPipeline and made available to operations for direct access.
        [WebGet(UriTemplate = "/Artifacts/{name}")]
        public HttpResponseMessage GetArtifactsFoundBy(HttpRequestMessage request)
        {
            // Take the finder name directly from the URI (for demonstration purposes)
            string requestUri = request.RequestUri.ToString();
            string finderName = requestUri.Substring(requestUri.LastIndexOf('/') + 1);

            // This would be replaced by a query
            Artifact[] artifacts = new Artifact[]
            {
                new Artifact() { Description = "Atlatl", GridPosition = new GridPosition(5,6), FoundBy = finderName },
            };

            // Serialize the content back into the response directly
            DataContractSerializer serializer = new DataContractSerializer(
                                typeof(Artifact[]),
                                new Type[] { typeof(Artifact), typeof(GridPosition) });

            MemoryStream stream = new MemoryStream();
            serializer.WriteObject(stream, artifacts);
            stream.Flush();
            stream.Seek(0L, SeekOrigin.Begin);

            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
            response.Content = new StreamContent(stream);

            // Copy any custom request headers to the response to demonstrate this feature
            IEnumerable<string> headerValues = null;
            if (request.Headers.TryGetValues("Artifacts", out headerValues))
            {
                response.Headers.Add("Artifacts", headerValues);
            }

            return response;
        }
    }

    [DataContract]
    public class Artifact
    {
        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public GridPosition GridPosition { get; set; }

        [DataMember]
        public string FoundBy { get; set; }
    }

    [DataContract]
    public class GridPosition
    {
        public GridPosition() { }

        public GridPosition(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        [DataMember]
        public int X { get; set; }

        [DataMember]
        public int Y { get; set; }
    }
}
