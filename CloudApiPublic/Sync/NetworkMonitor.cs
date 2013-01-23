using CloudApiPublic.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace CloudApiPublic.Sync
{
    internal class NetworkMonitor : IDisposable
    {
        #region singleton pattern
        public static NetworkMonitor Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    if (!InstanceLocker.Value)
                    {
                        _instance = new NetworkMonitor();
                        _instance.StartNetworkMonitor();
                        InstanceLocker.Value = true;
                    }
                    return _instance;
                }
            }
        }
        private static NetworkMonitor _instance = null;
        private static readonly GenericHolder<bool> InstanceLocker = new GenericHolder<bool>(false);

        public static void DisposeInstance()
        {
            lock (InstanceLocker)
            {
                InstanceLocker.Value = true;
                if (_instance != null)
                {
                    try
                    {
                        _instance.Dispose(true);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private NetworkMonitor() { }
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
            lock (this)
            {
                if (!this.Disposed)
                {
                    this.Disposed = true;

                    // Run dispose on inner managed objects based on disposing condition
                    if (disposing)
                    {
                        lock (InstanceLocker)
                        {
                            // only need to shutdown if it was initialized
                            if (InstanceLocker.Value)
                            {
                                _instance = null;
                            }
                        }
                    }

                    StopNetworkMonitor();
                }
            }
        }

        private bool Disposed = false;

        // my guess at what CheckResult probably means
        public void CheckResult(int hResult)
        {
            if (hResult != 0)
            {
                throw new Win32Exception(hResult);
            }
        }

        #region WSAData
        [StructLayout(LayoutKind.Sequential)]
        public class WSAData
        {
            public Int16 wVersion;
            public Int16 wHighVersion;
            public String szDescription;
            public String szSystemStatus;
            public Int16 iMaxSockets;
            public Int16 iMaxUdpDg;
            public IntPtr lpVendorInfo;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class WSAQUERYSET
        {
            public Int32 dwSize = 0;
            public String szServiceInstanceName = null;
            public IntPtr lpServiceClassId;
            public IntPtr lpVersion;
            public String lpszComment;
            public Int32 dwNameSpace;
            public IntPtr lpNSProviderId;
            public String lpszContext;
            public Int32 dwNumberOfProtocols;
            public IntPtr lpafpProtocols;
            public String lpszQueryString;
            public Int32 dwNumberOfCsAddrs;
            public IntPtr lpcsaBuffer;
            public Int32 dwOutputFlags;
            public IntPtr lpBlob;
        }

        [DllImport("Ws2_32.DLL", CharSet = CharSet.Auto,
        SetLastError = true)]
        private extern static
          Int32 WSAStartup(Int16 wVersionRequested, WSAData wsaData);

        [DllImport("Ws2_32.DLL", CharSet = CharSet.Auto,
        SetLastError = true)]
        private extern static
          Int32 WSACleanup();

        [DllImport("Ws2_32.dll", CharSet = CharSet.Auto,
        SetLastError = true)]
        private extern static
          Int32 WSALookupServiceBegin(WSAQUERYSET qsRestrictions,
            Int32 dwControlFlags, ref Int32 lphLookup);

        [DllImport("Ws2_32.dll", CharSet = CharSet.Auto,
        SetLastError = true)]
        private extern static
          Int32 WSALookupServiceNext(Int32 hLookup,
            Int32 dwControlFlags,
            ref Int32 lpdwBufferLength,
            IntPtr pqsResults);

        [DllImport("Ws2_32.dll", CharSet = CharSet.Auto,
        SetLastError = true)]
        private extern static
          Int32 WSALookupServiceEnd(Int32 hLookup);
        #endregion

        #region network event
        [DllImport("Ws2_32.dll", CharSet = CharSet.Auto,
   SetLastError = true)]
        private extern static Int32 WSANSPIoctl(
          Int32 hLookup,
          UInt32 dwControlCode,
          IntPtr lpvInBuffer,
          Int32 cbInBuffer,
          IntPtr lpvOutBuffer,
          Int32 cbOutBuffer,
          ref Int32 lpcbBytesReturned,
          IntPtr lpCompletion);

        public delegate void NetworkChangedEventHandler(object sender,
          NetworkChangedEventArgs e);

        private Int32 monitorLookup = 0;

        protected void WaitForNetworkChanges()
        {
            WSAQUERYSET qsRestrictions = new WSAQUERYSET();
            Int32 dwControlFlags;

            qsRestrictions.dwSize = Marshal.SizeOf(typeof(WSAQUERYSET));
            qsRestrictions.dwNameSpace = 0; //NS_ALL;

            dwControlFlags = 0x0FF0; //LUP_RETURN_ALL;

            int nResult = WSALookupServiceBegin(qsRestrictions,
                  dwControlFlags, ref monitorLookup);

            Int32 dwBytesReturned = 0;
            UInt32 cCode = 0x88000019; //SIO_NSP_NOTIFY_CHANGE
            nResult = WSANSPIoctl(monitorLookup, cCode,
                  new IntPtr(), 0, new IntPtr(), 0,
                  ref dwBytesReturned,
                  new IntPtr());

            if (0 != nResult)
            {
                CheckResult(nResult);
            }

            nResult = WSALookupServiceEnd(monitorLookup);
            monitorLookup = 0;
        }

        public event NetworkChangedEventHandler NetworkChanged;

        protected void NetworkMonitor()
        {
            int nResult = 0;

            while (0 == nResult)
            {
                WaitForNetworkChanges();

                ArrayList networks = GetConnectedNetworks();

                NetworkChangedEventArgs eventArgs =
                    new NetworkChangedEventArgs(networks);

                try
                {
                    if (null != NetworkChanged)
                    {
                        NetworkChanged(this, eventArgs);
                    }
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.ToString());
                }
            }
        }

        private readonly GenericHolder<Thread> monitorThread = new GenericHolder<Thread>(null);

        public virtual void StartNetworkMonitor()
        {
            lock (monitorThread)
            {
                if (monitorThread.Value == null)
                {
                    monitorThread.Value = new Thread(new
                      ThreadStart(this.NetworkMonitor));
                    monitorThread.Value.Name = "Network Monitor";
                    monitorThread.Value.IsBackground = true;
                    monitorThread.Value.Start();
                }
            }
        }

        public virtual void StopNetworkMonitor()
        {
            lock (monitorThread)
            {
                if (monitorThread.Value != null)
                {
                    try
                    {
                        monitorThread.Value.Abort();
                    }
                    catch
                    {
                    }
                    monitorThread.Value = null;
                    WSALookupServiceEnd(monitorLookup);
                }
            }
        }
        #endregion

        #region GetConnectedNetworks
        public virtual ArrayList GetConnectedNetworks()
        {
            ArrayList networkConnections = new ArrayList();
            WSAQUERYSET qsRestrictions;
            Int32 dwControlFlags;
            Int32 valHandle = 0;

            qsRestrictions = new WSAQUERYSET();
            qsRestrictions.dwSize = Marshal.SizeOf(typeof(WSAQUERYSET));
            qsRestrictions.dwNameSpace = 0; //NS_ALL;
            dwControlFlags = 0x0FF0; //LUP_RETURN_ALL;

            int result = WSALookupServiceBegin(qsRestrictions,
              dwControlFlags, ref valHandle);

            CheckResult(result);

            while (0 == result)
            {
                Int32 dwBufferLength = 0x10000;
                IntPtr pBuffer = Marshal.AllocHGlobal(dwBufferLength);

                WSAQUERYSET qsResult = new WSAQUERYSET();

                result = WSALookupServiceNext(valHandle, dwControlFlags,
                ref dwBufferLength, pBuffer);

                if (0 == result)
                {
                    Marshal.PtrToStructure(pBuffer, qsResult);
                    networkConnections.Add(
                        qsResult.szServiceInstanceName);
                }

                Marshal.FreeHGlobal(pBuffer);
            }

            result = WSALookupServiceEnd(valHandle);

            return networkConnections;
        }
        #endregion
    }

    internal class NetworkChangedEventArgs : EventArgs
    {
        private ArrayList networkList;
        public NetworkChangedEventArgs(ArrayList networks)
        {
            networkList = networks;
        }

        public ArrayList Networks
        {
            get { return networkList; }
        }
    }
}