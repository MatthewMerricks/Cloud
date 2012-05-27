//
//  CLCFMonitoringService.cs
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
    public sealed class CLCFMonitoringService
    {
        private static CLCFMonitoringService _instance = null;
        private static object _instanceLocker = new object();
        private static CLSptTrace _trace;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLCFMonitoringService Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLCFMonitoringService();

                        // Initialize at first Instance access here
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLCFMonitoringService()
        {
            // Initialize members, etc. here (at static initialization time).
            _trace = CLSptTrace.Instance;
        }

        /// <summary>
        /// Start the Cloud folder monitoring service.
        /// </summary>
        public void BeginCloudFolderMonitoring()
        {

        }

        /// <summary>
        /// Stop the Cloud folder monitoring service.
        /// </summary>
        public void EndCloudFolderMonitoring()
        {

        }
    }
}
