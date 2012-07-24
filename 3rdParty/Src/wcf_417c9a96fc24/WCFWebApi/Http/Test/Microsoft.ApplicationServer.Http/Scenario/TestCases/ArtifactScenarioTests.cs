// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Threading;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.ApplicationServer.Common.Test.Services;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Xml.Serialization;


    /// <summary>
    /// Class that demonstrates using the Http Programming Model against an ArtifactService
    /// to interact with archeological dig data.   It demonstrates the use of a custom processor
    /// to create a strongly-typed object from Uri variables, asking for either xml or json from
    /// a single strongly-typed operation, using media ranges, etc.
    /// </summary>
    [TestClass]
    public class ArtifactScenarioTests
    {
        private static int portNumber = 2000;
        private const int timeout = 30 * 1000;

        [Ignore]
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(timeout)]
        [Description("Demonstrates a custom processor creating a strongly-typed object from URI variables")]
        public void ArtifactScenario_GetArtifactsFromXY()
        {
            string baseAddress = string.Format("http://localhost:{0}", GetNextPortNumber());

            using (HttpServiceHost host = CreateHttpServiceHost(typeof(ArtifactService), baseAddress))
            {
                host.Open();

                using (HttpClient client = new HttpClient())
                {
                    client.Channel = new WebRequestChannel();
                    using (HttpResponseMessage response = client.Get(baseAddress + "/Artifacts/5,6"))
                    {
                        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "status code not ok");
                        Artifact[] artifacts = DeserializeArtifacts(response);
                        Assert.AreEqual(2, artifacts.Length, "Expected 2 artifacts at pos 5,6");
                        foreach (Artifact artifact in artifacts)
                        {
                            Assert.AreEqual(5, artifact.GridPosition.X, "gridX should be 5");
                            Assert.AreEqual(6, artifact.GridPosition.Y, "gridY should be 5");
                        }
                    }
                }
            }
        }

        [Ignore]
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(timeout)]
        
        [Description("Demonstrates same strongly-typed operation from Uri but asks for json response")]
        public void ArtifactScenario_GetArtifactsFromXY_Json()
        {
            string baseAddress = string.Format("http://localhost:{0}", GetNextPortNumber());

            using (HttpServiceHost host = CreateHttpServiceHost(typeof(ArtifactService), baseAddress))
            {
                host.Open();

                using (HttpClient client = new HttpClient())
                {
                    client.Channel = new WebRequestChannel();
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, baseAddress + "/Artifacts/5,6");
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/json"));
                    using (HttpResponseMessage response = client.Send(request))
                    {
                        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "status code not ok");
                        Artifact[] artifacts = DeserializeArtifactsJson(response);
                        Assert.AreEqual(2, artifacts.Length, "Expected 2 artifacts at pos 5,6");
                        foreach (Artifact artifact in artifacts)
                        {
                            Assert.AreEqual(5, artifact.GridPosition.X, "gridX should be 5");
                            Assert.AreEqual(6, artifact.GridPosition.Y, "gridY should be 5");
                        }
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(timeout)]
        
        [Description("Same strongly-typed scenario, but demonstrates use of media range mapping to give json for text/*")]
        public void ArtifactScenario_GetArtifactsFromXY_Json_MediaRange()
        {
            string baseAddress = string.Format("http://localhost:{0}", GetNextPortNumber());

            MediaTypeHeaderValue range = new MediaTypeHeaderValue("text/*");
            MediaTypeHeaderValue mapsToMediaType = new MediaTypeHeaderValue("text/json");
            using (HttpServiceHost host = CreateHttpServiceHost(typeof(ArtifactService), baseAddress, range, mapsToMediaType))
            {
                HttpEndpoint endPoint = host.Description.Endpoints.OfType<HttpEndpoint>().Single();

                // Add a media range mapping to map text/* to text/json
                //endPoint.MediaTypeMappings.Add(new MediaRangeMapping("text/*", "text/json"));

                host.Open();

                using (HttpClient client = new HttpClient())
                {
                    client.Channel = new WebRequestChannel();
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, baseAddress + "/Artifacts/5,6");
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/*"));
                    using (HttpResponseMessage response = client.Send(request))
                    {
                        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "status code not ok");
                        Artifact[] artifacts = DeserializeArtifactsJson(response);
                        Assert.AreEqual(2, artifacts.Length, "Expected 2 artifacts at pos 5,6");
                        foreach (Artifact artifact in artifacts)
                        {
                            Assert.AreEqual(5, artifact.GridPosition.X, "gridX should be 5");
                            Assert.AreEqual(6, artifact.GridPosition.Y, "gridY should be 5");
                        }
                    }
                }
            }
        }


        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(timeout)]
        
        [Description("Demonstrates operation that directly manipulates HttpResponseMessage (see ArtifactService.GetArtifactsFoundBy)")]
        public void ArtifactScenario_GetArtifactsFoundBy()
        {
            string baseAddress = string.Format("http://localhost:{0}", GetNextPortNumber());

            using (HttpServiceHost host = CreateHttpServiceHost(typeof(ArtifactService), baseAddress))
            {
                host.Open();

                using (HttpClient client = new HttpClient())
                {
                    client.Channel = new WebRequestChannel();
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, baseAddress + "/Artifacts/Rebecca"))
                    {
                        // Put sample data in the request header to demonstrate how service can acccess it
                        request.Headers.Add("Artifacts", "SampleHeaderData");

                        using (HttpResponseMessage response = client.Send(request))
                        //client.Get(baseAddress + "/Artifacts/Rebecca"))
                        {
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "status code not ok");
                            Artifact[] artifacts = DeserializeArtifactsDataContract(response);
                            Assert.AreEqual(1, artifacts.Length, "Expected 1 artifact found by Rebecca");
                            Assert.AreEqual("Rebecca", artifacts[0].FoundBy, "Rebecca should have been the finder");

                            // Prove the request/response headers were used by the service
                            IEnumerable<string> headerValues = null;
                            bool hadHeader = response.Headers.TryGetValues("Artifacts", out headerValues);
                            Assert.IsTrue(hadHeader, "Failed to write responseHeaders");
                            Assert.IsTrue(headerValues.Contains("SampleHeaderData"), "Did not copy responseHeaders correctly");
                        }
                    }
                }
            }
        }


        private static HttpServiceHost CreateHttpServiceHost(Type serviceType, string baseAddress)
        {
            HttpServiceHost host = new HttpServiceHost(serviceType, new Uri(baseAddress));
            host.AddDefaultEndpoints();
            HttpEndpoint endpoint = host.Description.Endpoints.OfType<HttpEndpoint>().Single();
            endpoint.OperationHandlerFactory = new ArtifactHttpOperationHandlerFactory(new MediaTypeFormatter[0]);
            return host;
        }

        private static HttpServiceHost CreateHttpServiceHost(Type serviceType, string baseAddress, MediaTypeHeaderValue range, MediaTypeHeaderValue mediaType)
        {
            HttpServiceHost host = new HttpServiceHost(serviceType, new Uri(baseAddress));
            host.AddDefaultEndpoints();
            HttpEndpoint endpoint = host.Description.Endpoints.OfType<HttpEndpoint>().Single();
            MediaTypeFormatterCollection formatters = new MediaTypeFormatterCollection();
            formatters.JsonFormatter.AddMediaRangeMapping(range, mediaType);
            endpoint.OperationHandlerFactory = new ArtifactHttpOperationHandlerFactory(formatters);
            return host;
        }


        private static Artifact[] DeserializeArtifacts(HttpResponseMessage response)
        {
            string xmlContent = ResponseContent(response);
            XmlSerializer serializer = new XmlSerializer(
                                                    typeof(Artifact[]),
                                                    new Type[]
                                                    {
                                                        typeof(Artifact),
                                                        typeof(GridPosition)
                                                    });

            object readObject = null;
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContent)))
            {
                readObject = serializer.Deserialize(stream);
            }

            return readObject as Artifact[];
        }

        private static Artifact[] DeserializeArtifactsDataContract(HttpResponseMessage response)
        {
            string xmlContent = ResponseContent(response);
            DataContractSerializer serializer = new DataContractSerializer(
                                                    typeof(Artifact[]),
                                                    new Type[]
                                                    {
                                                        typeof(Artifact),
                                                        typeof(GridPosition)
                                                    });

            object readObject = null;
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContent)))
            {
                readObject = serializer.ReadObject(stream);
            }

            return readObject as Artifact[];
        }

        private static Artifact[] DeserializeArtifactsJson(HttpResponseMessage response)
        {
            string jsonContent = ResponseContent(response);
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(
                                                    typeof(Artifact[]),
                                                    new Type[]
                                                    {
                                                        typeof(Artifact),
                                                        typeof(GridPosition)
                                                    });

            object readObject = null;
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent)))
            {
                readObject = serializer.ReadObject(stream);
            }

            return readObject as Artifact[];
        }

        private static string ResponseContent(HttpResponseMessage response)
        {
            Assert.IsNotNull(response, "null httpResponseMessage");
            Assert.IsNotNull(response.Content, "response had no content");
            Stream contentStream = response.Content.ContentReadStream;
            Assert.IsNotNull(contentStream, "Stream is null");
            contentStream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(contentStream);
            string content = reader.ReadToEnd();
            return content;
        }

        private static int GetNextPortNumber()
        {
            return Interlocked.Increment(ref portNumber);
        }
    }
}
