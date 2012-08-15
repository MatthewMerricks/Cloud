//
// Trace.cs
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
using System.Xml.Serialization;
using CloudApiPublic.Model;

namespace CloudApiPublic.Static
{
    public static class Trace
    {
        public static void LogCommunication(string traceLocation, string UserDeviceId, string UniqueUserId, CommunicationEntryDirection Direction, string DomainAndMethodUri, bool traceEnabled = false, WebHeaderCollection headers = null, Stream body = null, bool excludeAuthorization = true)
        {
            string bodyString = null;
            if (traceEnabled
                && body != null)
            {
                try
                {
                    using (TextReader textStream = new StreamReader(body, Encoding.UTF8))
                    {
                        bodyString = textStream.ReadToEnd();
                    }
                    body.Seek(0, SeekOrigin.Begin);
                }
                catch
                {
                }
            }
            LogCommunication(traceLocation, UserDeviceId, UniqueUserId, Direction, DomainAndMethodUri, traceEnabled, headers, bodyString, excludeAuthorization);
        }

        public static void LogCommunication(string traceLocation, string UserDeviceId, string UniqueUserId, CommunicationEntryDirection Direction, string DomainAndMethodUri, bool traceEnabled = false, WebHeaderCollection headers = null, string body = null, bool excludeAuthorization = true)
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
                        UniqueUserId,
                        Direction,
                        DomainAndMethodUri,
                        (headers == null
                            ? null
                            : headers.Keys.OfType<object>()
                                .Select(currentHeaderKey => (currentHeaderKey == null ? null : currentHeaderKey.ToString()))
                                .Where(currentHeaderKey => !string.IsNullOrWhiteSpace(currentHeaderKey) && (!excludeAuthorization || !currentHeaderKey.Equals(CLDefinitions.HeaderKeyAuthorization, StringComparison.InvariantCultureIgnoreCase)))
                                .Select(currentHeaderKey => new KeyValuePair<string, string>(currentHeaderKey,
                                    headers[currentHeaderKey]))),
                        body);
                }
                catch
                {
                }
            }
        }

        public static void LogCommunication(string traceLocation, string UserDeviceId, string UniqueUserId, CommunicationEntryDirection Direction, string DomainAndMethodUri, bool traceEnabled = false, HttpHeaders defaultHeaders = null, HttpHeaders messageHeaders = null, HttpContent body = null, bool excludeAuthorization = true)
        {
            if (traceEnabled
                && !string.IsNullOrWhiteSpace(DomainAndMethodUri))
            {
                try
                {
                    string UDid = (string.IsNullOrWhiteSpace(UserDeviceId)
                        ? "NotLinked"
                        : UserDeviceId);

                    LogCommunication(traceLocation,
                        UDid,
                        UniqueUserId,
                        Direction,
                        DomainAndMethodUri,
                        ((defaultHeaders == null && messageHeaders == null)
                            ? null
                            : (defaultHeaders ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
                                .Select(currentDefaultHeader => new KeyValuePair<string, string>(currentDefaultHeader.Key, string.Join(",", currentDefaultHeader.Value)))
                                .Concat((messageHeaders ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
                                    .Select(currentMessageHeader => new KeyValuePair<string, string>(currentMessageHeader.Key, string.Join(",", currentMessageHeader.Value))))
                                .Where(currentHeaderPair => !excludeAuthorization || !currentHeaderPair.Key.Equals(CLDefinitions.HeaderKeyAuthorization))),
                        (body == null
                            ? null
                            : body.ReadAsString()));
                }
                catch
                {
                }
            }
        }

        // the calling method should wrap this private helper in a try/catch
        private static void LogCommunication(string traceLocation, string UserDeviceId, string UniqueUserId, CommunicationEntryDirection Direction, string DomainAndMethodUri, IEnumerable<KeyValuePair<string, string>> headers = null, string body = null)
        {
            LogEntryType newEntry = new CommunicationEntry()
            {
                Type = (int)TraceType.Communication,
                Time = DateTime.UtcNow,
                ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
                ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId,
                Direction = Direction,
                Uri = DomainAndMethodUri,
                Headers = (headers == null
                    ? null
                    : headers
                        .Select(currentHeader => new CommunicationEntryHeader()
                        {
                            Key = currentHeader.Key,
                            Value = currentHeader.Value
                        })).ToArray(),
                Body = body
            };

            string logLocation = CheckLogFileExistance(traceLocation, UserDeviceId, UniqueUserId);

            lock (LogFileLocker)
            {
                using (TextWriter logWriter = File.CreateText(logLocation))
                {
                    logWriter.WriteLine();
                    LogEntryTypeSerializer.Serialize(logWriter, newEntry);
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
                        _fileCreationSerializer = new XmlSerializer(typeof(LogEntryType));
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
                "<Log xmlns=\"http://www.cloud.com/TraceLog.xsd\">" + Environment.NewLine +
                "  <Copyright>" + Environment.NewLine +
                "    <FileName>" + fileName + "</FileName>" + Environment.NewLine +
                "    <Copyright>Implementation of TraceLog.xsd XML Schema. Cloud. Copyright (c) Cloud.com. All rights reserved.</Copyright>" + Environment.NewLine +
                "    <Creator>" + creator + "</Creator>" + Environment.NewLine +
                "  </Copyright>";
        }
        private static readonly object LogFileLocker = new object();
        // the calling method should wrap this private helper in a try/catch
        private static string CheckLogFileExistance(string traceLocation, string UserDeviceId, string UniqueUserId)
        {
            string logFileName = UserDeviceId + ".xml";
            string logLocation = traceLocation + "\\" + logFileName;

            lock (LogFileLocker)
            {
                bool logAlreadyExists = File.Exists(logLocation);

                using (TextWriter logWriter = File.CreateText(logLocation))
                {
                    if (!logAlreadyExists)
                    {
                        logWriter.Write(LogXmlStart(logFileName, "UDid: {" + UserDeviceId + "}, UUid: {" + UniqueUserId + "}"));
                    }
                }
            }

            return logLocation;
        }
    }
}