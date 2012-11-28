//
// folder_objects.cs
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
    public sealed class folder_objects : HttpActionProcessor
    {
        #region singleton pattern
        public static folder_objects Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new folder_objects());
                }
            }
        }
        private static folder_objects _instance = null;
        private static readonly object InstanceLocker = new object();

        private folder_objects() { }
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

                queryString.Add("IsFolder", "true");

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