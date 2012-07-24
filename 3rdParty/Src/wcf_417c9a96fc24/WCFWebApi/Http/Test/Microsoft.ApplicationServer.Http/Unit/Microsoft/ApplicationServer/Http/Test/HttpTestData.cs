// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Test
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Moles;
using Microsoft.ApplicationServer.Http.Description;
    using System.ServiceModel.Web;

    public static class HttpTestData
    {
        public static readonly TestData<HttpMethod> AllHttpMethods = new RefTypeTestData<HttpMethod>(() =>
            StandardHttpMethods.Concat(CustomHttpMethods).ToList());

        public static readonly TestData<HttpMethod> StandardHttpMethods = new RefTypeTestData<HttpMethod>(() => new List<HttpMethod>() 
        { 
            HttpMethod.Head,
            HttpMethod.Get,
            HttpMethod.Post,
            HttpMethod.Put,
            HttpMethod.Delete,
            HttpMethod.Options,
            HttpMethod.Trace,
        });

        public static readonly TestData<HttpMethod> CustomHttpMethods = new RefTypeTestData<HttpMethod>(() => new List<HttpMethod>() 
        { 
            new HttpMethod("Custom")
        });

        public static readonly TestData<HttpStatusCode> AllHttpStatusCodes = new ValueTypeTestData<HttpStatusCode>(new HttpStatusCode[]
        {
            HttpStatusCode.Accepted,
            HttpStatusCode.Ambiguous,
            HttpStatusCode.BadGateway,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Conflict,
            HttpStatusCode.Continue,
            HttpStatusCode.Created,
            HttpStatusCode.ExpectationFailed,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Found,
            HttpStatusCode.GatewayTimeout,
            HttpStatusCode.Gone,
            HttpStatusCode.HttpVersionNotSupported,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.LengthRequired,
            HttpStatusCode.MethodNotAllowed,
            HttpStatusCode.Moved,
            HttpStatusCode.MovedPermanently,
            HttpStatusCode.MultipleChoices,
            HttpStatusCode.NoContent,
            HttpStatusCode.NonAuthoritativeInformation,
            HttpStatusCode.NotAcceptable,
            HttpStatusCode.NotFound,
            HttpStatusCode.NotImplemented,
            HttpStatusCode.NotModified,
            HttpStatusCode.OK,
            HttpStatusCode.PartialContent,
            HttpStatusCode.PaymentRequired,
            HttpStatusCode.PreconditionFailed,
            HttpStatusCode.ProxyAuthenticationRequired,
            HttpStatusCode.Redirect,
            HttpStatusCode.RedirectKeepVerb,
            HttpStatusCode.RedirectMethod,
            HttpStatusCode.RequestedRangeNotSatisfiable,
            HttpStatusCode.RequestEntityTooLarge,
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.RequestUriTooLong,
            HttpStatusCode.ResetContent,
            HttpStatusCode.SeeOther,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.SwitchingProtocols,
            HttpStatusCode.TemporaryRedirect,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.UnsupportedMediaType,
            HttpStatusCode.Unused,
            HttpStatusCode.UseProxy
        });

        public static readonly ReadOnlyCollection<TestData> ConvertablePrimitiveValueTypes = new ReadOnlyCollection<TestData>(new TestData[] {
            TestData.CharTestData, 
            TestData.IntTestData, 
            TestData.UintTestData, 
            TestData.ShortTestData, 
            TestData.UshortTestData, 
            TestData.LongTestData, 
            TestData.UlongTestData, 
            TestData.ByteTestData, 
            TestData.SByteTestData, 
            TestData.BoolTestData,
            TestData.DoubleTestData, 
            TestData.FloatTestData, 
            TestData.DecimalTestData, 
            TestData.TimeSpanTestData, 
            TestData.GuidTestData, 
            TestData.DateTimeTestData,
            TestData.DateTimeOffsetTestData,
            TestData.UriTestData});

        public static readonly ReadOnlyCollection<TestData> ConvertableEnumTypes = new ReadOnlyCollection<TestData>(new TestData[] {
            TestData.SimpleEnumTestData, 
            TestData.LongEnumTestData,
            TestData.FlagsEnumTestData, 
            TestData.DataContractEnumTestData});

        public static readonly ReadOnlyCollection<TestData> ConvertableValueTypes = new ReadOnlyCollection<TestData>(
            ConvertablePrimitiveValueTypes.Concat(ConvertableEnumTypes).ToList());

        public static readonly TestData<MediaTypeHeaderValue> StandardJsonMediaTypes = new RefTypeTestData<MediaTypeHeaderValue>(() => new List<MediaTypeHeaderValue>() 
        { 
            new MediaTypeHeaderValue("application/json") { CharSet="utf-8"},
            new MediaTypeHeaderValue("text/json") { CharSet="utf-8"}
        });

        public static readonly TestData<MediaTypeHeaderValue> StandardXmlMediaTypes = new RefTypeTestData<MediaTypeHeaderValue>(() => new List<MediaTypeHeaderValue>() 
        { 
            new MediaTypeHeaderValue("application/xml") { CharSet="utf-8"},
            new MediaTypeHeaderValue("text/xml") { CharSet="utf-8"}
        });

        public static readonly TestData<string> StandardJsonMediaTypeStrings = new RefTypeTestData<string>(() => new List<string>() 
        { 
            "application/json",
            "text/json"
        });

        public static readonly TestData<string> StandardXmlMediaTypeStrings = new RefTypeTestData<string>(() => new List<string>() 
        { 
            "application/xml",
            "text/xml"
        });

        public static readonly TestData<string> LegalMediaTypeStrings = new RefTypeTestData<string>(() =>
            StandardXmlMediaTypeStrings.Concat(StandardJsonMediaTypeStrings).ToList());


        // Illegal media type strings.  These will cause the MediaTypeHeaderValue ctor to throw FormatException
        public static readonly TestData<string> IllegalMediaTypeStrings = new RefTypeTestData<string>(() => new List<string>() 
        { 
            "\0",
            "9\r\n"
        });

        //// TODO: complete this list
        // Legal MediaTypeHeaderValues
        public static readonly TestData<MediaTypeHeaderValue> LegalMediaTypeHeaderValues = new RefTypeTestData<MediaTypeHeaderValue>(
            () => LegalMediaTypeStrings.Select<string, MediaTypeHeaderValue>((mediaType) => new MediaTypeHeaderValue(mediaType)).ToList());


        public static readonly TestData<HttpContent> StandardHttpContents = new RefTypeTestData<HttpContent>(() => new List<HttpContent>() 
        { 
            new ActionOfStreamContent((stream) => {}),
            new ByteArrayContent(new byte[0]),
            new FormUrlEncodedContent(new KeyValuePair<string, string>[0]),
            new MultipartContent(),
            new StringContent(""),
            new StreamContent(new MemoryStream())
        });

        //// TODO: make this list compose from other data?
        // Collection of legal instances of all standard MediaTypeMapping types
        public static readonly TestData<MediaTypeMapping> StandardMediaTypeMappings = new RefTypeTestData<MediaTypeMapping>(() =>
            QueryStringMappings.Cast<MediaTypeMapping>().Concat(
                UriPathExtensionMappings.Cast<MediaTypeMapping>().Concat(
                    MediaRangeMappings.Cast<MediaTypeMapping>())).ToList()
        );

        public static readonly TestData<QueryStringMapping> QueryStringMappings = new RefTypeTestData<QueryStringMapping>(() => new List<QueryStringMapping>() 
        { 
            new QueryStringMapping("format", "json", new MediaTypeHeaderValue("application/json"))
        });

        public static readonly TestData<UriPathExtensionMapping> UriPathExtensionMappings = new RefTypeTestData<UriPathExtensionMapping>(() => new List<UriPathExtensionMapping>() 
        { 
            new UriPathExtensionMapping("xml", new MediaTypeHeaderValue("application/xml")),
            new UriPathExtensionMapping("json", new MediaTypeHeaderValue("application/json")),
        });

        public static readonly TestData<MediaRangeMapping> MediaRangeMappings = new RefTypeTestData<MediaRangeMapping>(() => new List<MediaRangeMapping>() 
        { 
            new MediaRangeMapping(new MediaTypeHeaderValue("application/*"), new MediaTypeHeaderValue("application/xml"))
        });

        public static readonly TestData<string> LegalUriPathExtensions = new RefTypeTestData<string>(() => new List<string>()
        { 
            "xml", 
            "json"
        });

        public static readonly TestData<string> LegalQueryStringParameterNames = new RefTypeTestData<string>(() => new List<string>()
        { 
            "format", 
            "fmt" 
        });

        public static readonly TestData<string> LegalQueryStringParameterValues = new RefTypeTestData<string>(() => new List<string>()
        { 
            "xml", 
            "json" 
        });

        public static readonly TestData<string> LegalMediaRangeStrings = new RefTypeTestData<string>(() => new List<string>()
        { 
            "application/*", 
            "text/*"
        });

        public static readonly TestData<MediaTypeHeaderValue> LegalMediaRangeValues = new RefTypeTestData<MediaTypeHeaderValue>(() =>
            LegalMediaRangeStrings.Select<string, MediaTypeHeaderValue>((s) => new MediaTypeHeaderValue(s)).ToList()
            );

        public static readonly TestData<string> IllegalMediaRangeStrings = new RefTypeTestData<string>(() => new List<string>()
        { 
            "application/xml", 
            "text/xml" 
        });

        public static readonly TestData<MediaTypeHeaderValue> IllegalMediaRangeValues = new RefTypeTestData<MediaTypeHeaderValue>(() =>
            IllegalMediaRangeStrings.Select<string, MediaTypeHeaderValue>((s) => new MediaTypeHeaderValue(s)).ToList()
            );

        public static readonly TestData<MediaTypeFormatter> StandardFormatters = new RefTypeTestData<MediaTypeFormatter>(() => new List<MediaTypeFormatter>() 
        { 
            new XmlMediaTypeFormatter(),
            new JsonMediaTypeFormatter()
        });

        public static readonly TestData<Type> StandardFormatterTypes = new RefTypeTestData<Type>(() =>
            StandardFormatters.Select<MediaTypeFormatter, Type>((m) => m.GetType()));

        public static readonly TestData<MediaTypeFormatter> DerivedFormatters = new RefTypeTestData<MediaTypeFormatter>(() => new List<MediaTypeFormatter>() 
        { 
            new SXmlMediaTypeFormatter(),
            new SJsonMediaTypeFormatter()
        });

        public static readonly TestData<IEnumerable<MediaTypeFormatter>> AllFormatterCollections =
            new RefTypeTestData<IEnumerable<MediaTypeFormatter>>(() => new List<IEnumerable<MediaTypeFormatter>>()
            {
                new MediaTypeFormatter[0],
                StandardFormatters,
                DerivedFormatters,
            });

        /// <summary>
        /// A read-only collection of representative values and reference type test data.
        /// Uses where exhaustive coverage is not required.  It includes null values.
        /// </summary>
        public static readonly ReadOnlyCollection<TestData> RepresentativeValueAndRefTypeTestDataCollection = new ReadOnlyCollection<TestData>(new TestData[] {
             TestData.ByteTestData,
             TestData.IntTestData,
             TestData.BoolTestData,
             TestData.SimpleEnumTestData,
             TestData.StringTestData, 
             TestData.DateTimeTestData,
             TestData.DateTimeOffsetTestData,
             TestData.TimeSpanTestData,
             TestData.PocoTypeTestDataWithNull
        });

        public static bool IsKnownUnserializableType(Type type, Func<Type, bool> isTypeUnserializableCallback)
        {
            if (isTypeUnserializableCallback != null && isTypeUnserializableCallback(type))
            {
                return true;
            }

            if (type.IsGenericType)
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    if (type.GetMethod("Add") == null)
                    {
                        return true;
                    }
                }

                // Generic type -- recursively analyze generic arguments
                return IsKnownUnserializableType(type.GetGenericArguments()[0], isTypeUnserializableCallback);
            }

            if (type.HasElementType && IsKnownUnserializableType(type.GetElementType(), isTypeUnserializableCallback))
            {
                return true;
            }

            return false;
        }

        public static bool IsKnownUnserializable(Type type, object obj, Func<Type, bool> isTypeUnserializableCallback)
        {
            if (IsKnownUnserializableType(type, isTypeUnserializableCallback))
            {
                return true;
            }

            return obj != null && IsKnownUnserializableType(obj.GetType(), isTypeUnserializableCallback);
        }

        public static bool IsKnownUnserializable(Type type, object obj)
        {
            return IsKnownUnserializable(type, obj, null);
        }

        public static bool CanRoundTrip(Type type)
        {
            if (typeof(TimeSpan).IsAssignableFrom(type))
            {
                return false;
            }

            if (typeof(DateTimeOffset).IsAssignableFrom(type))
            {
                return false;
            }

            if (type.IsGenericType)
            {
                foreach (Type genericParameterType in type.GetGenericArguments())
                {
                    if (!CanRoundTrip(genericParameterType))
                    {
                        return false;
                    }
                }
            }

            if (type.HasElementType)
            {
                return CanRoundTrip(type.GetElementType());
            }

            return true;
        }
    }
}
