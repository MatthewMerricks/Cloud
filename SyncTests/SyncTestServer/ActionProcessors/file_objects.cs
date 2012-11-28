//
// file_objects.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;

namespace SyncTestServer.ActionProcessors
{
    public sealed class file_objects : HttpActionProcessor
    {
        #region singleton pattern
        public static file_objects Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new file_objects());
                }
            }
        }
        private static file_objects _instance = null;
        private static readonly object InstanceLocker = new object();

        private file_objects() { }
        #endregion

        private static readonly HttpActionSubprocessor[] ActionSubprocessors = new HttpActionSubprocessor[]
        {
            global::SyncTestServer.SubProcessors.metadata.Instance
        };
        private readonly Dictionary<string, HttpActionSubprocessor> MethodNameToActionSubprocessor = ActionSubprocessors.ToDictionary(innerMethod => innerMethod.InnerMethod,
            StringComparer.InvariantCultureIgnoreCase);

        public override string HttpPrefix
        {
            get
            {
                lock (HttpPrefixLocker)
                {
                    return _httpPrefix
                        ?? (_httpPrefix = (CLDefinitions.CLMetaDataServerURL.EndsWith("/")
                                ? CLDefinitions.CLMetaDataServerURL
                                : CLDefinitions.CLMetaDataServerURL + "/") +
                            this.GetType().Name + "/");
                }
            }
        }
        private string _httpPrefix = null;
        private readonly object HttpPrefixLocker = new object();

        public override bool ProcessContext(HttpListenerContext toProcess, IServerData serverData, string listenerFirstPath, string listenerFullPath, NameValueCollection queryString)
        {
            string innerMethodName = listenerFullPath.Substring(listenerFirstPath.Length);

            HttpActionSubprocessor subProcessor;
            if (MethodNameToActionSubprocessor.TryGetValue(innerMethodName, out subProcessor))
            {
                if (queryString == null)
                {
                    queryString = new NameValueCollection();
                }

                queryString.Add("IsFolder", "false");

                subProcessor.ProcessContext(toProcess, serverData, queryString);

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}