//
//  CLUIActivityService.cs
//  (was CLAgentService.cs)
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApi.Support;

namespace win_client.Services.UiActivity
{
    public sealed class CLUIActivityService
    {
        private static CLUIActivityService _instance = null;
        private static object _instanceLocker = new object();
        private static CLSptTrace _trace;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLUIActivityService Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLUIActivityService();

                        // Initialize at first Instance access here
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLUIActivityService()
        {
            // Initialize members, etc. here (at static initialization time).
            _trace = CLSptTrace.Instance;
        }

        /// <summary>
        /// Start the UI activity service.
        /// </summary>
        public void BeginUIActivityService()
        {

        }

        /// <summary>
        /// End the UI activity service.
        /// </summary>
        public void EndUIActivityService()
        {

        }
    }
}
