﻿//
//  CLBadgingService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudApiPublic.Support;
using BadgeNET;
using CloudApiPublic.Model;

namespace win_client.Services.Badging
{
    public sealed class CLBadgingService
    {
        private static CLBadgingService _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;

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
        }

        /// <summary>
        /// Start the badging service.
        /// </summary>
        public void BeginBadgingServices()
        {
            bool isBadgingInitialized;
            CLError badgingInitializedError = IconOverlay.IsBadgingInitialized(out isBadgingInitialized);
            // error not handled above
            if(!isBadgingInitialized)
            {
                CLError initializeError = IconOverlay.Initialize();
                // error not handled above
            }
        }

        /// <summary>
        /// Stop the badging service.
        /// </summary>
        public void EndBadgingServices()
        {
            IconOverlay.Shutdown();
        }
    }
}
