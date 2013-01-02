//
// JsonContractHelpers.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CloudApiPublic.JsonContracts
{
    internal static class JsonContractHelpers
    {
        public static DataContractJsonSerializer PushSerializer
        {
            get
            {
                lock (PushSerializerLocker)
                {
                    return _pushSerializer
                        ?? (_pushSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Push)));
                }
            }
        }
        private static DataContractJsonSerializer _pushSerializer = null;
        private static readonly object PushSerializerLocker = new object();

        public static DataContractJsonSerializer PushResponseSerializer
        {
            get
            {
                lock (PushResponseSerializerLocker)
                {
                    return _pushResponseSerializer
                        ?? (_pushResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.PushResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _pushResponseSerializer = null;
        private static readonly object PushResponseSerializerLocker = new object();

        public static DataContractJsonSerializer DownloadSerializer
        {
            get
            {
                lock (DownloadSerializerLocker)
                {
                    return _downloadSerializer
                        ?? (_downloadSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Download)));
                }
            }
        }
        private static DataContractJsonSerializer _downloadSerializer = null;
        private static readonly object DownloadSerializerLocker = new object();

        public static DataContractJsonSerializer GetMetadataResponseSerializer
        {
            get
            {
                lock (GetMetadataResponseSerializerLocker)
                {
                    return _getMetadataResponseSerializer
                        ?? (_getMetadataResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.Metadata)));
                }
            }
        }
        private static DataContractJsonSerializer _getMetadataResponseSerializer = null;
        private static readonly object GetMetadataResponseSerializerLocker = new object();

        public static DataContractJsonSerializer ToSerializer
        {
            get
            {
                lock (ToSerializerLocker)
                {
                    return _toSerializer
                        ?? (_toSerializer = new DataContractJsonSerializer(typeof(JsonContracts.To)));
                }
            }
        }
        private static DataContractJsonSerializer _toSerializer = null;
        private static readonly object ToSerializerLocker = new object();

        public static DataContractJsonSerializer NotificationResponseSerializer
        {
            get
            {
                lock (NotificationResponseSerializerLocker)
                {
                    return _notificationResponseSerializer
                        ?? (_notificationResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.NotificationResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _notificationResponseSerializer = null;
        private static readonly object NotificationResponseSerializerLocker = new object();

        public static string NotificationResponseToJSON(JsonContracts.NotificationResponse notificationResponse)
        {
            using (MemoryStream stringStream = new MemoryStream())
            {
                NotificationResponseSerializer.WriteObject(stringStream, notificationResponse);
                stringStream.Position = 0;
                using (StreamReader stringReader = new StreamReader(stringStream))
                {
                    return stringReader.ReadToEnd();
                }
            }
        }

        public static JsonContracts.NotificationResponse ParseNotificationResponse(string notificationResponse)
        {
            MemoryStream stringStream = null;
            try
            {
                stringStream = new MemoryStream(Encoding.Unicode.GetBytes(notificationResponse));
                return (JsonContracts.NotificationResponse)NotificationResponseSerializer.ReadObject(stringStream);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (stringStream != null)
                {
                    stringStream.Dispose();
                }
            }
        }

        public static DataContractJsonSerializer PurgePendingSerializer
        {
            get
            {
                lock (PurgePendingSerializerLocker)
                {
                    return _purgePendingSerializer
                        ?? (_purgePendingSerializer = new DataContractJsonSerializer(typeof(JsonContracts.PurgePending)));
                }
            }
        }
        private static DataContractJsonSerializer _purgePendingSerializer = null;
        private static readonly object PurgePendingSerializerLocker = new object();

        public static DataContractJsonSerializer PendingResponseSerializer
        {
            get
            {
                lock (PendingResponseSerializerLocker)
                {
                    return _pendingResponseSerializer
                        ?? (_pendingResponseSerializer = new DataContractJsonSerializer(typeof(JsonContracts.PendingResponse)));
                }
            }
        }
        private static DataContractJsonSerializer _pendingResponseSerializer = null;
        private static readonly object PendingResponseSerializerLocker = new object();
    }
}