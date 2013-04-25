using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Sync
{

    internal sealed class NetworkChangedEventArgs : EventArgs
    {
        private readonly bool _isConnected;

        public NetworkChangedEventArgs(bool connected)
        {
            this._isConnected = connected;
        }

        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
        }
    }

    /// <summary>
    /// Network connectivity monitor base; 
    /// Gives access to a singleton instance of a generic network connectivity monitor;
    /// Descendant classes can implement different strategies of monitoring the network connectivity;
    /// </summary>
    internal abstract class NetworkMonitor : IDisposable
    {
        #region Public interface

        /// <summary>
        /// delegate for network changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void NetworkChangedEventHandler(object sender, NetworkChangedEventArgs e);

        /// <summary>
        /// NetworkChanged event will be triggered on network change;
        /// clients can call CheckInternetIsConnected() to retrieve network status;
        /// </summary>
        public event NetworkChangedEventHandler NetworkChanged;

        /// <summary>
        /// checks if internet is connected
        /// </summary>
        /// <returns>true if internet is connected</returns>
        public abstract bool CheckInternetIsConnected();

        /// <summary>
        /// starts monitoring the network;
        /// change of monitor state will trigger the NetworkChanged event;
        /// </summary>
        public abstract void StartNetworkMonitor();

        /// <summary>
        /// stops monitoring the network;
        /// StopNetworkMonitor() is called at monitor disposal;
        /// </summary>
        public abstract void StopNetworkMonitor();

        #endregion

        #region IDisposable

        private bool _disposed = false;

        ~NetworkMonitor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                StopNetworkMonitor();

                if (disposing)
                {
                    DisposeManagedObjects();
                }

                DisposeUnmanagedObjects();

                _disposed = true;
            }
        }

        /// <summary>
        /// placeholder for disposing managed objects in descendant classes
        /// </summary>
        protected virtual void DisposeManagedObjects()
        {
        }

        /// <summary>
        /// placeholder for disposing unmanaged objects in descendant classes
        /// </summary>
        protected virtual void DisposeUnmanagedObjects()
        {
        }

        #endregion

        #region Singleton pattern

        private static NetworkMonitor _instance = null;
        private static readonly object _instanceLock = new object();

        /// <summary>
        /// singleton Instance access;
        /// an object will be created at the time of the first access;
        /// </summary>
        public static NetworkMonitor Instance
        {
            get
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        _instance = createNetworkMonitor();
                    }
                    return _instance;
                }
            }
        }

        /// <summary>
        /// disposes the singleton instance;
        /// a new object will be created at the next Instance access;
        /// </summary>
        public static void DisposeInstance()
        {
            lock (_instanceLock)
            {
                if (_instance != null)
                {
                    _instance.Dispose();
                    _instance = null;
                }
            }
        }

        #endregion

        #region Factory pattern

        private static NetworkMonitor createNetworkMonitor()
        {
            // OS dependent or preferred implementation...
            return new NLANetworkMonitor();
        }

        #endregion

        #region Common helper methods
        
        /// <summary>
        /// fires the NetworkChanged event
        /// </summary>
        /// <param name="connected"></param>
        protected virtual void NotifyNetworkChanged(bool connected)
        {
            if (NetworkChanged != null)
            {
                try
                {
                    NetworkChanged(this, new NetworkChangedEventArgs(connected));
                }
                catch
                {
                    //noop;
                }

            }
        }
        
        #endregion

    }

    internal abstract class NetworkMonitorBase : NetworkMonitor
    {
        private bool _isStarted = false;
        private readonly object _lock = new object(); // "this" private lock

        /// <summary>
        /// this startup method is guaranteed to be called only if monitor is not yet started;
        /// </summary>
        protected abstract void InternalStartNetworkMonitor();

        /// <summary>
        /// this shutdown method is guaranteed to be called only if monitor has been already started;
        /// </summary>
        protected abstract void InternalStopNetworkMonitor();

        public override void StartNetworkMonitor()
        {
            lock (_lock)
            {
                if (!_isStarted)
                {
                    InternalStartNetworkMonitor();

                    _isStarted = true;
                }
            }
        }

        public override void StopNetworkMonitor()
        {
            lock (_lock)
            {
                if (_isStarted)
                {
                    InternalStopNetworkMonitor();

                    _isStarted = false;
                }
            }
        }

    }

}
