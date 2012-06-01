//
//  CLIndexingService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Support;
using CloudApiPublic.Model;

namespace win_client.Services.Indexing
{
    public enum CLEventOrigin
    {
        CLEventOriginMDS = 0,
        CLEventOriginFSM = 1,    
    };

    public enum CLIndexEventType
    {
        CLIndexEventTypeAdd = 0,
        CLIndexEventTypeModify,
        CLIndexEventTypeRenameMove,
        CLIndexEventTypeDelete,    
     };

    public sealed class CLIndexingService
    {
        private static CLIndexingService _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLIndexingService Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLIndexingService();

                        // Initialize at first Instance access here
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLIndexingService()
        {
            // Initialize members, etc. here (at static initialization time).
            _trace = CLTrace.Instance;
        }

        /// <summary>
        /// Start the indexing service.
        /// </summary>
        public void StartIndexingService()
        {

        }
    }
}