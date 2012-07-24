// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Syndication;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Web;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Serialization;
    using Microsoft.ApplicationServer.Serialization.Json;

    internal class HelpPage
    {
        public const string OperationListHelpPageUriTemplate = "help";
        public const string OperationHelpPageUriTemplateBase = "help/operations/";
        public const string OperationHelpPageUriTemplate = "help/operations/{operation}";
        public const string HelpMethodName = "GetHelpPage";
        public const string HelpOperationMethodName = "GetOperationHelpPage";

        private static readonly Encoding utf8Encoding = new UTF8Encoding(true);
        private DateTime startupTime = DateTime.UtcNow;
        private Uri baseAddress;
        private Dictionary<string, OperationHelpInformation> operationInfoDictionary;
        private NameValueCache<string> operationPageCache;
        private NameValueCache<string> helpPageCache;

        public HelpPage(Uri baseAddress, string javascriptCallbackParameterName, ContractDescription description)
        {
            Fx.Assert(baseAddress != null, "The 'baseAddress' parameter should not be null.");

            this.baseAddress = baseAddress;

            this.operationInfoDictionary = new Dictionary<string, OperationHelpInformation>();
            this.operationPageCache = new NameValueCache<string>();
            this.helpPageCache = new NameValueCache<string>();

            foreach (OperationDescription od in description.Operations)
            {
                HttpOperationDescription httpOperationDescription = od.ToHttpOperationDescription();
                operationInfoDictionary.Add(
                    httpOperationDescription.Name,
                    new OperationHelpInformation(javascriptCallbackParameterName, WebMessageBodyStyle.Bare, httpOperationDescription));
            }
        }

        public static OperationDescription[] AddHelpOperations(ContractDescription contractDescription, DispatchRuntime dispatchRuntime)
        {
            Fx.Assert(contractDescription != null, "The 'contractDescription' parameter should not be null.");
            Fx.Assert(dispatchRuntime != null, "The 'dispatchRuntime' parameter should not be null.");

            Uri baseAddress = dispatchRuntime.EndpointDispatcher.EndpointAddress.Uri;
            HelpPage helpPage = new HelpPage(baseAddress, null, contractDescription);

            HttpOperationDescription helpPageOperation = new HttpOperationDescription(HelpPage.HelpMethodName, contractDescription);
            helpPageOperation.Behaviors.Add(new WebGetAttribute() { UriTemplate = HelpPage.OperationListHelpPageUriTemplate });
            helpPageOperation.InputParameters.Add(HttpParameter.RequestMessage);
            helpPageOperation.ReturnValue = HttpParameter.ResponseMessage;

            HttpOperationDescription operationhelpPageOperation = new HttpOperationDescription(HelpPage.HelpOperationMethodName, contractDescription);
            operationhelpPageOperation.Behaviors.Add(new WebGetAttribute() { UriTemplate = HelpPage.OperationHelpPageUriTemplate });
            operationhelpPageOperation.InputParameters.Add(HttpParameter.RequestMessage);
            operationhelpPageOperation.InputParameters.Add(new HttpParameter("operation", TypeHelper.StringType));
            operationhelpPageOperation.ReturnValue = HttpParameter.ResponseMessage;

            dispatchRuntime.Operations.Add(
                new DispatchOperation(dispatchRuntime, HelpPage.HelpMethodName, null, null) { Invoker = helpPage.GetHelpPageInvoker(true) });
            dispatchRuntime.Operations.Add(
                new DispatchOperation(dispatchRuntime, HelpPage.HelpOperationMethodName, null, null) { Invoker = helpPage.GetHelpPageInvoker(false) });

            return new OperationDescription[] { helpPageOperation.ToOperationDescription(), operationhelpPageOperation.ToOperationDescription() };
        }

        public static IEnumerable<KeyValuePair<UriTemplate, object>> GetOperationTemplatePairs()
        {
            return new KeyValuePair<UriTemplate, object>[]
            {
                new KeyValuePair<UriTemplate, object>(new UriTemplate(OperationListHelpPageUriTemplate), HelpMethodName),
                new KeyValuePair<UriTemplate, object>(new UriTemplate(OperationHelpPageUriTemplate), HelpOperationMethodName)
            };
        }

        public IOperationInvoker GetHelpPageInvoker(bool invokerForOperationListHelpPage)
        {
            return new HttpHelpOperationInvoker(this, invokerForOperationListHelpPage);
        }

        public void InvokeHelpPage(HttpResponseMessage response)
        {
            ApplyCaching();

            Uri baseUri = this.baseAddress.GetHostNormalizedUri(response.RequestMessage);

            string helpPage = this.helpPageCache.Lookup(baseUri.Authority);

            if (string.IsNullOrEmpty(helpPage))
            {
                helpPage = HelpHtmlPageBuilder.CreateHelpPage(baseUri, operationInfoDictionary.Values).ToString();
                if (HttpContext.Current == null)
                {
                    this.helpPageCache.AddOrUpdate(baseUri.Authority, helpPage);
                }
            }

            response.Content = new ActionOfStreamContent(
                stream =>
                {
                    byte[] preamble = utf8Encoding.GetPreamble();
                    stream.Write(preamble, 0, preamble.Length);
                    byte[] bytes = utf8Encoding.GetBytes(helpPage);
                    stream.Write(bytes, 0, bytes.Length);
                });

            response.Content.Headers.ContentType = MediaTypeConstants.HtmlMediaType;
        }

        public void InvokeOperationHelpPage(string operationName, HttpResponseMessage response)
        {
            ApplyCaching();

            Uri requestUri = response.RequestMessage.RequestUri.GetHostNormalizedUri(response.RequestMessage);
            string helpPage = this.operationPageCache.Lookup(requestUri.AbsoluteUri);

            if (string.IsNullOrEmpty(helpPage))
            {
                OperationHelpInformation operationInfo;
                if (this.operationInfoDictionary.TryGetValue(operationName, out operationInfo))
                {
                    Uri baseUri = this.baseAddress.GetHostNormalizedUri(response.RequestMessage);
                    helpPage = HelpHtmlPageBuilder.CreateOperationHelpPage(baseUri, operationInfo).ToString();
                    if (HttpContext.Current == null)
                    {
                        this.operationPageCache.AddOrUpdate(requestUri.AbsoluteUri, helpPage);
                    }
                }
                else
                {
                    throw Fx.Exception.AsError(new WebFaultException(HttpStatusCode.NotFound));
                }
            }

            response.Content = new ActionOfStreamContent(
                stream =>
                {
                    byte[] preamble = utf8Encoding.GetPreamble();
                    stream.Write(preamble, 0, preamble.Length);
                    byte[] bytes = utf8Encoding.GetBytes(helpPage);
                    stream.Write(bytes, 0, bytes.Length);
                });
            response.Content.Headers.ContentType = MediaTypeConstants.HtmlMediaType;
        }

        private void ApplyCaching()
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.Public);
                HttpContext.Current.Response.Cache.SetMaxAge(TimeSpan.MaxValue);
                HttpContext.Current.Response.Cache.AddValidationCallback(new HttpCacheValidateHandler(this.CacheValidationCallback), this.startupTime);
                HttpContext.Current.Response.Cache.SetValidUntilExpires(true);
            }
        }

        private void CacheValidationCallback(HttpContext context, object state, ref HttpValidationStatus result)
        {
            if (((DateTime)state) == this.startupTime)
            {
                result = HttpValidationStatus.Valid;
            }
            else
            {
                result = HttpValidationStatus.Invalid;
            }
        }

        private class NameValueCache<T>
        {
            // The NameValueCache implements a structure that uses a dictionary to map objects to
            // indices of an array of cache entries.  This allows us to store the cache entries in 
            // the order in which they were added to the cache, and yet still lookup any cache entry.
            // The eviction policy of the cache is to evict the least-recently-added cache entry.  
            // Using a pointer to the next available cache entry in the array, we can always be sure 
            // that the given entry is the oldest entry. 
            private Hashtable cache;
            private string[] currentKeys;
            private int nextAvailableCacheIndex;
            private object cachelock;
            internal const int maxNumberofEntriesInCache = 16;

            public NameValueCache()
                : this(maxNumberofEntriesInCache)
            {
            }

            public NameValueCache(int maxCacheEntries)
            {
                cache = new Hashtable();
                currentKeys = new string[maxCacheEntries];
                cachelock = new object();
            }

            public T Lookup(string key)
            {
                return (T)cache[key];
            }

            public void AddOrUpdate(string key, T value)
            {
                lock (cache)
                {
                    if (!cache.ContainsKey(key))
                    {
                        if (!String.IsNullOrEmpty(currentKeys[nextAvailableCacheIndex]))
                        {
                            cache.Remove(currentKeys[nextAvailableCacheIndex]);
                        }
                        currentKeys[nextAvailableCacheIndex] = key;
                        nextAvailableCacheIndex = ++nextAvailableCacheIndex % currentKeys.Length;
                    }
                    cache[key] = value;
                }
            }
        }

        private class OperationHelpInformation
        {
            HttpOperationDescription httpOperationDescription;
            MessageHelpInformation request;
            MessageHelpInformation response;
            string javascriptCallbackParameterName;
            WebMessageBodyStyle bodyStyle;

            internal OperationHelpInformation(string javascriptCallbackParameterName, WebMessageBodyStyle bodyStyle, HttpOperationDescription httpOperationDescription)
            {
                this.httpOperationDescription = httpOperationDescription;
                this.javascriptCallbackParameterName = javascriptCallbackParameterName;
                this.bodyStyle = bodyStyle;
            }

            public string Name
            {
                get
                {
                    return httpOperationDescription.Name;
                }
            }

            public string UriTemplate
            {
                get
                {
                    return httpOperationDescription.GetUriTemplate().ToString();
                }
            }

            public string Method
            {
                get
                {
                    return httpOperationDescription.GetHttpMethod().Method;
                }
            }

            public string Description
            {
                get
                {
                    return httpOperationDescription.GetDescription();
                }
            }

            public string JavascriptCallbackParameterName
            {
                get
                {
                    if (this.Response.SupportsJson && string.Equals(this.Method, HttpMethod.Get.ToString()))
                    {
                        return this.javascriptCallbackParameterName;
                    }
                    return null;
                }
            }

            public WebMessageBodyStyle BodyStyle
            {
                get
                {
                    return this.bodyStyle;
                }
            }

            public MessageHelpInformation Request
            {
                get
                {
                    if (this.request == null)
                    {
                        this.request = new MessageHelpInformation(httpOperationDescription, true, GetRequestBodyType(httpOperationDescription, this.UriTemplate),
                            this.BodyStyle == WebMessageBodyStyle.WrappedRequest || this.BodyStyle == WebMessageBodyStyle.Wrapped);
                    }
                    return this.request;
                }
            }

            public MessageHelpInformation Response
            {
                get
                {
                    if (this.response == null)
                    {
                        this.response = new MessageHelpInformation(httpOperationDescription, false, GetResponseBodyType(httpOperationDescription),
                            this.BodyStyle == WebMessageBodyStyle.WrappedResponse || this.BodyStyle == WebMessageBodyStyle.Wrapped);
                    }
                    return this.response;
                }
            }

            public static Type GetResponseBodyType(HttpOperationDescription description)
            {
                if (description.OutputParameters.Count > 0)
                {
                    // If it is more than 0 the response is wrapped and not supported
                    return null;
                }

                HttpParameter returnDescription = description.ReturnValue;

                return returnDescription == null ? null : returnDescription.Type;
            }

            public Type GetRequestBodyType(HttpOperationDescription description, string uriTemplate)
            {
                if (description.Behaviors.Contains(typeof(WebGetAttribute)))
                {
                    return typeof(void);
                }

                UriTemplate template = new UriTemplate(uriTemplate);
                IEnumerable<HttpParameter> parameterDescriptions =
                    from parameterDescription in description.InputParameters
                    where !template.PathSegmentVariableNames.Contains(parameterDescription.Name.ToUpperInvariant()) && !template.QueryValueVariableNames.Contains(parameterDescription.Name.ToUpperInvariant())
                    select parameterDescription;

                HttpParameter[] matchingParameters = parameterDescriptions.ToArray();

                // No match == void
                // One match == the body type
                // Multiple matches == the request is wrapped and not supported
                return matchingParameters.Length == 0
                        ? typeof(void)
                        : (matchingParameters.Length == 1)
                            ? matchingParameters[0].Type
                            : null;
            }
        }

        private class MessageHelpInformation
        {
            private static readonly Type typeOfIQueryable = typeof(IQueryable);
            private static readonly Type typeOfIQueryableGeneric = typeof(IQueryable<>);
            private static readonly Type typeOfIEnumerable = typeof(IEnumerable);
            private static readonly Type typeOfIEnumerableGeneric = typeof(IEnumerable<>);

            public string BodyDescription { get; private set; }
            public string FormatString { get; private set; }
            public Type Type { get; private set; }
            public bool SupportsJson { get; private set; }
            public XmlSchemaSet SchemaSet { get; private set; }
            public XmlSchema Schema { get; private set; }
            public XElement XmlExample { get; private set; }
            public XElement JsonExample { get; private set; }

            internal MessageHelpInformation(HttpOperationDescription description, bool isRequest, Type type, bool wrapped)
            {
                this.Type = type;
                this.SupportsJson = true;
                string direction = isRequest ? Http.SR.HelpPageRequest : Http.SR.HelpPageResponse;

                if (wrapped && !typeof(void).Equals(type))
                {
                    this.BodyDescription = Http.SR.HelpPageBodyIsWrapped(direction);
                    this.FormatString = Http.SR.HelpPageUnknown;
                }
                else if (typeof(void).Equals(type))
                {
                    this.BodyDescription = Http.SR.HelpPageBodyIsEmpty(direction);
                    this.FormatString = Http.SR.HelpPageNA;
                }
                else if (typeof(Message).IsAssignableFrom(type))
                {
                    this.BodyDescription = Http.SR.HelpPageIsMessage(direction);
                    this.FormatString = Http.SR.HelpPageUnknown;
                }
                else if (typeof(Stream).IsAssignableFrom(type))
                {
                    this.BodyDescription = Http.SR.HelpPageIsStream(direction);
                    this.FormatString = Http.SR.HelpPageUnknown;
                }
                else if (typeof(Atom10FeedFormatter).IsAssignableFrom(type))
                {
                    this.BodyDescription = Http.SR.HelpPageIsAtom10Feed(direction);
                    this.FormatString = WebMessageFormat.Xml.ToString();
                }
                else if (typeof(Atom10ItemFormatter).IsAssignableFrom(type))
                {
                    this.BodyDescription = Http.SR.HelpPageIsAtom10Entry(direction);
                    this.FormatString = WebMessageFormat.Xml.ToString();
                }
                else if (typeof(AtomPub10ServiceDocumentFormatter).IsAssignableFrom(type))
                {
                    this.BodyDescription = Http.SR.HelpPageIsAtomPubServiceDocument(direction);
                    this.FormatString = WebMessageFormat.Xml.ToString();
                }
                else if (typeof(AtomPub10CategoriesDocumentFormatter).IsAssignableFrom(type))
                {
                    this.BodyDescription = Http.SR.HelpPageIsAtomPubCategoriesDocument(direction);
                    this.FormatString = WebMessageFormat.Xml.ToString();
                }
                else if (typeof(Rss20FeedFormatter).IsAssignableFrom(type))
                {
                    this.BodyDescription = Http.SR.HelpPageIsRSS20Feed(direction);
                    this.FormatString = WebMessageFormat.Xml.ToString();
                }
                else if (typeof(SyndicationFeedFormatter).IsAssignableFrom(type))
                {
                    this.BodyDescription = Http.SR.HelpPageIsSyndication(direction);
                    this.FormatString = WebMessageFormat.Xml.ToString();
                }
                else if (typeof(XElement).IsAssignableFrom(type) || typeof(XmlElement).IsAssignableFrom(type))
                {
                    this.BodyDescription = Http.SR.HelpPageIsXML(direction);
                    this.FormatString = WebMessageFormat.Xml.ToString();
                }
                else
                {
                    try
                    {
                        bool usesXmlSerializer = description.Behaviors.Contains(typeof(XmlSerializerOperationBehavior));
                        XmlQualifiedName name;
                        this.SchemaSet = new XmlSchemaSet();
                        IDictionary<XmlQualifiedName, Type> knownTypes = new Dictionary<XmlQualifiedName, Type>();
                        if (usesXmlSerializer)
                        {
                            XmlReflectionImporter importer = new XmlReflectionImporter();
                            XmlTypeMapping typeMapping = importer.ImportTypeMapping(this.Type);
                            name = new XmlQualifiedName(typeMapping.ElementName, typeMapping.Namespace);
                            XmlSchemas schemas = new XmlSchemas();
                            XmlSchemaExporter exporter = new XmlSchemaExporter(schemas);
                            exporter.ExportTypeMapping(typeMapping);
                            foreach (XmlSchema schema in schemas)
                            {
                                this.SchemaSet.Add(schema);
                            }
                        }
                        else
                        {
                            XsdDataContractExporter exporter = new XsdDataContractExporter();
                            List<Type> listTypes = new List<Type>(description.KnownTypes);
                            bool isQueryable;
                            Type dataContractType = GetSubstituteDataContractType(this.Type, out isQueryable);

                            listTypes.Add(dataContractType);
                            exporter.Export(listTypes);
                            if (!exporter.CanExport(dataContractType))
                            {
                                this.BodyDescription = Http.SR.HelpPageCouldNotGenerateSchema;
                                this.FormatString = Http.SR.HelpPageUnknown;
                                return;
                            }
                            name = exporter.GetRootElementName(dataContractType);
                            DataContract typeDataContract = DataContract.GetDataContract(dataContractType);
                            if (typeDataContract.KnownDataContracts != null)
                            {
                                foreach (XmlQualifiedName dataContractName in typeDataContract.KnownDataContracts.Keys)
                                {
                                    knownTypes.Add(dataContractName, typeDataContract.KnownDataContracts[dataContractName].UnderlyingType);
                                }
                            }
                            foreach (Type knownType in description.KnownTypes)
                            {
                                XmlQualifiedName knownTypeName = exporter.GetSchemaTypeName(knownType);
                                if (!knownTypes.ContainsKey(knownTypeName))
                                {
                                    knownTypes.Add(knownTypeName, knownType);
                                }
                            }

                            foreach (XmlSchema schema in exporter.Schemas.Schemas())
                            {
                                this.SchemaSet.Add(schema);
                            }
                        }
                        this.SchemaSet.Compile();

                        XmlWriterSettings settings = new XmlWriterSettings
                        {
                            CloseOutput = false,
                            Indent = true,
                        };

                        if (this.SupportsJson)
                        {
                            XDocument exampleDocument = new XDocument();
                            using (XmlWriter writer = XmlWriter.Create(exampleDocument.CreateWriter(), settings))
                            {
                                HelpExampleGenerator.GenerateJsonSample(this.SchemaSet, name, writer, knownTypes);
                            }
                            this.JsonExample = exampleDocument.Root;
                        }

                        if (name.Namespace != "http://schemas.microsoft.com/2003/10/Serialization/")
                        {
                            foreach (XmlSchema schema in this.SchemaSet.Schemas(name.Namespace))
                            {
                                this.Schema = schema;

                            }
                        }

                        XDocument XmlExampleDocument = new XDocument();
                        using (XmlWriter writer = XmlWriter.Create(XmlExampleDocument.CreateWriter(), settings))
                        {
                            HelpExampleGenerator.GenerateXmlSample(this.SchemaSet, name, writer);
                        }
                        this.XmlExample = XmlExampleDocument.Root;

                    }
                    catch (Exception e)
                    {
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }
                        this.BodyDescription = Http.SR.HelpPageCouldNotGenerateSchema;
                        this.FormatString = Http.SR.HelpPageUnknown;
                        this.Schema = null;
                        this.JsonExample = null;
                        this.XmlExample = null;
                    }
                }
            }

            private static Type GetSubstituteDataContractType(Type type, out bool isQueryable)
            {
                if (type == typeOfIQueryable)
                {
                    isQueryable = true;
                    return typeOfIEnumerable;
                }

                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeOfIQueryableGeneric)
                {
                    isQueryable = true;
                    return typeOfIEnumerableGeneric.MakeGenericType(type.GetGenericArguments());
                }

                isQueryable = false;
                return type;
            }
        }

        private class HelpHtmlPageBuilder : HtmlPageBuilder
        {
            internal const string HelpOperationPageUrl = "help/operations/{0}";
          
            public static XDocument CreateHelpPage(Uri baseUri, IEnumerable<OperationHelpInformation> operations)
            {
                XDocument document = CreateBaseDocument(Http.SR.HelpPageOperationsAt(baseUri));
                XElement table = new XElement(HtmlTableElementName,
                        new XElement(HtmlTrElementName,
                            new XElement(HtmlThElementName, Http.SR.HelpPageUri),
                            new XElement(HtmlThElementName, Http.SR.HelpPageMethod),
                            new XElement(HtmlThElementName, Http.SR.HelpPageDescription)));

                string lastOperation = null;
                XElement firstTr = null;
                int rowspan = 0;
                foreach (OperationHelpInformation operationHelpInfo in operations.OrderBy(o => FilterQueryVariables(o.UriTemplate)))
                {
                    string operationUri = FilterQueryVariables(operationHelpInfo.UriTemplate);
                    string description = operationHelpInfo.Description;
                    if (String.IsNullOrEmpty(description))
                    {
                        description = Http.SR.HelpPageDefaultDescription(BuildFullUriTemplate(baseUri, operationHelpInfo.UriTemplate));
                    }
                    XElement tr = new XElement(HtmlTrElementName,
                        new XElement(HtmlTdElementName, new XAttribute(HtmlTitleAttributeName, BuildFullUriTemplate(baseUri, operationHelpInfo.UriTemplate)),
                            new XElement(HtmlAElementName,
                                new XAttribute(HtmlRelAttributeName, HtmlOperationClass),
                                new XAttribute(HtmlHrefAttributeName, String.Format(CultureInfo.InvariantCulture, HelpOperationPageUrl, operationHelpInfo.Name)), operationHelpInfo.Method)),
                        new XElement(HtmlTdElementName, description));
                    table.Add(tr);
                    if (operationUri != lastOperation)
                    {
                        XElement td = new XElement(HtmlTdElementName, operationUri == lastOperation ? String.Empty : operationUri);
                        tr.AddFirst(td);
                        if (firstTr != null && rowspan > 1)
                        {
                            firstTr.Descendants(HtmlTdElementName).First().Add(new XAttribute(HtmlRowspanAttributeName, rowspan));
                        }
                        firstTr = tr;
                        rowspan = 0;
                        lastOperation = operationUri;
                    }
                    ++rowspan;
                }
                if (firstTr != null && rowspan > 1)
                {
                    firstTr.Descendants(HtmlTdElementName).First().Add(new XAttribute(HtmlRowspanAttributeName, rowspan));
                }
                document.Descendants(HtmlBodyElementName).First().Add(new XElement(HtmlDivElementName, new XAttribute(HtmlIdAttributeName, HtmlContentClass),
                    new XElement(HtmlPElementName, new XAttribute(HtmlClassAttributeName, HtmlHeading1Class), Http.SR.HelpPageOperationsAt(baseUri)),
                    new XElement(HtmlPElementName, Http.SR.HelpPageStaticText),
                    table));
                return document;
            }

            public static XDocument CreateOperationHelpPage(Uri baseUri, OperationHelpInformation operationInfo)
            {
                XDocument document = CreateBaseDocument(Http.SR.HelpPageReferenceFor(BuildFullUriTemplate(baseUri, operationInfo.UriTemplate)));
                XElement table = new XElement(HtmlTableElementName,
                    new XElement(HtmlTrElementName,
                        new XElement(HtmlThElementName, Http.SR.HelpPageMessageDirection),
                        new XElement(HtmlThElementName, Http.SR.HelpPageFormat),
                        new XElement(HtmlThElementName, Http.SR.HelpPageBody)));

                RenderMessageInformation(table, operationInfo, true);
                RenderMessageInformation(table, operationInfo, false);

                XElement div = new XElement(HtmlDivElementName, new XAttribute(HtmlIdAttributeName, HtmlContentClass),
                    new XElement(HtmlPElementName, new XAttribute(HtmlClassAttributeName, HtmlHeading1Class), Http.SR.HelpPageReferenceFor(BuildFullUriTemplate(baseUri, operationInfo.UriTemplate))),
                    new XElement(HtmlPElementName, operationInfo.Description),
                    XElement.Parse(Http.SR.HelpPageOperationUri(HttpUtility.HtmlEncode(BuildFullUriTemplate(baseUri, operationInfo.UriTemplate)))),
                    XElement.Parse(Http.SR.HelpPageOperationMethod(HttpUtility.HtmlEncode(operationInfo.Method))));
                if (!String.IsNullOrEmpty(operationInfo.JavascriptCallbackParameterName))
                {
                    div.Add(XElement.Parse(Http.SR.HelpPageCallbackText(HttpUtility.HtmlEncode(operationInfo.JavascriptCallbackParameterName))), table);
                }
                else
                {
                    div.Add(table);
                }
                document.Descendants(HtmlBodyElementName).First().Add(div);

                CreateOperationSamples(document.Descendants(HtmlDivElementName).First(), operationInfo);

                return document;
            }

            private static string FilterQueryVariables(string uriTemplate)
            {
                int variablesIndex = uriTemplate.IndexOf('?');
                if (variablesIndex > 0)
                {
                    return uriTemplate.Substring(0, variablesIndex);
                }
                return uriTemplate;
            }

            private static void RenderMessageInformation(XElement table, OperationHelpInformation operationInfo, bool isRequest)
            {
                MessageHelpInformation info = isRequest ? operationInfo.Request : operationInfo.Response;
                string direction = isRequest ? Http.SR.HelpPageRequest : Http.SR.HelpPageResponse;
                string nonLocalizedDirection = isRequest ? HtmlRequestXmlId : HtmlResponseXmlId;

                if (info.BodyDescription != null)
                {
                    table.Add(new XElement(HtmlTrElementName,
                        new XElement(HtmlTdElementName, direction),
                        new XElement(HtmlTdElementName, info.FormatString),
                        new XElement(HtmlTdElementName, info.BodyDescription)));
                }
                else
                {
                    if (info.XmlExample != null || info.Schema != null)
                    {
                        XElement contentTd;
                        table.Add(new XElement(HtmlTrElementName,
                            new XElement(HtmlTdElementName, direction),
                            new XElement(HtmlTdElementName, "Xml"),
                            contentTd = new XElement(HtmlTdElementName)));

                        if (info.XmlExample != null)
                        {
                            contentTd.Add(new XElement(HtmlAElementName, new XAttribute(HtmlHrefAttributeName, "#" + (isRequest ? HtmlRequestXmlId : HtmlResponseXmlId)), Http.SR.HelpPageExample));
                            if (info.Schema != null)
                            {
                                contentTd.Add(",");
                            }
                        }
                        if (info.Schema != null)
                        {
                            contentTd.Add(new XElement(HtmlAElementName, new XAttribute(HtmlHrefAttributeName, "#" + (isRequest ? HtmlRequestSchemaId : HtmlResponseSchemaId)), Http.SR.HelpPageSchema));
                        }
                    }
                    if (info.JsonExample != null)
                    {
                        table.Add(new XElement(HtmlTrElementName,
                            new XElement(HtmlTdElementName, direction),
                            new XElement(HtmlTdElementName, "Json"),
                            new XElement(HtmlTdElementName,
                                new XElement(HtmlAElementName, new XAttribute(HtmlHrefAttributeName, "#" + (isRequest ? HtmlRequestJsonId : HtmlResponseJsonId)), Http.SR.HelpPageExample))));
                    }
                }
            }

            private static void CreateOperationSamples(XElement element, OperationHelpInformation operationInfo)
            {
                if (operationInfo.Request.XmlExample != null)
                {
                    element.Add(GenerateSampleXml(operationInfo.Request.XmlExample, Http.SR.HelpPageXmlRequest, HtmlRequestXmlId));
                }
                if (operationInfo.Request.JsonExample != null)
                {
                    element.Add(AddSampleJson(operationInfo.Request.JsonExample, Http.SR.HelpPageJsonRequest, HtmlRequestJsonId));
                }
                if (operationInfo.Response.XmlExample != null)
                {
                    element.Add(GenerateSampleXml(operationInfo.Response.XmlExample, Http.SR.HelpPageXmlResponse, HtmlResponseXmlId));
                }
                if (operationInfo.Response.JsonExample != null)
                {
                    element.Add(AddSampleJson(operationInfo.Response.JsonExample, Http.SR.HelpPageJsonResponse, HtmlResponseJsonId));
                }

                if (operationInfo.Request.Schema != null)
                {
                    element.Add(GenerateSampleXml(XmlSchemaToXElement(operationInfo.Request.Schema), Http.SR.HelpPageRequestSchema, HtmlRequestSchemaId));
                    int count = 0;
                    foreach (XmlSchema schema in operationInfo.Request.SchemaSet.Schemas())
                    {
                        if (schema.TargetNamespace != operationInfo.Request.Schema.TargetNamespace)
                        {
                            element.Add(GenerateSampleXml(XmlSchemaToXElement(schema), ++count == 1 ? Http.SR.HelpPageAdditionalRequestSchema : null, HtmlRequestSchemaId));
                        }
                    }
                }
                if (operationInfo.Response.Schema != null)
                {
                    element.Add(GenerateSampleXml(XmlSchemaToXElement(operationInfo.Response.Schema), Http.SR.HelpPageResponseSchema, HtmlResponseSchemaId));
                    int count = 0;
                    foreach (XmlSchema schema in operationInfo.Response.SchemaSet.Schemas())
                    {
                        if (schema.TargetNamespace != operationInfo.Response.Schema.TargetNamespace)
                        {
                            element.Add(GenerateSampleXml(XmlSchemaToXElement(schema), ++count == 1 ? Http.SR.HelpPageAdditionalResponseSchema : null, HtmlResponseSchemaId));
                        }
                    }
                }
            }

            private static XElement XmlSchemaToXElement(XmlSchema schema)
            {
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    CloseOutput = false,
                    Indent = true,
                };

                XDocument schemaDocument = new XDocument();

                using (XmlWriter writer = XmlWriter.Create(schemaDocument.CreateWriter(), settings))
                {
                    schema.Write(writer);
                }
                return schemaDocument.Root;
            }

            private static XElement AddSample(object content, string title, string label)
            {
                if (String.IsNullOrEmpty(title))
                {
                    return new XElement(HtmlPElementName,
                        new XElement(HtmlPreElementName, new XAttribute(HtmlClassAttributeName, label), content));
                }
                else
                {
                    return new XElement(HtmlPElementName,
                        new XElement(HtmlAElementName, new XAttribute(HtmlNameAttributeName, "#" + label), title),
                        new XElement(HtmlPreElementName, new XAttribute(HtmlClassAttributeName, label), content));
                }
            }

            private static XElement GenerateSampleXml(XElement content, string title, string label)
            {
                StringBuilder sample = new StringBuilder();
                using (XmlWriter writer = XmlTextWriter.Create(sample, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                {
                    content.WriteTo(writer);
                }
                return AddSample(sample.ToString(), title, label);
            }

            private static XElement AddSampleJson(XElement content, string title, string label)
            {
                StringBuilder sample = new StringBuilder();
                using (MemoryStream stream = new MemoryStream())
                {
                    using (XmlDictionaryWriter writer =
                        JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.Unicode, false))
                    {
                        content.WriteTo(writer);
                    }

                    stream.Position = 0;
                    sample.Append(new StreamReader(stream, Encoding.Unicode).ReadToEnd());
                }
                int depth = 0;
                bool inString = false;
                for (int i = 0; i < sample.Length; ++i)
                {
                    if (sample[i] == '"')
                    {
                        inString = !inString;
                    }
                    else if (sample[i] == '{')
                    {
                        sample.Insert(i + 1, "\n" + new String('\t', ++depth));
                        i += depth + 1;
                    }
                    else if (sample[i] == ',' && !inString)
                    {
                        sample.Insert(i + 1, "\n" + new String('\t', depth));
                    }
                    else if (sample[i] == '}' && depth > 0)
                    {
                        sample.Insert(i, "\n" + new String('\t', --depth));
                        i += depth + 1;
                    }
                }
                return AddSample(sample.ToString(), title, label);
            }

            private static string BuildFullUriTemplate(Uri baseUri, string uriTemplate)
            {
                UriTemplate template = new UriTemplate(uriTemplate);
                Uri result = template.BindByPosition(baseUri, template.PathSegmentVariableNames.Concat(template.QueryValueVariableNames).Select(name => "{" + name + "}").ToArray());
                return result.ToString();
            }
        }

        private class HelpExampleGenerator
        {
            private const int MaxDepthLevel = 256;
            private const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";
            private const string XmlNamespacePrefix = "xmlns";
            private const string XmlSchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
            private const string XmlSchemaInstanceNil = "nil";
            private const string XmlSchemaInstanceType = "type";

            private static Dictionary<Type, Action<XmlSchemaObject, HelpExampleGeneratorContext>> XmlObjectHandler = new Dictionary<Type, Action<XmlSchemaObject, HelpExampleGeneratorContext>>
            {                
                { typeof(XmlSchemaComplexContent), ContentHandler },
                { typeof(XmlSchemaSimpleContent), ContentHandler },
                { typeof(XmlSchemaSimpleTypeRestriction), SimpleTypeRestrictionHandler },                
                { typeof(XmlSchemaChoice), ChoiceHandler },
                // Nothing to do, inheritance is resolved by Schema compilation process                
                { typeof(XmlSchemaComplexContentExtension), EmptyHandler },
                { typeof(XmlSchemaSimpleContentExtension), EmptyHandler },
                // No need to generate XML for these objects
                { typeof(XmlSchemaAny), EmptyHandler },
                { typeof(XmlSchemaAnyAttribute), EmptyHandler },
                { typeof(XmlSchemaAnnotated), EmptyHandler },
                { typeof(XmlSchema), EmptyHandler },
                // The following schema objects are not handled            
                { typeof(XmlSchemaAttributeGroup), ErrorHandler },
                { typeof(XmlSchemaAttributeGroupRef), ErrorHandler },
                { typeof(XmlSchemaComplexContentRestriction), ErrorHandler },
                { typeof(XmlSchemaSimpleContentRestriction), ErrorHandler },
                // Enumerations are supported by the GenerateContentForSimpleType
                { typeof(XmlSchemaEnumerationFacet), EmptyHandler },
                { typeof(XmlSchemaMaxExclusiveFacet), ErrorHandler },
                { typeof(XmlSchemaMaxInclusiveFacet), ErrorHandler },
                { typeof(XmlSchemaMinExclusiveFacet), ErrorHandler },
                { typeof(XmlSchemaMinInclusiveFacet), ErrorHandler },
                { typeof(XmlSchemaNumericFacet), ErrorHandler },
                { typeof(XmlSchemaFractionDigitsFacet), ErrorHandler },
                { typeof(XmlSchemaLengthFacet), ErrorHandler },
                { typeof(XmlSchemaMaxLengthFacet), ErrorHandler },
                { typeof(XmlSchemaMinLengthFacet), ErrorHandler },
                { typeof(XmlSchemaTotalDigitsFacet), ErrorHandler },
                { typeof(XmlSchemaPatternFacet), ErrorHandler },
                { typeof(XmlSchemaWhiteSpaceFacet), ErrorHandler },
                { typeof(XmlSchemaGroup), ErrorHandler },
                { typeof(XmlSchemaIdentityConstraint), ErrorHandler },
                { typeof(XmlSchemaKey), ErrorHandler },
                { typeof(XmlSchemaKeyref), ErrorHandler },
                { typeof(XmlSchemaUnique), ErrorHandler },
                { typeof(XmlSchemaNotation), ErrorHandler },
                { typeof(XmlSchemaAll), ErrorHandler },
                { typeof(XmlSchemaGroupRef), ErrorHandler },
                { typeof(XmlSchemaSimpleTypeUnion), ErrorHandler },
                { typeof(XmlSchemaSimpleTypeList), XmlSimpleTypeListHandler },
                { typeof(XmlSchemaXPath), ErrorHandler },
                { typeof(XmlSchemaAttribute), XmlAttributeHandler },
                { typeof(XmlSchemaElement), XmlElementHandler },
                { typeof(XmlSchemaComplexType), XmlComplexTypeHandler },
                { typeof(XmlSchemaSequence), XmlSequenceHandler },
                { typeof(XmlSchemaSimpleType), XmlSimpleTypeHandler },
            };

            private static Dictionary<Type, Action<XmlSchemaObject, HelpExampleGeneratorContext>> JsonObjectHandler = new Dictionary<Type, Action<XmlSchemaObject, HelpExampleGeneratorContext>>
            {                
                { typeof(XmlSchemaComplexContent), ContentHandler },
                { typeof(XmlSchemaSimpleContent), ContentHandler },
                { typeof(XmlSchemaSimpleTypeRestriction), SimpleTypeRestrictionHandler },                
                { typeof(XmlSchemaChoice), ChoiceHandler },
                // Nothing to do, inheritance is resolved by Schema compilation process                
                { typeof(XmlSchemaComplexContentExtension), EmptyHandler },
                { typeof(XmlSchemaSimpleContentExtension), EmptyHandler },
                // No need to generate XML for these objects
                { typeof(XmlSchemaAny), EmptyHandler },
                { typeof(XmlSchemaAnyAttribute), EmptyHandler },
                { typeof(XmlSchemaAnnotated), EmptyHandler },
                { typeof(XmlSchema), EmptyHandler },
                // The following schema objects are not handled            
                { typeof(XmlSchemaAttributeGroup), ErrorHandler },
                { typeof(XmlSchemaAttributeGroupRef), ErrorHandler },
                { typeof(XmlSchemaComplexContentRestriction), ErrorHandler },
                { typeof(XmlSchemaSimpleContentRestriction), ErrorHandler },
                // Enumerations are supported by the GenerateContentForSimpleType
                { typeof(XmlSchemaEnumerationFacet), EmptyHandler },
                { typeof(XmlSchemaMaxExclusiveFacet), ErrorHandler },
                { typeof(XmlSchemaMaxInclusiveFacet), ErrorHandler },
                { typeof(XmlSchemaMinExclusiveFacet), ErrorHandler },
                { typeof(XmlSchemaMinInclusiveFacet), ErrorHandler },
                { typeof(XmlSchemaNumericFacet), ErrorHandler },
                { typeof(XmlSchemaFractionDigitsFacet), ErrorHandler },
                { typeof(XmlSchemaLengthFacet), ErrorHandler },
                { typeof(XmlSchemaMaxLengthFacet), ErrorHandler },
                { typeof(XmlSchemaMinLengthFacet), ErrorHandler },
                { typeof(XmlSchemaTotalDigitsFacet), ErrorHandler },
                { typeof(XmlSchemaPatternFacet), ErrorHandler },
                { typeof(XmlSchemaWhiteSpaceFacet), ErrorHandler },
                { typeof(XmlSchemaGroup), ErrorHandler },
                { typeof(XmlSchemaIdentityConstraint), ErrorHandler },
                { typeof(XmlSchemaKey), ErrorHandler },
                { typeof(XmlSchemaKeyref), ErrorHandler },
                { typeof(XmlSchemaUnique), ErrorHandler },
                { typeof(XmlSchemaNotation), ErrorHandler },
                { typeof(XmlSchemaAll), ErrorHandler },
                { typeof(XmlSchemaGroupRef), ErrorHandler },
                { typeof(XmlSchemaSimpleTypeUnion), ErrorHandler },
                { typeof(XmlSchemaSimpleTypeList), JsonSimpleTypeListHandler },
                { typeof(XmlSchemaXPath), ErrorHandler },
                { typeof(XmlSchemaElement), JsonElementHandler },
                { typeof(XmlSchemaComplexType), JsonComplexTypeHandler },
                { typeof(XmlSchemaSequence), JsonSequenceHandler },
                { typeof(XmlSchemaSimpleType), JsonSimpleTypeHandler },
            };

            public static void GenerateJsonSample(XmlSchemaSet schemaSet, XmlQualifiedName name, XmlWriter writer, IDictionary<XmlQualifiedName, Type> knownTypes)
            {
                HelpExampleGeneratorContext context = new HelpExampleGeneratorContext
                {
                    currentDepthLevel = 0,
                    elementDepth = new Dictionary<XmlSchemaElement, int>(),
                    knownTypes = knownTypes,
                    objectHandler = JsonObjectHandler,
                    schemaSet = schemaSet,
                    overrideElementName = JsonGlobals.rootString,
                    writer = writer,
                };

                if (!schemaSet.IsCompiled)
                {
                    schemaSet.Compile();
                }
                InvokeHandler(schemaSet.GlobalElements[name], context);
            }

            public static void GenerateXmlSample(XmlSchemaSet schemaSet, XmlQualifiedName name, XmlWriter writer)
            {
                HelpExampleGeneratorContext context = new HelpExampleGeneratorContext
                {
                    currentDepthLevel = 0,
                    elementDepth = new Dictionary<XmlSchemaElement, int>(),
                    knownTypes = null,
                    objectHandler = XmlObjectHandler,
                    schemaSet = schemaSet,
                    overrideElementName = null,
                    writer = writer,
                };

                if (!schemaSet.IsCompiled)
                {
                    schemaSet.Compile();
                }

                InvokeHandler(schemaSet.GlobalElements[name], context);
            }

            [System.Diagnostics.DebuggerStepThrough]
            private static void InvokeHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                if (++context.currentDepthLevel < MaxDepthLevel)
                {
                    Action<XmlSchemaObject, HelpExampleGeneratorContext> action;
                    Type objectType = schemaObject.GetType();
                    if (context.objectHandler.TryGetValue(objectType, out action))
                    {
                        action(schemaObject, context);
                    }
                    else if (objectType.Name != "EmptyParticle")
                    {
                        throw Fx.Exception.AsError(new InvalidOperationException(Http.SR.HelpExampleGeneratorHandlerNotFound(schemaObject.GetType().Name)));
                    }
                    --context.currentDepthLevel;
                }
                else
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(Http.SR.HelpExampleGeneratorMaxDepthLevelReached(schemaObject.GetType().Name)));
                }
            }

            private static void XmlAttributeHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaAttribute attribute = (XmlSchemaAttribute)schemaObject;
                string content = GenerateContentForXmlSimpleType(attribute.AttributeSchemaType);
                if (String.IsNullOrEmpty(content))
                {
                    context.writer.WriteAttributeString("i", XmlSchemaInstanceNil, XmlSchemaInstanceNamespace, "true");
                }
                else
                {
                    context.writer.WriteAttributeString(attribute.QualifiedName.Name, attribute.QualifiedName.Namespace, content);
                }
            }

            private static void ChoiceHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaChoice choice = (XmlSchemaChoice)schemaObject;
                InvokeHandler(choice.Items[0], context);
            }

            private static void ContentHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaContentModel model = (XmlSchemaContentModel)schemaObject;
                InvokeHandler(model.Content, context);
            }

            private static void SimpleTypeRestrictionHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)schemaObject;
                foreach (XmlSchemaObject facet in restriction.Facets)
                {
                    InvokeHandler(facet, context);
                }
            }

            private static void ErrorHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(Http.SR.HelpExampleGeneratorSchemaObjectNotSupported(schemaObject.GetType().Name)));
            }

            private static void EmptyHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
            }

            private static void XmlElementHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaElement element = (XmlSchemaElement)schemaObject;
                XmlSchemaElement contentElement = GenerateValidElementsComment(element, context);
                context.writer.WriteStartElement(element.QualifiedName.Name, element.QualifiedName.Namespace);
                if (contentElement != element)
                {
                    string value = contentElement.QualifiedName.Name;
                    if (contentElement.QualifiedName.Namespace != element.QualifiedName.Namespace && !String.IsNullOrEmpty(contentElement.QualifiedName.Namespace))
                    {
                        string prefix = context.writer.LookupPrefix(contentElement.QualifiedName.Namespace);
                        if (prefix == null)
                        {
                            prefix = string.Concat("d", context.currentDepthLevel.ToString(System.Globalization.NumberFormatInfo.InvariantInfo));
                            context.writer.WriteAttributeString(XmlNamespacePrefix, prefix, null, contentElement.QualifiedName.Namespace);
                        }
                        value = String.Format(CultureInfo.InvariantCulture, "{0}:{1}", prefix, contentElement.QualifiedName.Name);
                    }
                    context.writer.WriteAttributeString("i", XmlSchemaInstanceType, XmlSchemaInstanceNamespace, value);
                }
                foreach (XmlSchemaObject constraint in contentElement.Constraints)
                {
                    InvokeHandler(constraint, context);
                }
                InvokeHandler(contentElement.ElementSchemaType, context);
                context.writer.WriteEndElement();
            }

            private static void XmlComplexTypeHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaComplexType complexType = (XmlSchemaComplexType)schemaObject;
                foreach (XmlSchemaObject attribute in complexType.AttributeUses.Values)
                {
                    InvokeHandler(attribute, context);
                }
                if (complexType.ContentModel != null)
                {
                    InvokeHandler(complexType.ContentModel, context);
                }
                InvokeHandler(complexType.ContentTypeParticle, context);
                if (complexType.IsMixed)
                {
                    context.writer.WriteString(Http.SR.HelpExampleGeneratorThisElementContainsText);
                }
            }

            private static void XmlSequenceHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaSequence sequence = (XmlSchemaSequence)schemaObject;
                foreach (XmlSchemaObject innerObject in sequence.Items)
                {
                    XmlSchemaElement element = innerObject as XmlSchemaElement;
                    for (int count = 0; element != null && count < 2 && element.MaxOccurs > count; ++count)
                    {
                        if (element != null && IsObject(element))
                        {
                            int instances = 0;
                            context.elementDepth.TryGetValue(element, out instances);
                            context.elementDepth[element] = ++instances;
                            if (instances < 3)
                            {
                                InvokeHandler(innerObject, context);
                            }
                            else
                            {
                                context.writer.WriteStartElement(element.QualifiedName.Name, element.QualifiedName.Namespace);
                                context.writer.WriteAttributeString("i", XmlSchemaInstanceNil, XmlSchemaInstanceNamespace, "true");
                                context.writer.WriteEndElement();
                            }
                            --context.elementDepth[element];
                        }
                        else
                        {
                            InvokeHandler(innerObject, context);
                        }
                    }
                }
            }

            private static void XmlSimpleTypeListHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaSimpleTypeList simpleTypeList = (XmlSchemaSimpleTypeList)schemaObject;
                InvokeHandler(simpleTypeList.ItemType, context);
            }

            private static void XmlSimpleTypeHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                //// ALTERED_FOR_PORT: Globals is internal
                ////string globalsSerializationNamespace = System.Runtime.Serialization.Globals.SerializationNamespace;
                string globalsSerializationNamespace = "http://schemas.microsoft.com/2003/10/Serialization/";

                XmlSchemaSimpleType simpleType = (XmlSchemaSimpleType)schemaObject;
                if (simpleType.QualifiedName.Namespace != globalsSerializationNamespace
                    && simpleType.QualifiedName.Namespace != XmlSchemaNamespace
                    && simpleType.QualifiedName.Name != "guid")
                {
                    InvokeHandler(simpleType.Content, context);
                }
                string content = GenerateContentForXmlSimpleType(simpleType);
                if (String.IsNullOrEmpty(content))
                {
                    if (!(simpleType.Content is XmlSchemaSimpleTypeList))
                    {
                        context.writer.WriteAttributeString("i", XmlSchemaInstanceNil, XmlSchemaInstanceNamespace, "true");
                    }
                }
                else
                {
                    context.writer.WriteString(content);
                }
            }

            private static string GenerateContentForXmlSimpleType(XmlSchemaSimpleType simpleType)
            {
                if (simpleType.Content != null && simpleType.Content is XmlSchemaSimpleTypeRestriction)
                {
                    XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)simpleType.Content;
                    foreach (XmlSchemaObject facet in restriction.Facets)
                    {
                        if (facet is XmlSchemaEnumerationFacet)
                        {
                            XmlSchemaEnumerationFacet enumeration = (XmlSchemaEnumerationFacet)facet;
                            return enumeration.Value;
                        }
                    }
                }

                if (simpleType.QualifiedName.Name == "dateTime")
                {
                    DateTime dateTime = DateTime.Parse("1999-05-31T11:20:00", CultureInfo.InvariantCulture);
                    return dateTime.ToString("s", CultureInfo.InvariantCulture);
                }
                else if (simpleType.QualifiedName.Name == "char")
                {
                    return "97";
                }

                return GetConstantValue(simpleType.QualifiedName.Name);
            }

            private static void JsonElementHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaElement element = (XmlSchemaElement)schemaObject;
                XmlSchemaElement contentElement = GetDerivedTypes(element, context).FirstOrDefault();
                if (contentElement == null)
                {
                    contentElement = element;
                }

                if (context.overrideElementName != null)
                {
                    context.writer.WriteStartElement(null, context.overrideElementName, null);
                    context.overrideElementName = null;
                }
                else
                {
                    context.writer.WriteStartElement(null, element.Name, null);
                }

                if (IsArrayElementType(element))
                {
                    context.writer.WriteAttributeString(JsonGlobals.typeString, JsonGlobals.arrayString);
                    context.overrideElementName = JsonGlobals.itemString;
                }
                else if (IsObject(element))
                {
                    if (contentElement != element)
                    {
                        Type derivedType = null;
                        context.knownTypes.TryGetValue(contentElement.QualifiedName, out derivedType);
                        if (derivedType != null)
                        {
                            context.writer.WriteStartAttribute(null, JsonGlobals.serverTypeString, null);
                            context.writer.WriteString(String.Format(CultureInfo.InvariantCulture, "{0}:#{1}", derivedType.Name, derivedType.Namespace));
                            context.writer.WriteEndAttribute();
                        }
                    }
                    context.writer.WriteAttributeString(JsonGlobals.typeString, JsonGlobals.objectString);
                }
                InvokeHandler(contentElement.ElementSchemaType, context);
                context.overrideElementName = null;
                context.writer.WriteEndElement();
            }

            private static void JsonComplexTypeHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaComplexType complexType = (XmlSchemaComplexType)schemaObject;
                if (complexType.ContentModel != null)
                {
                    InvokeHandler(complexType.ContentModel, context);
                }
                InvokeHandler(complexType.ContentTypeParticle, context);
            }

            private static void JsonSequenceHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaSequence sequence = (XmlSchemaSequence)schemaObject;
                foreach (XmlSchemaObject innerObject in sequence.Items)
                {
                    XmlSchemaElement element = innerObject as XmlSchemaElement;
                    if (element != null && IsObject(element))
                    {
                        int instances = 0;
                        context.elementDepth.TryGetValue(element, out instances);
                        context.elementDepth[element] = ++instances;
                        if (instances < 3)
                        {
                            InvokeHandler(innerObject, context);
                        }
                        else
                        {
                            if (context.overrideElementName != null)
                            {
                                context.writer.WriteStartElement(context.overrideElementName);
                                context.overrideElementName = null;
                            }
                            else
                            {
                                context.writer.WriteStartElement(element.QualifiedName.Name);
                            }
                            context.writer.WriteAttributeString(JsonGlobals.typeString, JsonGlobals.nullString);
                            context.writer.WriteEndElement();
                        }
                        --context.elementDepth[element];
                    }
                    else
                    {
                        InvokeHandler(innerObject, context);
                    }
                }
            }

            private static void JsonSimpleTypeListHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaSimpleTypeList simpleTypeList = (XmlSchemaSimpleTypeList)schemaObject;
                InvokeHandler(simpleTypeList.ItemType, context);
            }

            private static void JsonSimpleTypeHandler(XmlSchemaObject schemaObject, HelpExampleGeneratorContext context)
            {
                XmlSchemaSimpleType simpleType = (XmlSchemaSimpleType)schemaObject;
                // Enumerations return 0
                if (simpleType.Content != null)
                {
                    if (simpleType.Content is XmlSchemaSimpleTypeRestriction)
                    {
                        XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)simpleType.Content;
                        foreach (XmlSchemaObject facet in restriction.Facets)
                        {
                            if (facet is XmlSchemaEnumerationFacet)
                            {
                                context.writer.WriteAttributeString(string.Empty, JsonGlobals.typeString, string.Empty, JsonGlobals.numberString);
                                context.writer.WriteString("0");
                                return;
                            }
                        }
                    }
                    else if (simpleType.Content is XmlSchemaSimpleTypeList)
                    {
                        InvokeHandler(simpleType.Content, context);
                    }
                }

                string value = GetConstantValue(simpleType.QualifiedName.Name);

                if (simpleType.QualifiedName.Name == "base64Binary")
                {
                    char[] base64stream = value.ToCharArray();
                    context.writer.WriteAttributeString(string.Empty, JsonGlobals.typeString, string.Empty, JsonGlobals.arrayString);
                    for (int i = 0; i < base64stream.Length; i++)
                    {
                        context.writer.WriteStartElement(JsonGlobals.itemString, string.Empty);
                        context.writer.WriteAttributeString(string.Empty, JsonGlobals.typeString, string.Empty, JsonGlobals.numberString);
                        context.writer.WriteValue((int)base64stream[i]);
                        context.writer.WriteEndElement();
                    }
                }
                else if (simpleType.QualifiedName.Name == "dateTime")
                {
                    DateTime dateTime = DateTime.Parse("1999-05-31T11:20:00", CultureInfo.InvariantCulture);
                    context.writer.WriteString(JsonGlobals.DateTimeStartGuardReader);
                    context.writer.WriteValue((dateTime.ToUniversalTime().Ticks - JsonGlobals.unixEpochTicks) / 10000);

                    switch (dateTime.Kind)
                    {
                        case DateTimeKind.Unspecified:
                        case DateTimeKind.Local:
                            TimeSpan ts = TimeZone.CurrentTimeZone.GetUtcOffset(dateTime.ToLocalTime());
                            if (ts.Ticks < 0)
                            {
                                context.writer.WriteString("-");
                            }
                            else
                            {
                                context.writer.WriteString("+");
                            }
                            int hours = Math.Abs(ts.Hours);
                            context.writer.WriteString((hours < 10) ? "0" + hours : hours.ToString(CultureInfo.InvariantCulture));
                            int minutes = Math.Abs(ts.Minutes);
                            context.writer.WriteString((minutes < 10) ? "0" + minutes : minutes.ToString(CultureInfo.InvariantCulture));
                            break;
                        case DateTimeKind.Utc:
                            break;
                    }
                    context.writer.WriteString(JsonGlobals.DateTimeEndGuardReader);
                }
                else if (simpleType.QualifiedName.Name == "char")
                {
                    context.writer.WriteString(XmlConvert.ToString('a'));
                }
                else if (!String.IsNullOrEmpty(value))
                {
                    if (simpleType.QualifiedName.Name == "integer" ||
                        simpleType.QualifiedName.Name == "int" ||
                        simpleType.QualifiedName.Name == "long" ||
                        simpleType.QualifiedName.Name == "unsignedLong" ||
                        simpleType.QualifiedName.Name == "unsignedInt" ||
                        simpleType.QualifiedName.Name == "short" ||
                        simpleType.QualifiedName.Name == "unsignedShort" ||
                        simpleType.QualifiedName.Name == "byte" ||
                        simpleType.QualifiedName.Name == "unsignedByte" ||
                        simpleType.QualifiedName.Name == "decimal" ||
                        simpleType.QualifiedName.Name == "float" ||
                        simpleType.QualifiedName.Name == "double" ||
                        simpleType.QualifiedName.Name == "negativeInteger" ||
                        simpleType.QualifiedName.Name == "nonPositiveInteger" ||
                        simpleType.QualifiedName.Name == "positiveInteger" ||
                        simpleType.QualifiedName.Name == "nonNegativeInteger")
                    {
                        context.writer.WriteAttributeString(JsonGlobals.typeString, JsonGlobals.numberString);
                    }
                    else if (simpleType.QualifiedName.Name == "boolean")
                    {
                        context.writer.WriteAttributeString(JsonGlobals.typeString, JsonGlobals.booleanString);
                    }
                    context.writer.WriteString(value);
                }
                else
                {
                    if (!(simpleType.Content is XmlSchemaSimpleTypeList))
                    {
                        context.writer.WriteAttributeString(JsonGlobals.typeString, JsonGlobals.nullString);
                    }
                }
            }

            private static string GetConstantValue(string typeName)
            {
                if (typeName == "base64Binary")
                {
                    return "QmFzZSA2NCBTdHJlYW0=";
                }
                else if (typeName == "string" ||
                    typeName == "normalizedString" ||
                    typeName == "token" ||
                    typeName == "NMTOKEN" ||
                    typeName == "NMTOKENS")
                {
                    return Http.SR.HelpExampleGeneratorStringContent;
                }
                else if (typeName == "hexBinary")
                {
                    return "GpM7";
                }
                else if (typeName == "integer" || typeName == "int")
                {
                    return "2147483647";
                }
                else if (typeName == "positiveInteger" || typeName == "nonNegativeInteger")
                {
                    return "+2147483647";
                }
                else if (typeName == "long")
                {
                    return "9223372036854775807";
                }
                else if (typeName == "unsignedLong")
                {
                    return "18446744073709551615";
                }
                else if (typeName == "unsignedInt")
                {
                    return "4294967295";
                }
                else if (typeName == "short")
                {
                    return "32767";
                }
                else if (typeName == "unsignedShort")
                {
                    return "65535";
                }
                else if (typeName == "byte")
                {
                    return "127";
                }
                else if (typeName == "unsignedByte")
                {
                    return "255";
                }
                else if (typeName == "decimal")
                {
                    return "12678967.543233";
                }
                else if (typeName == "float")
                {
                    return "1.26743237E+15";
                }
                else if (typeName == "double")
                {
                    return "1.26743233E+15";
                }
                else if (typeName == "negativeInteger" || typeName == "nonPositiveInteger")
                {
                    return "-12678967543233";
                }
                else if (typeName == "boolean")
                {
                    return "true";
                }
                else if (typeName == "duration")
                {
                    return "P428DT10H30M12.3S";
                }
                else if (typeName == "date")
                {
                    return "1999-05-31";
                }
                else if (typeName == "time")
                {
                    return "13:20:00.000, 13:20:00.000-05:00";
                }
                else if (typeName == "gYear")
                {
                    return "1999";
                }
                else if (typeName == "gYearMonth")
                {
                    return "1999-02";
                }
                else if (typeName == "gMonth")
                {
                    return "--05";
                }
                else if (typeName == "gMonthDay")
                {
                    return "--05-31";
                }
                else if (typeName == "gDay")
                {
                    return "---31";
                }
                else if (typeName == "Name")
                {
                    return "Name";
                }
                else if (typeName == "QName" || typeName == "NOTATION")
                {
                    return "namespace:Name";
                }
                else if (typeName == "NCName" ||
                    typeName == "ID" ||
                    typeName == "IDREF" ||
                    typeName == "IDREFS" ||
                    typeName == "ENTITY" ||
                    typeName == "ENTITY" ||
                    typeName == "ID")
                {
                    return "NCNameString";
                }
                else if (typeName == "anyURI")
                {
                    return "http://www.example.com/";
                }
                else if (typeName == "language")
                {
                    return Http.SR.HelpExampleGeneratorLanguage;
                }
                else if (typeName == "guid")
                {
                    return "1627aea5-8e0a-4371-9022-9b504344e724";
                }
                return null;
            }

            private static XmlSchemaElement GenerateValidElementsComment(XmlSchemaElement element, HelpExampleGeneratorContext context)
            {
                XmlSchemaElement firstNonAbstractElement = element;
                StringBuilder validTypes = new StringBuilder();
                foreach (XmlSchemaElement derivedElement in GetDerivedTypes(element, context))
                {
                    if (firstNonAbstractElement == element)
                    {
                        firstNonAbstractElement = derivedElement;
                    }
                    if (validTypes.Length > 0)
                    {
                        validTypes.AppendFormat(", {0}", derivedElement.Name);
                    }
                    else
                    {
                        validTypes.AppendFormat(Http.SR.HelpPageValidElementOfType(derivedElement.Name));
                    }
                }
                if (validTypes.Length > 0)
                {
                    context.writer.WriteComment(validTypes.ToString());
                }
                return firstNonAbstractElement;
            }

            private static IEnumerable<XmlSchemaElement> GetDerivedTypes(XmlSchemaElement element, HelpExampleGeneratorContext context)
            {
                if (element.ElementSchemaType is XmlSchemaComplexType)
                {
                    foreach (XmlSchemaElement derivedElement in context.schemaSet.GlobalElements.Values.OfType<XmlSchemaElement>().Where(e =>
                        e.IsAbstract == false &&
                        e.ElementSchemaType != element.ElementSchemaType &&
                        e.ElementSchemaType is XmlSchemaComplexType &&
                        DerivesFrom((XmlSchemaComplexType)element.ElementSchemaType, (XmlSchemaComplexType)e.ElementSchemaType)).OrderBy(e => e.Name))
                    {
                        yield return derivedElement;
                    }
                }
            }

            private static bool DerivesFrom(XmlSchemaComplexType parent, XmlSchemaComplexType child)
            {
                if (parent == child)
                {
                    return true;
                }
                else if (child.BaseXmlSchemaType is XmlSchemaComplexType)
                {
                    return DerivesFrom(parent, (XmlSchemaComplexType)child.BaseXmlSchemaType);
                }
                else
                {
                    return false;
                }
            }

            private static bool IsArrayElementType(XmlSchemaElement element)
            {
                if (element.ElementSchemaType is XmlSchemaComplexType)
                {
                    XmlSchemaComplexType complexType = element.ElementSchemaType as XmlSchemaComplexType;
                    if (complexType.ContentTypeParticle != null && complexType.ContentTypeParticle is XmlSchemaSequence)
                    {
                        XmlSchemaSequence sequence = complexType.ContentTypeParticle as XmlSchemaSequence;
                        if (sequence.Items.Count > 0)
                        {
                            XmlSchemaElement firstElement = sequence.Items[0] as XmlSchemaElement;
                            if (firstElement != null && firstElement.MaxOccurs > 1)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            private static bool IsObject(XmlSchemaElement element)
            {
                return element.ElementSchemaType is XmlSchemaComplexType;
            }

            private class HelpExampleGeneratorContext
            {
                public string overrideElementName;
                public int currentDepthLevel;
                public IDictionary<XmlQualifiedName, Type> knownTypes;
                public XmlSchemaSet schemaSet;
                public IDictionary<XmlSchemaElement, int> elementDepth;
                public XmlWriter writer;
                public Dictionary<Type, Action<XmlSchemaObject, HelpExampleGeneratorContext>> objectHandler;
            }
        }

        private class HttpHelpOperationInvoker : IOperationInvoker
        {
            private HelpPage helpPage;
            private bool invokerForOperationListHelpPage;

            public HttpHelpOperationInvoker(HelpPage helpPage, bool invokerForOperationListHelpPage)
            {
                this.helpPage = helpPage;
                this.invokerForOperationListHelpPage = invokerForOperationListHelpPage;
            }

            public bool IsSynchronous
            {
                get
                {
                    return true;
                }
            }

            public object Invoke(object instance, object[] inputs, out object[] outputs)
            {            
                HttpRequestMessage request = inputs[0] as HttpRequestMessage;
                HttpResponseMessage response = new HttpResponseMessage();
                response.RequestMessage = request;

                outputs = new object[0];

                if (this.invokerForOperationListHelpPage)
                {
                    this.helpPage.InvokeHelpPage(response);
                }
                else
                {
                    string operationName = inputs[1] as string;
                    this.helpPage.InvokeOperationHelpPage(operationName, response);
                }

                return response;
            }

            public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
            {
                throw Fx.Exception.AsError(new NotSupportedException());
            }

            public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
            {
                throw Fx.Exception.AsError(new NotSupportedException());
            }

            public object[] AllocateInputs()
            {
                return this.invokerForOperationListHelpPage ? new object[1] : new object[2];
            }
        }
    }
}
