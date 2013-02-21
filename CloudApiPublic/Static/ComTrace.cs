//
// ComTrace.cs
// Cloud
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using CloudApiPublic.Model;

namespace CloudApiPublic.Static
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ComTrace
    {
        #region public methods
        public static void LogCommunication(string traceLocation, string UserDeviceId, Nullable<long> SyncBoxId, CommunicationEntryDirection Direction, string DomainAndMethodUri, bool traceEnabled = false, WebHeaderCollection headers = null, Stream body = null, Nullable<int> statusCode = null, bool excludeAuthorization = true, string hostHeader = null, string contentLengthHeader = null, string expectHeader = null, string connectionHeader = null)
        {
            string bodyString = null;
            if (traceEnabled
                && body != null)
            {
                try
                {
                    TextReader textStream = new StreamReader(body, Encoding.UTF8);
                    bodyString = textStream.ReadToEnd();
                    body.Seek(0, SeekOrigin.Begin);
                }
                catch
                {
                }
            }
            LogCommunication(traceLocation,
                UserDeviceId,
                SyncBoxId,
                Direction,
                DomainAndMethodUri,
                traceEnabled,
                headers,
                bodyString,
                statusCode,
                excludeAuthorization,
                hostHeader,
                contentLengthHeader,
                expectHeader,
                connectionHeader);
        }

        public static void LogCommunication(string traceLocation, string UserDeviceId, Nullable<long> SyncBoxId, CommunicationEntryDirection Direction, string DomainAndMethodUri, bool traceEnabled = false, WebHeaderCollection headers = null, string body = null, Nullable<int> statusCode = null, bool excludeAuthorization = true, string hostHeader = null, string contentLengthHeader = null, string expectHeader = null, string connectionHeader = null)
        {
            if (traceEnabled
                && !string.IsNullOrWhiteSpace(UserDeviceId)
                && !string.IsNullOrWhiteSpace(DomainAndMethodUri))
            {
                try
                {
                    string UDid = (string.IsNullOrWhiteSpace(UserDeviceId)
                        ? "NotLinked"
                        : UserDeviceId);

                    LogCommunication(traceLocation,
                        UDid,
                        SyncBoxId,
                        Direction,
                        DomainAndMethodUri,
                        (headers == null
                            ? null
                            : headers.Keys.OfType<object>()
                                .Select(currentHeaderKey => (currentHeaderKey == null ? null : currentHeaderKey.ToString()))
                                .Select(currentHeaderKey => new KeyValuePair<string, string>(currentHeaderKey,
                                    headers[currentHeaderKey]))
                                .Select(currentHeaderPair => (!excludeAuthorization || (!currentHeaderPair.Key.Equals(CLDefinitions.HeaderKeyAuthorization, StringComparison.InvariantCultureIgnoreCase) && !currentHeaderPair.Key.Equals(CLDefinitions.HeaderKeyProxyAuthorization, StringComparison.InvariantCultureIgnoreCase) && !currentHeaderPair.Key.Equals(CLDefinitions.HeaderKeyProxyAuthenticate, StringComparison.InvariantCultureIgnoreCase))
                                    ? currentHeaderPair
                                    : new KeyValuePair<string, string>(currentHeaderPair.Key, "---Authorization excluded---")))
                                .Concat((new Nullable<KeyValuePair<string, string>>[]
                                    {
                                        (string.IsNullOrEmpty(hostHeader) ? (Nullable<KeyValuePair<string, string>>)null : new KeyValuePair<string, string>("Host", hostHeader)),                                    
                                        (string.IsNullOrEmpty(contentLengthHeader) ? (Nullable<KeyValuePair<string, string>>)null : new KeyValuePair<string, string>("Content-Length", contentLengthHeader)),
                                        (string.IsNullOrEmpty(expectHeader) ? (Nullable<KeyValuePair<string, string>>)null : new KeyValuePair<string, string>("Expect", expectHeader)),
                                        (string.IsNullOrEmpty(connectionHeader) ? (Nullable<KeyValuePair<string, string>>)null : new KeyValuePair<string, string>("Connection", connectionHeader))
                                    })
                                    .Where(currentCustomHeader => currentCustomHeader != null)
                                    .Select(currentCustomHeader => (KeyValuePair<string, string>)currentCustomHeader))),
                        body,
                        statusCode,
                        excludeAuthorization);
                }
                catch
                {
                }
            }
        }

        public static void LogCommunication(string traceLocation, string UserDeviceId, Nullable<long> SyncBoxId, CommunicationEntryDirection Direction, string DomainAndMethodUri, bool traceEnabled = false, HttpHeaders defaultHeaders = null, HttpHeaders messageHeaders = null, HttpContent body = null, Nullable<int> statusCode = null, bool excludeAuthorization = true)
        {
            if (traceEnabled
                && !string.IsNullOrWhiteSpace(DomainAndMethodUri))
            {
                try
                {
                    string UDid = (string.IsNullOrWhiteSpace(UserDeviceId)
                        ? "NotLinked"
                        : UserDeviceId);

                    #region pullIgnoredHeaders
#pragma warning disable 665
                    Func<HttpHeaders, IEnumerable<KeyValuePair<string, string>>> pullIgnoredHeaders = originalHeaders =>
                    {
                        if (originalHeaders == null)
                        {
                            return Enumerable.Empty<KeyValuePair<string, string>>();
                        }

                        List<KeyValuePair<string, string>> ignoredHeadersList = new List<KeyValuePair<string, string>>();
                        HttpRequestHeaders castRequest = originalHeaders as HttpRequestHeaders;
                        if (castRequest != null)
                        {
                            #region ignored request headers
                            if (castRequest.Accept.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Accept",
                                    string.Join(", ",
                                        castRequest.Accept.Select(currentAccept => new KeyValuePair<MediaTypeWithQualityHeaderValue, GenericHolder<bool>>(currentAccept, new GenericHolder<bool>(false)))
                                        .Select(currentAccept =>
                                            (string.IsNullOrEmpty(currentAccept.Key.MediaType)
                                                ? string.Empty
                                                : ((currentAccept.Value.Value = true) ? currentAccept.Key.MediaType : string.Empty)) +
                                            (currentAccept.Key.Quality == null
                                                ? string.Empty
                                                : (currentAccept.Value.Value ? "; " : ((currentAccept.Value.Value = true) ? string.Empty : string.Empty)) +
                                                    "q=" + ((double)currentAccept.Key.Quality).ToString()) +
                                            (string.IsNullOrEmpty(currentAccept.Key.CharSet)
                                                ? string.Empty
                                                : (currentAccept.Value.Value ? "; " : ((currentAccept.Value.Value = true) ? string.Empty : string.Empty)) +
                                                    "charset=" + currentAccept.Key.CharSet) +
                                            string.Join(string.Empty,
                                                currentAccept.Key.Parameters.Select(currentAcceptParameter =>
                                                    (currentAcceptParameter.Name.Equals("charset", StringComparison.InvariantCultureIgnoreCase)
                                                        ? string.Empty
                                                        : (currentAccept.Value.Value ? "; " : string.Empty) +
                                                            currentAcceptParameter.Name + (currentAcceptParameter.Value == null ? string.Empty : "=" + currentAcceptParameter.Value))))))));
                            }

                            if (castRequest.AcceptCharset.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Accept-Charset",
                                    string.Join(", ",
                                        castRequest.AcceptCharset.Select(currentAcceptCharset => new KeyValuePair<StringWithQualityHeaderValue, GenericHolder<bool>>(currentAcceptCharset, new GenericHolder<bool>(false)))
                                        .Select(currentAcceptCharset =>
                                            (string.IsNullOrEmpty(currentAcceptCharset.Key.Value)
                                                ? string.Empty
                                                : ((currentAcceptCharset.Value.Value = true) ? currentAcceptCharset.Key.Value : string.Empty)) +
                                            (currentAcceptCharset.Key.Quality == null
                                                ? string.Empty
                                                : (currentAcceptCharset.Value.Value ? "; " : string.Empty) +
                                                    "q=" + ((double)currentAcceptCharset.Key.Quality).ToString())))));
                            }

                            if (castRequest.AcceptEncoding.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Accept-Encoding",
                                    string.Join(", ",
                                        castRequest.AcceptEncoding
                                        .Select(currentAcceptEncoding =>
                                            (string.IsNullOrEmpty(currentAcceptEncoding.Value)
                                                ? string.Empty
                                                : currentAcceptEncoding.Value +
                                                    (currentAcceptEncoding.Quality == null
                                                        ? string.Empty
                                                        : "; ")) +
                                            (currentAcceptEncoding.Quality == null
                                                ? string.Empty
                                                : "q=" + ((double)currentAcceptEncoding.Quality).ToString())))));
                            }

                            if (castRequest.AcceptLanguage.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Accept-Language",
                                    string.Join(", ",
                                        castRequest.AcceptLanguage
                                        .Select(currentAcceptLanguage =>
                                            (string.IsNullOrEmpty(currentAcceptLanguage.Value)
                                                ? string.Empty
                                                : currentAcceptLanguage.Value +
                                                    (currentAcceptLanguage.Quality == null
                                                        ? string.Empty
                                                        : "; ")) +
                                            (currentAcceptLanguage.Quality == null
                                                ? string.Empty
                                                : "q=" + ((double)currentAcceptLanguage.Quality).ToString())))));
                            }

                            if (castRequest.Authorization != null)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Authorization",
                                    (excludeAuthorization
                                        ? "---Authorization excluded---"
                                        : (string.IsNullOrEmpty(castRequest.Authorization.Scheme)
                                                ? string.Empty
                                                : castRequest.Authorization.Scheme + (string.IsNullOrEmpty(castRequest.Authorization.Parameter) ? string.Empty : " ")) +
                                            (string.IsNullOrEmpty(castRequest.Authorization.Parameter)
                                                ? string.Empty
                                                : castRequest.Authorization.Parameter))));
                            }

                            if (castRequest.CacheControl != null)
                            {
                                bool firstCacheControlParameterSet = false;
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Cache-Control",
                                    (castRequest.CacheControl.NoStore
                                        ? ((firstCacheControlParameterSet = true) ? "no-store" : string.Empty)
                                        : string.Empty) +
                                    (castRequest.CacheControl.NoTransform
                                        ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                            ((firstCacheControlParameterSet = true) ? "no-transform" : string.Empty)
                                        : string.Empty) +
                                    (castRequest.CacheControl.OnlyIfCached
                                        ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                            ((firstCacheControlParameterSet = true) ? "only-if-cached" : string.Empty)
                                        : string.Empty) +
                                    (castRequest.CacheControl.Public
                                        ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                            ((firstCacheControlParameterSet = true) ? "public" : string.Empty)
                                        : string.Empty) +
                                    (castRequest.CacheControl.MustRevalidate
                                        ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                            ((firstCacheControlParameterSet = true) ? "must-revalidate" : string.Empty)
                                        : string.Empty) +
                                    (castRequest.CacheControl.ProxyRevalidate
                                        ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                            ((firstCacheControlParameterSet = true) ? "proxy-revalidate" : string.Empty)
                                        : string.Empty) +
                                    (castRequest.CacheControl.NoCacheHeaders.Count > 0
                                        ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                            ((firstCacheControlParameterSet = true) ? "no-cache=\"" +
                                                string.Join(", ", castRequest.CacheControl.NoCacheHeaders) +
                                                "\"" : string.Empty)
                                        : (castRequest.CacheControl.NoCache
                                            ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "no-cache" : string.Empty)
                                            : string.Empty)) +
                                    (castRequest.CacheControl.MaxAge == null
                                        ? string.Empty
                                        : (firstCacheControlParameterSet ? ", " : string.Empty) +
                                            ((firstCacheControlParameterSet = true) ? "max-age=" + ((TimeSpan)castRequest.CacheControl.MaxAge).Seconds.ToString() : string.Empty)) +
                                    (castRequest.CacheControl.SharedMaxAge == null
                                        ? string.Empty
                                        : (firstCacheControlParameterSet ? ", " : string.Empty) +
                                            ((firstCacheControlParameterSet = true) ? "s-maxage=" + ((TimeSpan)castRequest.CacheControl.SharedMaxAge).Seconds.ToString() : string.Empty)) +
                                    (castRequest.CacheControl.MaxStaleLimit == null
                                        ? (castRequest.CacheControl.MaxStale
                                            ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "max-stale" : string.Empty)
                                            : string.Empty)
                                        : (firstCacheControlParameterSet ? ", " : string.Empty) +
                                            ((firstCacheControlParameterSet = true) ? "max-stale=" + ((TimeSpan)castRequest.CacheControl.MaxStaleLimit).Seconds.ToString() : string.Empty)) +
                                    (castRequest.CacheControl.MinFresh == null
                                        ? string.Empty
                                        : (firstCacheControlParameterSet ? ", " : string.Empty) +
                                            ((firstCacheControlParameterSet = true) ? "min-fresh=" + ((TimeSpan)castRequest.CacheControl.MinFresh).Seconds.ToString() : string.Empty)) +
                                    (castRequest.CacheControl.PrivateHeaders.Count > 0
                                        ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                            ((firstCacheControlParameterSet = true) ? "private=\"" +
                                                string.Join(",", castRequest.CacheControl.PrivateHeaders) +
                                                "\"" : string.Empty)
                                        : (castRequest.CacheControl.Private
                                            ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "private" : string.Empty)
                                            : string.Empty)) +
                                    string.Join(string.Empty,
                                        castRequest.CacheControl.Extensions.Select(currentCacheControlExtension =>
                                            (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? currentCacheControlExtension.Name + (currentCacheControlExtension.Value == null ? string.Empty : "=" + currentCacheControlExtension.Value) : string.Empty)))));
                            }

                            ignoredHeadersList.Add(new KeyValuePair<string, string>("Connection",
                                castRequest.Connection.Count > 0
                                    ? string.Join(", ", castRequest.Connection)
                                    : ((castRequest.ConnectionClose == null || !((bool)castRequest.ConnectionClose))
                                        ? "Keep-Alive"
                                        : "Close")));

                            if (castRequest.Date != null)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Date", ((DateTimeOffset)castRequest.Date).ToString("r")));
                            }

                            if (castRequest.Expect.Count > 0)
                            {
                                bool firstExpectParameterSet = false;
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Expect",
                                    string.Join(string.Empty,
                                            castRequest.Expect.Select(currentExpect =>
                                                (firstExpectParameterSet ? "; " : string.Empty) +
                                                    ((firstExpectParameterSet = true) ? currentExpect.Name + (currentExpect.Value == null ? string.Empty : "=" + currentExpect.Value) : string.Empty) +
                                                    (currentExpect.Parameters.Count > 0
                                                        ? string.Join(string.Empty,
                                                            currentExpect.Parameters.Select(currentExpectParameter =>
                                                                "; " + currentExpectParameter.Name + (currentExpectParameter.Value == null ? string.Empty : "=" + currentExpectParameter.Value)))
                                                        : string.Empty))) +
                                            ((castRequest.ExpectContinue != null && ((bool)castRequest.ExpectContinue))
                                                ? (firstExpectParameterSet ? "," : string.Empty) + "100-continue"
                                                : string.Empty)));
                            }
                            else if (castRequest.ExpectContinue == null || ((bool)castRequest.ExpectContinue))
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Expect", "100-continue"));
                            }

                            if (castRequest.From != null)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("From", castRequest.From));
                            }

                            if (castRequest.Host != null)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Host", castRequest.Host));
                            }

                            if (castRequest.IfMatch.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("If-Match",
                                    string.Join(", ",
                                        castRequest.IfMatch.Select(currentIfMatch =>
                                            (currentIfMatch.IsWeak
                                                ? "W/"
                                                : string.Empty) +
                                                currentIfMatch.Tag))));
                            }

                            if (castRequest.IfUnmodifiedSince != null)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("If-Unmodified-Since",
                                    ((DateTimeOffset)castRequest.IfUnmodifiedSince).ToString("r")));
                            }
                            else if (castRequest.IfModifiedSince != null)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("If-Modified-Since",
                                    ((DateTimeOffset)castRequest.IfModifiedSince).ToString("r")));
                            }

                            if (castRequest.IfNoneMatch.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("If-None-Match",
                                    string.Join(", ",
                                        castRequest.IfNoneMatch.Select(currentIfNoneMatch =>
                                            (currentIfNoneMatch.IsWeak
                                                ? "W/"
                                                : string.Empty) +
                                                currentIfNoneMatch.Tag))));
                            }

                            if (castRequest.IfRange != null)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("If-Range",
                                    (castRequest.IfRange.Date == null
                                        ? (castRequest.IfRange.EntityTag.IsWeak
                                            ? "W/"
                                            : string.Empty) +
                                            castRequest.IfRange.EntityTag.Tag
                                        : ((DateTimeOffset)castRequest.IfRange.Date).ToString("r"))));
                            }

                            if (castRequest.MaxForwards != null)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Max-Forwards", ((int)castRequest.MaxForwards).ToString()));
                            }

                            if (castRequest.Pragma.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Pragma",
                                    string.Join(", ",
                                        castRequest.Pragma.Select(currentPragma =>
                                            currentPragma.Name + (currentPragma.Value == null ? string.Empty : "=" + currentPragma.Value)))));
                            }

                            if (castRequest.ProxyAuthorization != null)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Proxy-Authorization",
                                    (excludeAuthorization
                                        ? "---Authorization excluded---"
                                        : (string.IsNullOrEmpty(castRequest.ProxyAuthorization.Scheme)
                                                ? string.Empty
                                                : castRequest.ProxyAuthorization.Scheme + (string.IsNullOrEmpty(castRequest.ProxyAuthorization.Parameter) ? string.Empty : " ")) +
                                            (string.IsNullOrEmpty(castRequest.ProxyAuthorization.Parameter)
                                                ? string.Empty
                                                : castRequest.ProxyAuthorization.Parameter))));
                            }

                            if (castRequest.Range != null)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Range",
                                    (castRequest.Range.Unit == null
                                        ? string.Empty
                                        : castRequest.Range.Unit + (castRequest.Range.Ranges.Count > 0 ? "=" : string.Empty)) +
                                        string.Join(",",
                                            castRequest.Range.Ranges.Select(currentRange =>
                                                (currentRange.From == null
                                                    ? string.Empty
                                                    : ((long)currentRange.From).ToString()) +
                                                    (currentRange.To == null
                                                        ? string.Empty
                                                        : "-" + ((long)currentRange.To).ToString())))));
                            }

                            if (castRequest.Referrer != null)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Referer", castRequest.Referrer.ToString()));
                            }

                            if (castRequest.TE.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("TE",
                                    string.Join(", ",
                                        castRequest.TE.Select(currentTE => new KeyValuePair<TransferCodingWithQualityHeaderValue, GenericHolder<bool>>(currentTE, new GenericHolder<bool>(false)))
                                            .Select(currentTE =>
                                                (string.IsNullOrEmpty(currentTE.Key.Value)
                                                    ? string.Empty
                                                    : (currentTE.Value.Value ? "; " : string.Empty) +
                                                        ((currentTE.Value.Value = true) ? currentTE.Key.Value : string.Empty)) +
                                                    (currentTE.Key.Quality == null
                                                        ? string.Empty
                                                        : (currentTE.Value.Value ? "; " : string.Empty) +
                                                            ((currentTE.Value.Value = true) ? ((double)currentTE.Key.Quality).ToString() : string.Empty)) +
                                                    string.Join(string.Empty,
                                                        currentTE.Key.Parameters.Select(currentTEParameter =>
                                                            (currentTE.Value.Value ? "; " : string.Empty) +
                                                                ((currentTE.Value.Value = true) ? currentTEParameter.Name + (currentTEParameter.Value == null ? string.Empty : "=" + currentTEParameter.Value) : string.Empty)))))));
                            }

                            if (castRequest.Trailer.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Trailer",
                                    string.Join(", ", castRequest.Trailer)));
                            }

                            if (castRequest.TransferEncoding.Count > 0
                                || (castRequest.TransferEncodingChunked != null && ((bool)castRequest.TransferEncodingChunked)))
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Transfer-Encoding",
                                    string.Join(", ",
                                        castRequest.TransferEncoding.Select(currentTransferEncoding => new KeyValuePair<TransferCodingHeaderValue, GenericHolder<bool>>(currentTransferEncoding, new GenericHolder<bool>(false)))
                                            .Select(currentTransferEncoding =>
                                                (string.IsNullOrEmpty(currentTransferEncoding.Key.Value)
                                                    ? string.Empty
                                                    : (currentTransferEncoding.Value.Value ? "; " : string.Empty) +
                                                        ((currentTransferEncoding.Value.Value = true) ? currentTransferEncoding.Key.Value : string.Empty)) +
                                                string.Join(string.Empty,
                                                    currentTransferEncoding.Key.Parameters.Select(currentTransferEncodingParameter =>
                                                        (currentTransferEncoding.Value.Value ? "; " : string.Empty) +
                                                            ((currentTransferEncoding.Value.Value = true) ? currentTransferEncodingParameter.Name + (currentTransferEncodingParameter.Value == null ? string.Empty : "=" + currentTransferEncodingParameter.Value) : string.Empty))))) +
                                        (castRequest.TransferEncoding.Count > 0
                                            ? string.Empty
                                            : "chunked")));
                            }

                            if (castRequest.Upgrade.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Upgrade",
                                    string.Join(", ",
                                        castRequest.Upgrade.Select(currentUpgrade => currentUpgrade.Name + (currentUpgrade.Version == null ? string.Empty : "/" + currentUpgrade.Version)))));
                            }

                            if (castRequest.UserAgent.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("User-Agent",
                                    string.Join(", ",
                                        castRequest.UserAgent.Select(currentUserAgent =>
                                            (string.IsNullOrEmpty(currentUserAgent.Comment)
                                                ? currentUserAgent.Product.Name + (currentUserAgent.Product.Version == null ? string.Empty : "/" + currentUserAgent.Product.Version)
                                                : currentUserAgent.Comment)))));
                            }

                            if (castRequest.Via.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Via",
                                    string.Join(", ",
                                        castRequest.Via.Select(currentVia => new KeyValuePair<ViaHeaderValue, GenericHolder<bool>>(currentVia, new GenericHolder<bool>(false)))
                                            .Select(currentVia =>
                                                ((currentVia.Key.ProtocolName != null || currentVia.Key.ProtocolVersion != null)
                                                    ? ((currentVia.Value.Value = true) ? currentVia.Key.ProtocolName + (currentVia.Key.ProtocolVersion == null ? string.Empty : "/" + currentVia.Key.ProtocolVersion) : string.Empty)
                                                    : string.Empty) +
                                                (string.IsNullOrEmpty(currentVia.Key.ReceivedBy)
                                                    ? string.Empty
                                                    : (currentVia.Value.Value ? " " : string.Empty) +
                                                        ((currentVia.Value.Value = true) ? currentVia.Key.ReceivedBy : string.Empty)) +
                                                ((!string.IsNullOrEmpty(currentVia.Key.ProtocolName) || currentVia.Key.ProtocolVersion != null)
                                                    ? (currentVia.Value.Value ? " " : string.Empty) +
                                                        ((currentVia.Value.Value = true) ? currentVia.Key.ProtocolName + (currentVia.Key.ProtocolVersion == null ? string.Empty : "/" + currentVia.Key.ProtocolVersion) : string.Empty)
                                                    : string.Empty) +
                                                (string.IsNullOrEmpty(currentVia.Key.Comment)
                                                    ? string.Empty
                                                    : (currentVia.Value.Value ? " " : string.Empty) +
                                                        currentVia.Key.Comment)))));
                            }

                            if (castRequest.Warning.Count > 0)
                            {
                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Warning",
                                    string.Join(", ",
                                        castRequest.Warning
                                            .Select(currentWarning =>
                                                (new string[] { currentWarning.Code.ToString() })
                                                    .Select(currentWarningCode => ("00" + currentWarningCode).Substring(Math.Min(2, currentWarningCode.Length - 1)))
                                                    .Single() +
                                                    (string.IsNullOrEmpty(currentWarning.Agent)
                                                        ? string.Empty
                                                        : " " + currentWarning.Agent) +
                                                    (string.IsNullOrEmpty(currentWarning.Text)
                                                        ? string.Empty
                                                        : " " + currentWarning.Text) +
                                                    (currentWarning.Date == null
                                                        ? string.Empty
                                                        : " " + ((DateTimeOffset)currentWarning.Date).ToString())))));
                            }
                            #endregion
                        }
                        else
                        {
                            HttpResponseHeaders castResponse = originalHeaders as HttpResponseHeaders;
                            if (castResponse != null)
                            {
                                #region ignored response headers
                                if (castResponse.AcceptRanges.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Accept-Ranges", string.Join(", ", castResponse.AcceptRanges)));
                                }

                                if (castResponse.Age != null)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Age", ((TimeSpan)castResponse.Age).TotalSeconds.ToString()));
                                }

                                if (castResponse.CacheControl != null)
                                {
                                    bool firstCacheControlParameterSet = false;
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Cache-Control",
                                        (castResponse.CacheControl.NoStore
                                            ? ((firstCacheControlParameterSet = true) ? "no-store" : string.Empty)
                                            : string.Empty) +
                                        (castResponse.CacheControl.NoTransform
                                            ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "no-transform" : string.Empty)
                                            : string.Empty) +
                                        (castResponse.CacheControl.OnlyIfCached
                                            ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "only-if-cached" : string.Empty)
                                            : string.Empty) +
                                        (castResponse.CacheControl.Public
                                            ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "public" : string.Empty)
                                            : string.Empty) +
                                        (castResponse.CacheControl.MustRevalidate
                                            ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "must-revalidate" : string.Empty)
                                            : string.Empty) +
                                        (castResponse.CacheControl.ProxyRevalidate
                                            ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "proxy-revalidate" : string.Empty)
                                            : string.Empty) +
                                        (castResponse.CacheControl.NoCacheHeaders.Count > 0
                                            ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "no-cache=\"" +
                                                    string.Join(", ", castResponse.CacheControl.NoCacheHeaders) +
                                                    "\"" : string.Empty)
                                            : (castResponse.CacheControl.NoCache
                                                ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                    ((firstCacheControlParameterSet = true) ? "no-cache" : string.Empty)
                                                : string.Empty)) +
                                        (castResponse.CacheControl.MaxAge == null
                                            ? string.Empty
                                            : (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "max-age=" + ((TimeSpan)castResponse.CacheControl.MaxAge).Seconds.ToString() : string.Empty)) +
                                        (castResponse.CacheControl.SharedMaxAge == null
                                            ? string.Empty
                                            : (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "s-maxage=" + ((TimeSpan)castResponse.CacheControl.SharedMaxAge).Seconds.ToString() : string.Empty)) +
                                        (castResponse.CacheControl.MaxStaleLimit == null
                                            ? (castResponse.CacheControl.MaxStale
                                                ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                    ((firstCacheControlParameterSet = true) ? "max-stale" : string.Empty)
                                                : string.Empty)
                                            : (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "max-stale=" + ((TimeSpan)castResponse.CacheControl.MaxStaleLimit).Seconds.ToString() : string.Empty)) +
                                        (castResponse.CacheControl.MinFresh == null
                                            ? string.Empty
                                            : (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "min-fresh=" + ((TimeSpan)castResponse.CacheControl.MinFresh).Seconds.ToString() : string.Empty)) +
                                        (castResponse.CacheControl.PrivateHeaders.Count > 0
                                            ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                ((firstCacheControlParameterSet = true) ? "private=\"" +
                                                    string.Join(",", castResponse.CacheControl.PrivateHeaders) +
                                                    "\"" : string.Empty)
                                            : (castResponse.CacheControl.Private
                                                ? (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                    ((firstCacheControlParameterSet = true) ? "private" : string.Empty)
                                                : string.Empty)) +
                                        string.Join(string.Empty,
                                            castResponse.CacheControl.Extensions.Select(currentCacheControlExtension =>
                                                (firstCacheControlParameterSet ? ", " : string.Empty) +
                                                    ((firstCacheControlParameterSet = true) ? currentCacheControlExtension.Name + (currentCacheControlExtension.Value == null ? string.Empty : "=" + currentCacheControlExtension.Value) : string.Empty)))));
                                }

                                ignoredHeadersList.Add(new KeyValuePair<string, string>("Connection",
                                    castResponse.Connection.Count > 0
                                        ? string.Join(", ", castResponse.Connection)
                                        : ((castResponse.ConnectionClose == null || !((bool)castResponse.ConnectionClose))
                                            ? "Keep-Alive"
                                            : "Close")));

                                if (castResponse.Date != null)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Date", ((DateTimeOffset)castResponse.Date).ToString("r")));
                                }

                                if (castResponse.ETag != null)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("ETag", (castResponse.ETag.IsWeak ? "W/" : string.Empty) + castResponse.ETag.Tag));
                                }

                                if (castResponse.Location != null)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Location", castResponse.Location.ToString()));
                                }

                                if (castResponse.Pragma.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Pragma",
                                        string.Join(", ",
                                            castResponse.Pragma.Select(currentPragma =>
                                                currentPragma.Name + (currentPragma.Value == null ? string.Empty : "=" + currentPragma.Value)))));
                                }

                                if (castResponse.ProxyAuthenticate.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Proxy-Authenticate",
                                        (excludeAuthorization
                                            ? "---Authorization excluded---"
                                            : string.Join(", ",
                                                castResponse.ProxyAuthenticate.Select(currentProxyAuthenticate =>
                                                    (string.IsNullOrEmpty(currentProxyAuthenticate.Scheme)
                                                        ? string.Empty
                                                        : currentProxyAuthenticate.Scheme + (string.IsNullOrEmpty(currentProxyAuthenticate.Parameter) ? string.Empty : " ")) +
                                                    currentProxyAuthenticate.Parameter)))));
                                }

                                if (castResponse.RetryAfter != null)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Retry-After",
                                        (castResponse.RetryAfter.Date == null
                                            ? string.Empty
                                            : ((DateTimeOffset)castResponse.RetryAfter.Date).ToString("r") + (castResponse.RetryAfter.Delta == null ? string.Empty : " ")) +
                                        (castResponse.RetryAfter.Delta == null
                                            ? string.Empty
                                            : ((TimeSpan)castResponse.RetryAfter.Delta).TotalSeconds.ToString())));
                                }

                                if (castResponse.Server.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Server",
                                        string.Join(", ",
                                            castResponse.Server.Select(currentServer =>
                                                (currentServer.Product == null
                                                    ? string.Empty
                                                    : currentServer.Product.Name + (currentServer.Product.Version == null ? string.Empty : "/" + currentServer.Product.Version) + (string.IsNullOrEmpty(currentServer.Comment) ? string.Empty : " ")) +
                                                currentServer.Comment))));
                                }

                                if (castResponse.Trailer.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Trailer",
                                        string.Join(", ", castResponse.Trailer)));
                                }

                                if (castResponse.TransferEncoding.Count > 0
                                    || (castResponse.TransferEncodingChunked != null && ((bool)castResponse.TransferEncodingChunked)))
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Transfer-Encoding",
                                        string.Join(", ",
                                            castResponse.TransferEncoding.Select(currentTransferEncoding => new KeyValuePair<TransferCodingHeaderValue, GenericHolder<bool>>(currentTransferEncoding, new GenericHolder<bool>(false)))
                                                .Select(currentTransferEncoding =>
                                                    (string.IsNullOrEmpty(currentTransferEncoding.Key.Value)
                                                        ? string.Empty
                                                        : (currentTransferEncoding.Value.Value ? "; " : string.Empty) +
                                                            ((currentTransferEncoding.Value.Value = true) ? currentTransferEncoding.Key.Value : string.Empty)) +
                                                    string.Join(string.Empty,
                                                        currentTransferEncoding.Key.Parameters.Select(currentTransferEncodingParameter =>
                                                            (currentTransferEncoding.Value.Value ? "; " : string.Empty) +
                                                                ((currentTransferEncoding.Value.Value = true) ? currentTransferEncodingParameter.Name + (currentTransferEncodingParameter.Value == null ? string.Empty : "=" + currentTransferEncodingParameter.Value) : string.Empty))))) +
                                            (castResponse.TransferEncoding.Count > 0
                                                ? string.Empty
                                                : "chunked")));
                                }

                                if (castResponse.Upgrade.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Upgrade",
                                        string.Join(", ",
                                            castResponse.Upgrade.Select(currentUpgrade => currentUpgrade.Name + (currentUpgrade.Version == null ? string.Empty : "/" + currentUpgrade.Version)))));
                                }

                                if (castResponse.Vary.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Vary",
                                        string.Join(", ", castResponse.Vary)));
                                }

                                if (castResponse.Via.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Via",
                                        string.Join(", ",
                                            castResponse.Via.Select(currentVia => new KeyValuePair<ViaHeaderValue, GenericHolder<bool>>(currentVia, new GenericHolder<bool>(false)))
                                                .Select(currentVia =>
                                                    ((currentVia.Key.ProtocolName != null || currentVia.Key.ProtocolVersion != null)
                                                        ? ((currentVia.Value.Value = true) ? currentVia.Key.ProtocolName + (currentVia.Key.ProtocolVersion == null ? string.Empty : "/" + currentVia.Key.ProtocolVersion) : string.Empty)
                                                        : string.Empty) +
                                                    (string.IsNullOrEmpty(currentVia.Key.ReceivedBy)
                                                        ? string.Empty
                                                        : (currentVia.Value.Value ? " " : string.Empty) +
                                                            ((currentVia.Value.Value = true) ? currentVia.Key.ReceivedBy : string.Empty)) +
                                                    ((!string.IsNullOrEmpty(currentVia.Key.ProtocolName) || currentVia.Key.ProtocolVersion != null)
                                                        ? (currentVia.Value.Value ? " " : string.Empty) +
                                                            ((currentVia.Value.Value = true) ? currentVia.Key.ProtocolName + (currentVia.Key.ProtocolVersion == null ? string.Empty : "/" + currentVia.Key.ProtocolVersion) : string.Empty)
                                                        : string.Empty) +
                                                    (string.IsNullOrEmpty(currentVia.Key.Comment)
                                                        ? string.Empty
                                                        : (currentVia.Value.Value ? " " : string.Empty) +
                                                            currentVia.Key.Comment)))));
                                }

                                if (castResponse.Warning.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Warning",
                                        string.Join(", ",
                                            castResponse.Warning
                                                .Select(currentWarning =>
                                                    (new string[] { currentWarning.Code.ToString() })
                                                        .Select(currentWarningCode => ("00" + currentWarningCode).Substring(Math.Min(2, currentWarningCode.Length - 1)))
                                                        .Single() +
                                                        (string.IsNullOrEmpty(currentWarning.Agent)
                                                            ? string.Empty
                                                            : " " + currentWarning.Agent) +
                                                        (string.IsNullOrEmpty(currentWarning.Text)
                                                            ? string.Empty
                                                            : " " + currentWarning.Text) +
                                                        (currentWarning.Date == null
                                                            ? string.Empty
                                                            : " " + ((DateTimeOffset)currentWarning.Date).ToString())))));
                                }

                                if (castResponse.WwwAuthenticate.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("WWW-Authenticat",
                                        string.Join(", ",
                                            castResponse.WwwAuthenticate.Select(currentWwwAuthenticate =>
                                                (string.IsNullOrEmpty(currentWwwAuthenticate.Scheme)
                                                    ? string.Empty
                                                    : currentWwwAuthenticate.Scheme + (string.IsNullOrEmpty(currentWwwAuthenticate.Parameter) ? string.Empty : " ")) +
                                                currentWwwAuthenticate.Parameter))));
                                }
                                #endregion
                            }
                        }
                        if (body != null)
                        {
                            if (body.Headers != null)
                            {
                                #region ignored body headers
                                if (body.Headers.Allow.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Allow",
                                        string.Join(", ",
                                            body.Headers.Allow)));
                                }

                                if (body.Headers.ContentEncoding.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Content-Encoding",
                                        string.Join(", ",
                                            body.Headers.ContentEncoding)));
                                }

                                if (body.Headers.ContentLanguage.Count > 0)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Content-Langauge",
                                        string.Join(", ",
                                            body.Headers.ContentLanguage)));
                                }

                                if (body.Headers.ContentLength != null)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Content-Length", ((long)body.Headers.ContentLength).ToString()));
                                }

                                if (body.Headers.ContentLocation != null)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Content-Location", body.Headers.ContentLocation.ToString()));
                                }

                                if (body.Headers.ContentMD5 != null)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Content-MD5",
                                        body.Headers.ContentMD5
                                            .Select(md5Byte => string.Format("{0:x2}", md5Byte))
                                            .Aggregate((previousBytes, newByte) => previousBytes + newByte)));
                                }

                                if (body.Headers.ContentRange != null)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Content-Range",
                                        (string.IsNullOrEmpty(body.Headers.ContentRange.Unit)
                                            ? string.Empty
                                            : body.Headers.ContentRange.Unit + " ") +
                                        (body.Headers.ContentRange.From == null
                                            ? string.Empty
                                            : ((long)body.Headers.ContentRange.From).ToString()) +
                                        (body.Headers.ContentRange.To == null
                                            ? string.Empty
                                            : "-" + ((long)body.Headers.ContentRange.To).ToString()) +
                                        (body.Headers.ContentRange.Length == null
                                            ? string.Empty
                                            : "/" + ((long)body.Headers.ContentRange.Length).ToString())));
                                }

                                if (body.Headers.ContentType != null)
                                {
                                    bool firstContentTypeParameterSet = false;
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Content-Type",
                                        (string.IsNullOrEmpty(body.Headers.ContentType.MediaType)
                                            ? string.Empty
                                            : ((firstContentTypeParameterSet = true) ? body.Headers.ContentType.MediaType : string.Empty)) +
                                        (string.IsNullOrEmpty(body.Headers.ContentType.CharSet)
                                            ? string.Empty
                                            : (firstContentTypeParameterSet ? "; " : ((firstContentTypeParameterSet = true) ? string.Empty : string.Empty)) +
                                                "charset=" + body.Headers.ContentType.CharSet) +
                                        string.Join(string.Empty,
                                            body.Headers.ContentType.Parameters.Select(currentBodyContentTypeParameter =>
                                                (currentBodyContentTypeParameter.Name.Equals("charset", StringComparison.InvariantCultureIgnoreCase)
                                                    ? string.Empty
                                                    : (firstContentTypeParameterSet ? "; " : ((firstContentTypeParameterSet = true) ? string.Empty : string.Empty)) +
                                                        currentBodyContentTypeParameter.Name + (currentBodyContentTypeParameter.Value == null ? string.Empty : "=" + currentBodyContentTypeParameter.Value))))));
                                }

                                if (body.Headers.Expires != null)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Expires", ((DateTimeOffset)body.Headers.Expires).ToString("r")));
                                }

                                if (body.Headers.LastModified != null)
                                {
                                    ignoredHeadersList.Add(new KeyValuePair<string, string>("Last-Modified", ((DateTimeOffset)body.Headers.LastModified).ToString("r")));
                                }
                                #endregion
                            }
                        }

                        return ignoredHeadersList;
                    };
#pragma warning restore 665
                    #endregion

                    LogCommunication(traceLocation,
                        UDid,
                        SyncBoxId,
                        Direction,
                        DomainAndMethodUri,
                        ((defaultHeaders == null && messageHeaders == null)
                            ? null
                            : (defaultHeaders ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
                                .Select(currentDefaultHeader => new KeyValuePair<string, string>(currentDefaultHeader.Key, string.Join(",", currentDefaultHeader.Value)))
                                .Concat((messageHeaders ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
                                    .Select(currentMessageHeader => new KeyValuePair<string, string>(currentMessageHeader.Key, string.Join(",", currentMessageHeader.Value))))
                                .Select(currentHeaderPair => (!excludeAuthorization || (!currentHeaderPair.Key.Equals(CLDefinitions.HeaderKeyAuthorization, StringComparison.InvariantCultureIgnoreCase) && !currentHeaderPair.Key.Equals(CLDefinitions.HeaderKeyProxyAuthorization, StringComparison.InvariantCultureIgnoreCase) && !currentHeaderPair.Key.Equals(CLDefinitions.HeaderKeyProxyAuthenticate, StringComparison.InvariantCultureIgnoreCase))
                                    ? currentHeaderPair
                                    : new KeyValuePair<string, string>(currentHeaderPair.Key, "---Authorization excluded---")))
                                .Concat(pullIgnoredHeaders(defaultHeaders))
                                .Concat(pullIgnoredHeaders(messageHeaders))),
                        (body == null
                            ? null
                            : body.ReadAsString()),
                        statusCode,
                        excludeAuthorization);
                }
                catch
                {
                }
            }
        }

        public static void LogFileChangeFlow(string traceLocation, string UserDeviceId, Nullable<long> SyncBoxId, FileChangeFlowEntryPositionInFlow position, IEnumerable<FileChange> changes)
        {
            try
            {
                FileChange[] changesArray = (changes ?? Enumerable.Empty<FileChange>()).ToArray();

                TraceFileChange[] traceChangesArray = (changesArray.Length == 0 ? null : new TraceFileChange[changesArray.Length]);

                Entry newEntry = new FileChangeFlowEntry()
                {
                    Type = (int)TraceType.FileChangeFlow,
                    Time = DateTime.UtcNow,
                    ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
                    ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId,
                    SyncBoxId = SyncBoxId ?? 0,
                    SyncBoxIdSpecified = SyncBoxId != null,
                    PositionInFlow = position,
                    FileChanges = traceChangesArray
                };

                if (changesArray.Length > 0)
                {
                    Action<object, TraceFileChange[], int, FileChange> fillIndexInTraceFileChangeArray = (thisAction, currentArray, currentIndex, currentChange) =>
                    {
                        Action<object, TraceFileChange[], int, FileChange> castThisAction = thisAction as Action<object, TraceFileChange[], int, FileChange>;
                        if (castThisAction != null)
                        {
                            FileChangeWithDependencies currentChangeWithDependencies = currentChange as FileChangeWithDependencies;

                            TraceFileChange[] innerTraceArray;
                            if (currentChangeWithDependencies != null
                                && currentChangeWithDependencies.DependenciesCount > 0)
                            {
                                innerTraceArray = new TraceFileChange[currentChangeWithDependencies.DependenciesCount];
                            }
                            else
                            {
                                innerTraceArray = null;
                            }

                            TraceFileChangeType convertedCurrentType;
                            switch (currentChange.Type)
                            {
                                case FileChangeType.Created:
                                    convertedCurrentType = TraceFileChangeType.Created;
                                    break;
                                case FileChangeType.Deleted:
                                    convertedCurrentType = TraceFileChangeType.Deleted;
                                    break;
                                case FileChangeType.Modified:
                                    convertedCurrentType = TraceFileChangeType.Modified;
                                    break;
                                case FileChangeType.Renamed:
                                    convertedCurrentType = TraceFileChangeType.Renamed;
                                    break;
                                default:
                                    throw new NotSupportedException("Unknown FileChangeType: " + currentChange.Type.ToString());
                            }

                            currentArray[currentIndex] = new TraceFileChange()
                            {
                                ServerId = currentChange.Metadata.ServerId,
                                EventId = currentChange.EventId,
                                EventIdSpecified = currentChange.EventId != 0,
                                NewPath = currentChange.NewPath.ToString(),
                                OldPath = (currentChange.OldPath == null ? null : currentChange.OldPath.ToString()),
                                IsFolder = currentChange.Metadata.HashableProperties.IsFolder,
                                Type = convertedCurrentType,
                                LastTime = currentChange.Metadata.HashableProperties.LastTime.ToUniversalTime(),
                                LastTimeSpecified = currentChange.Metadata.HashableProperties.LastTime.Ticks != FileConstants.InvalidUtcTimeTicks,
                                CreationTime = currentChange.Metadata.HashableProperties.CreationTime.ToUniversalTime(),
                                CreationTimeSpecified = currentChange.Metadata.HashableProperties.CreationTime.Ticks != FileConstants.InvalidUtcTimeTicks,
                                Size = (currentChange.Metadata.HashableProperties.Size == null ? 0L : (long)currentChange.Metadata.HashableProperties.Size),
                                SizeSpecified = currentChange.Metadata.HashableProperties.Size != null,
                                IsSyncFrom = IsSyncFromBySyncDirection(currentChange.Direction),
                                MD5 = PullMD5(currentChange),
                                LinkTargetPath = (currentChange.Metadata.LinkTargetPath == null ? null : currentChange.Metadata.LinkTargetPath.ToString()),
                                Revision = currentChange.Metadata.Revision,
                                StorageKey = currentChange.Metadata.StorageKey,
                                Dependencies = innerTraceArray
                            };

                            if (innerTraceArray != null)
                            {
                                FileChange[] innerDependencies = currentChangeWithDependencies.Dependencies;

                                for (int innerDependencyIndex = 0; innerDependencyIndex < innerDependencies.Length; innerDependencyIndex++)
                                {
                                    FileChange nextChange = innerDependencies[innerDependencyIndex];

                                    if (nextChange == currentChange)
                                    {
                                        throw new ArgumentException("A stack overflow would occur if processing continued, a FileChange being logged is parent to itself");
                                    }

                                    castThisAction(castThisAction, innerTraceArray, innerDependencyIndex, nextChange);
                                }
                            }
                        }
                    };

                    for (int outerChangeIndex = 0; outerChangeIndex < changesArray.Length; outerChangeIndex++)
                    {
                        fillIndexInTraceFileChangeArray(fillIndexInTraceFileChangeArray, traceChangesArray, outerChangeIndex, changesArray[outerChangeIndex]);
                    }
                }

                WriteLogEntry(newEntry, traceLocation, UserDeviceId, SyncBoxId);
            }
            catch
            {
            }
        }
        #endregion

        #region private methods
        // the calling method should wrap this private helper in a try/catch
        private static void LogCommunication(string traceLocation, string UserDeviceId, Nullable<long> SyncBoxId, CommunicationEntryDirection Direction, string DomainAndMethodUri, IEnumerable<KeyValuePair<string, string>> headers = null, string body = null, Nullable<int> statusCode = null, bool excludeAuthorization = true)
        {
            if (!string.IsNullOrEmpty(body)
                && excludeAuthorization)
            {
                try
                {
                    string trimJson = (body ?? string.Empty).Trim();

                    if ((trimJson.StartsWith("{") && trimJson.EndsWith("}"))
                        || (trimJson.StartsWith("[") && trimJson.EndsWith("]")))
                    {
                        Dictionary<string, object> bodyDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
                        if (bodyDict != null
                            && bodyDict.ContainsKey(CLDefinitions.CLRegistrationAccessTokenKey))
                        {
                            bodyDict[CLDefinitions.CLRegistrationAccessTokenKey] = "---Access token excluded---";
                            body = Newtonsoft.Json.JsonConvert.SerializeObject(bodyDict);
                        }
                    }
                }
                catch
                {
                }
            }

            Entry newEntry = new CommunicationEntry()
            {
                Type = (int)TraceType.Communication,
                Time = DateTime.UtcNow,
                ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
                ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId,
                SyncBoxId = SyncBoxId ?? 0,
                SyncBoxIdSpecified = SyncBoxId != null,
                Direction = Direction,
                Uri = DomainAndMethodUri,
                Headers = (headers == null
                    ? null
                    : headers
                        .DistinctBy(keySelector => keySelector.Key)
                        .Select(currentHeader => new CommunicationEntryHeader()
                        {
                            Key = currentHeader.Key,
                            Value = currentHeader.Value
                        }).ToArray()),
                Body = body,
                StatusCodeSpecified = statusCode != null,
                StatusCode = (statusCode == null
                    ? Helpers.DefaultForType<int>()
                    : (int)statusCode)
            };

            WriteLogEntry(newEntry, traceLocation, UserDeviceId, SyncBoxId);
        }
        // the calling method should wrap this private helper in a try/catch
        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void WriteLogEntry(Entry logEntry, string traceLocation, string UserDeviceId, Nullable<long> SyncBoxId)
        {
            string logLocation = Helpers.CheckLogFileExistance(traceLocation, SyncBoxId, UserDeviceId, "Sync", "xml",
                new Action<TextWriter, string, Nullable<long>, string>((logWriter, finalLocation, innerSyncBoxId, innerDeviceId) =>
                {
                    logWriter.Write(LogXmlStart(finalLocation, "UDid: {" + innerDeviceId + "}, SyncBoxId: {" + innerSyncBoxId + "}"));
                }),
                new Action<TextWriter>((logWriter) =>
                {
                    logWriter.Write(Environment.NewLine + "</Log>");
                }));

            lock (Helpers.LogFileLocker)
            {
                XmlWriterSettings logWriterSettings = new XmlWriterSettings();
                logWriterSettings.OmitXmlDeclaration = true;
                logWriterSettings.NewLineChars = Environment.NewLine + "  ";
                logWriterSettings.Encoding = Encoding.UTF8;
                logWriterSettings.Indent = true;
                using (TextWriter logWriter = File.AppendText(logLocation))
                {
                    logWriter.Write(Environment.NewLine + "  ");
                    using (XmlWriter logXmlWriter = XmlWriter.Create(logWriter, logWriterSettings))
                    {
                        LogEntryTypeSerializer.Serialize(logXmlWriter, logEntry);
                    }
                }
            }
        }
        private static XmlSerializer LogEntryTypeSerializer
        {
            get
            {
                lock (FileCreationSerializerLocker)
                {
                    if (_fileCreationSerializer == null)
                    {
                        _fileCreationSerializer = new XmlSerializer(typeof(Entry));
                    }
                    return _fileCreationSerializer;
                }
            }
        }
        private static XmlSerializer _fileCreationSerializer = null;
        private static object FileCreationSerializerLocker = new object();
        private static string LogXmlStart(string fileName, string creator)
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine +
                "<Log xmlns=\"http://www.cloud.com/TraceLog.xsd\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
                "  <Copyright>" + Environment.NewLine +
                "    <FileName>" + fileName + "</FileName>" + Environment.NewLine +
                "    <Copyright>Implementation of TraceLog.xsd XML Schema. Cloud. Copyright (c) Cloud.com. All rights reserved.</Copyright>" + Environment.NewLine +
                "    <Creator>" + creator + "</Creator>" + Environment.NewLine +
                "  </Copyright>";
        }
        private static bool IsSyncFromBySyncDirection(SyncDirection direction)
        {
            switch (direction)
            {
                case SyncDirection.From:
                    return true;
                case SyncDirection.To:
                    return false;
                default:
                    throw new ArgumentException("Unknown SyncDirection: " + direction.ToString());
            }
        }
        private static string PullMD5(FileChange toPull)
        {
            string toReturn;
            CLError getMD5Error = toPull.GetMD5LowercaseString(out toReturn);
            if (getMD5Error != null)
            {
                throw new AggregateException("Failed to retrieve MD5 lowercase string", getMD5Error.GrabExceptions());
            }
            return toReturn;
        }
        #endregion
    }
}