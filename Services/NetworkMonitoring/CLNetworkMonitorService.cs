//
//  CLNetworkMonitorService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Support;

namespace win_client.Services.Badging
{
    public sealed class CLNetworkMonitorService
    {
        private static CLNetworkMonitorService _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLNetworkMonitorService Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLNetworkMonitorService();

                        // Initialize at first Instance access here
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLNetworkMonitorService()
        {
            // Initialize members, etc. here (at static initialization time).
            _trace = CLTrace.Instance;
        }

        /// <summary>
        /// Start the network monitoring service.
        /// </summary>
        public void BeginNetworkMonitoring()
        {

        }

        /// <summary>
        /// Stop the network monitoring service.
        /// </summary>
        public void EndNetworkMonitoring()
        {

        }
    }
}
