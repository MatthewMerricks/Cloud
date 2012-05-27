//
//  CLBadgingService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApi.Support;

namespace win_client.Services.Badging
{
    public sealed class CLBadgingService
    {
        private static CLBadgingService _instance = null;
        private static object _instanceLocker = new object();
        private static CLSptTrace _trace;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLBadgingService Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLBadgingService();

                        // Initialize at first Instance access here
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLBadgingService()
        {
            // Initialize members, etc. here (at static initialization time).
            _trace = CLSptTrace.Instance;
        }

        /// <summary>
        /// Start the badging service.
        /// </summary>
        public void BeginBadgingServices()
        {
        
        }

        /// <summary>
        /// Stop the badging service.
        /// </summary>
        public void EndBadgingServices()
        {

        }
    }
}
