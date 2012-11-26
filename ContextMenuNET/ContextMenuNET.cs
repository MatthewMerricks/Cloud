//
// ContextMenuNET.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.IO;
using CloudApiPublic.Support;
using System.Runtime.InteropServices;
using CloudApiPublic.Interfaces;

namespace ContextMenuNET
{
    /// <summary>
    /// ContextMenuServer listens for context menu messages from the BadgeCOM shell extension.  The messages are sent when the user
    /// has selected one or more files or folders in Explorer, and has selected the primary context menu "Share with Cloud", or the
    /// secondary Send-To context menu item "Share with Cloud".  This server receives the event and the list of files/folders.
    /// The files and/or folders are copied to the Cloud folder root.
    /// </summary>
    public sealed class ContextMenuServer : IDisposable
    {
        #region Singleton pattern
        /// <summary>
        /// Access all ContextMenuServer public methods via this object reference
        /// </summary>
        private static ContextMenuServer Instance
        {
            get
            {
                // ensure instance is only created once
                lock (InstanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new ContextMenuServer();
                    }
                    return _instance;
                }
            }
        }
        private static ContextMenuServer _instance = null;
        private static object InstanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;

        // Constructor for Singleton pattern is private
        private ContextMenuServer()
        {
        }

        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~ContextMenuServer()
        {
            this.Dispose(false);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Initialize ContextMenuServer badge COM object processing with or without initial list
        /// (initial list can be added later by a call to InitializeOrReplace)
        /// ¡¡ Do not call this method a second time nor after InitializeOrReplace has been called !!
        /// </summary>
        /// <param name="syncSettings">The settings to use for this singleton.</param>
        public static CLError Initialize(ISyncSettings syncSettings)
        {
            try
            {
                if (syncSettings == null)
                {
                    throw new NullReferenceException("syncSettings cannot be null");
                }

                _trace.writeToLog(9, "ContextMenuServer: Initialize: Entry.");
                return Instance.pInitialize(syncSettings.CloudRoot);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "ContextMenuServer: Initialize: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
        }
        private CLError pInitialize(string cloudRoot)
        {
            try
            {
                // ensure ContextMenuServer is only ever initialized once
                lock (this)
                {
                    if (isInitialized)
                    {
                        _trace.writeToLog(1, "ContextMenuServer: pInitialize: ERROR: THROW: Instance already initailized.");
                        throw new Exception("ContextMenuServer Instance already initialized");
                    }
                    isInitialized = true;
                }

                // Capture the Cloud directory path for performance.
                _filePathCloudDirectory = cloudRoot;

                // Start the named pipes for communicating with BadgeCOM.
                _trace.writeToLog(9, "ContextMenuServer: StartBadgeCOMPipes.");
                StartBadgeCOMPipes();
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "ContextMenuServer: pInitialize: ERROR: Exception: Msg: <{0}>, Code: {1}.", ex.Message);
                return ex;
            }
            _trace.writeToLog(9, "ContextMenuServer: pInitialize: Return success.");
            return null;
        }

        /// <summary>
        /// Returns whether ContextMenuServer is already initialized. If it is initialized, do not initialize it again.
        /// </summary>
        /// <param name="isInitialized">Return value</param>
        /// <returns>Error if it exists</returns>
        public static CLError IsContextMenuServerInitialized(out bool isInitialized)
        {
            try
            {
                return Instance.pIsContextMenuServerInitialized(out isInitialized);
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "ContextMenuServer: IsBadgingInitialized: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                isInitialized = Helpers.DefaultForType<bool>();
                return ex;
            }
        }
        private CLError pIsContextMenuServerInitialized(out bool isInitialized)
        {
            try
            {
                isInitialized = this.IsInitialized;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "ContextMenuServer: pIsBadgingInitialized: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                isInitialized = Helpers.DefaultForType<bool>();
                return ex;
            }
            return null;
        }
        private bool isInitialized = false;
        private bool IsInitialized
        {
            get
            {
                bool rc;
                lock (this)
                {
                    rc = isInitialized;
                }
                return rc;
            }
        }

        // The functionality of clearAllBadges is implemented by shutting down the badge service (confirmed with Gus/Steve that badging only stops when service is killed)
        /// <summary>
        /// Call this on application shutdown to clean out open named pipes to badge COM objects
        /// and to notify the system immediately to remove badges. Do not initialize again after shutting down
        /// </summary>
        public static CLError Shutdown()
        {
            _trace.writeToLog(9, "ContextMenuServer: Shutdown.  Entry.");
            return Instance.pShutdown();
        }
        public CLError pShutdown()
        {
            try
            {
                ((IDisposable)this).Dispose();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "ContextMenuServer: pShutdown: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                return ex;
            }
            return null;
        }
        #endregion

        #region IDisposable member
        // Standard IDisposable implementation based on MSDN System.IDisposable
        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            if (!this.Disposed)
            {
                // lock on current object for changing RunningStatus so it cannot be stopped/started simultaneously
                _trace.writeToLog(9, "ContextMenuServer: Dispose.  Lock.");
                lock (this)
                {
                    // monitor is now set as disposed which will produce errors if startup is called later
                    _trace.writeToLog(9, "ContextMenuServer: Dispose.  Set Disposed.");
                    Disposed = true;
                }

                // Run dispose on inner managed objects based on disposing condition
                if (disposing)
                {
                    // locks on this in case initialization is occurring simultaneously
                    _trace.writeToLog(9, "ContextMenuServer: Dispose. Disposing.");
                    lock (this)
                    {
                        // only need to shutdown if it was initialized
                        if (isInitialized)
                        {
                            // lock on object containing intial pipe connection running state
                            _trace.writeToLog(9, "ContextMenuServer: Dispose. Initialized.");
                            lock (pipeLocker)
                            {
                                // set runningstate to off
                                _trace.writeToLog(9, "ContextMenuServer: Dispose. PipeLocker.");
                                pipeLocker.pipeRunning = false;

                                // Dispose the context menu stream
                                try
                                {
                                    // cleanup initial pipe connection

                                    try
                                    {
                                        pipeContextMenuServer.Stop();
                                    }
                                    catch
                                    {
                                        _trace.writeToLog(1, "ContextMenuServer: Dispose. ERROR: Exception stopping NamedPipeServerContextMenu for context menu.");
                                    }
                                }
                                catch
                                {
                                    _trace.writeToLog(1, "ContextMenuServer: Dispose. ERROR: Exception (3).");
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool Disposed = false;

        /// <summary>
        /// The Cloud directory path captured as a FilePath at initialization.
        /// </summary>
        private FilePath _filePathCloudDirectory { get; set; }


        #region methods to interface with BadgeCOM
        #region variables, constants, and local classes
        /// <summary>
        /// Constant pipename for initial badging connections and appended for unique return connections
        /// (must match pipename used by COM objects).  The actual pipe name is of the form:
        /// "<UserName>/BadgeCOM/ContextMenu".</UserName>
        /// </summary>
        private const string PipeName = "BadgeCOM";

        /// <summary>
        /// Lockable object used to store running state of the initial badging connection pipe
        /// </summary>
        private pipeRunningHolder pipeLocker = new pipeRunningHolder()
        {
            pipeRunning = true
        };

        /// <summary>
        /// Creates the named pipe server stream for the shell extension context menu support.
        /// </summary>
        private NamedPipeServerContextMenu pipeContextMenuServer = null;

        /// <summary>
        /// Object type of pipeLocker
        /// (Lockable object storing running state of the initial badging connection pipe)
        /// </summary>
        private class pipeRunningHolder
        {
            public bool pipeRunning { get; set; }
        }
        #endregion

        /// <summary>
        /// Initializes listener threads for NamedPipeServerStreams to talk to BadgeCOM objects
        /// </summary>
        private void StartBadgeCOMPipes()
        {
            try
            {
                // Set up the thread params to start the pipe to listen to shell extension context menu messages
                _trace.writeToLog(9, "ContextMenuServer: StartBadgeCOMPipes. Start new server pipe for the context menu.");
                NamedPipeServerContextMenu serverContextMenu = new NamedPipeServerContextMenu();
                serverContextMenu.UserState = new NamedPipeServerContextMenu_UserState { FilePathCloudDirectory = _filePathCloudDirectory };
                serverContextMenu.PipeName = Environment.UserName + "/" + PipeName + "/ContextMenu";
                serverContextMenu.Run();

                // Remember this thread for Dispose
                pipeContextMenuServer = serverContextMenu;
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "ContextMenuServer: StartBadgeCOMPipes: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
            }
        }
        #endregion
    }
}
