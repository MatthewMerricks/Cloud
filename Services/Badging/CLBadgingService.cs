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
using CloudApiPublic.Support;
using BadgeNET;
using CloudApiPublic.Model;
using CloudApiPrivate.Model.Settings;

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
            if (badgingInitializedError != null)
            {
                _trace.writeToLog(1, String.Format("CLBadgingServices: BeginBadgingServices: ERROR: From IconOverlay.IsBadingInitialized. Msg: <{0}>. Code: {1}.", badgingInitializedError.errorDescription, badgingInitializedError.errorCode));
            }
            else
            {
                if (!isBadgingInitialized)
                {
                    CLError initializeError = IconOverlay.Initialize(Settings.Instance.CloudFolderPath);
                    if (initializeError != null)
                    {
                        _trace.writeToLog(1, String.Format("CLBadgingServices: BeginBadgingServices: ERROR: From IconOverlay.Initialize. Msg: <{0}>. Code: {1}.", initializeError.errorDescription, initializeError.errorCode));
                    }
                }
            }
        }

        /// <summary>
        /// Stop the badging service.
        /// </summary>
        public void EndBadgingServices()
        {
            CLError error = IconOverlay.Shutdown();
            if (error != null)
            {
                _trace.writeToLog(1, String.Format("CLBadgingServices: EndBadgingServices: ERROR: From IconOverlay.Shutdonw. Msg: <{0}>. Code: {1}.", error.errorDescription, error.errorCode));
            }
        }
    }
}
