//
//  CLContextMenuService.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.Support;
using ContextMenuNET;
using Cloud.Model;
using CloudApiPrivate.Model.Settings;

namespace win_client.Services.ContextMenu
{
    public sealed class CLContextMenuService
    {
        private static CLContextMenuService _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;

        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static CLContextMenuService Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new CLContextMenuService();

                        // Initialize at first Instance access here
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private CLContextMenuService()
        {
            // Initialize members, etc. here (at static initialization time).
        }

        /// <summary>
        /// Start the context menu service.
        /// </summary>
        public void BeginContextMenuServices()
        {
            bool isContextMenuServerInitialized;
            CLError contextMenuServerInitializedError = ContextMenuServer.IsContextMenuServerInitialized(out isContextMenuServerInitialized);
            if (contextMenuServerInitializedError != null)
            {
                _trace.writeToLog(1, "CLContextMenuService: BeginContextMenuServices: ERROR: From ContextMenuServer.IsBadingInitialized. Msg: <{0}>. Code: {1}.", contextMenuServerInitializedError.errorDescription, contextMenuServerInitialized((int)error.code).ToString());
            }
            else
            {
                if (!isContextMenuServerInitialized)
                {
                    CLError initializeError = ContextMenuServer.Initialize(CLSettingsSync.Instance);
                    if (initializeError != null)
                    {
                        _trace.writeToLog(1, "CLContextMenuService: BeginContextMenuServices: ERROR: From ContextMenuServer.Initialize. Msg: <{0}>. Code: {1}.", initializeError.errorDescription, initialize((int)error.code).ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Stop the context menu service.
        /// </summary>
        public void EndContextMenuServices()
        {
            CLError error = ContextMenuServer.Shutdown();
            if (error != null)
            {
                _trace.writeToLog(1, "CLContextMenuService: EndContextMenuServices: ERROR: From ContextMenuServer.Shutdown. Msg: <{0}>. Code: {1}.", error.errorDescription, ((int)error.code).ToString());
            }
        }
    }
}
