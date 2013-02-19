//
// NetworkMonitor.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using CloudApiPublic.Static;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace CloudApiPublic.Sync
{
    internal sealed class NetworkMonitor : IDisposable
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

                    StopWSA();
                }
            }
        }

        private void StopWSA()
        {
            lock (WSAStarted)
            {
                if (WSAStarted.Value)
                {
                    NativeMethods.WSACleanup();
                }
            }
        }

        private bool Disposed = false;

        // my guess at what CheckResult probably means
        public void CheckResult(int hResult)
        {
            if (hResult != 0
                && hResult != -1) // seems like -1 was a common case for some reason
            {
                throw new Win32Exception(hResult);
            }
        }

        #region WSAData
        public delegate void NetworkChangedEventHandler(
            object sender,
            NetworkChangedEventArgs e);

        private IntPtr monitorLookup = IntPtr.Zero;

        private readonly GenericHolder<bool> WSAStarted = new GenericHolder<bool>(false);
        private void StartWSAIfNotStarted()
        {
            lock (WSAStarted)
            {
                if (WSAStarted.Value)
                {
                    return;
                }

                const short winSocksVersion = 0x0202; // version 2.2

                NativeMethods.WSAData startupData = new NativeMethods.WSAData();
                startupData.highVersion = 2;
                startupData.version = 2;
                int result = NativeMethods.WSAStartup(winSocksVersion, out startupData);

                CheckResult(result);

                WSAStarted.Value = true;
            }
        }

        private void WaitForNetworkChanges()
        {
            StartWSAIfNotStarted();

            NativeMethods.WSAQUERYSET qsRestrictions = new NativeMethods.WSAQUERYSET();
            Int32 dwControlFlags;

            qsRestrictions.dwSize = Marshal.SizeOf(typeof(NativeMethods.WSAQUERYSET));
            qsRestrictions.dwNameSpace = NativeMethods.NAMESPACE_PROVIDER_PTYPE.NS_NLA;

            dwControlFlags = 0x0FF0; //LUP_RETURN_ALL;

            int nResult = NativeMethods.WSALookupServiceBegin(
                qsRestrictions,
                dwControlFlags,
                ref monitorLookup);

            Int32 dwBytesReturned = 0;
            UInt32 cCode = 0x88000019; //SIO_NSP_NOTIFY_CHANGE
            nResult = NativeMethods.WSANSPIoctl(
                monitorLookup,
                cCode,
                new IntPtr(),
                0,
                new IntPtr(),
                0,
                ref dwBytesReturned,
                new IntPtr());

            if (0 != nResult)
            {
                CheckResult(nResult);
            }

            nResult = NativeMethods.WSALookupServiceEnd(monitorLookup);
            monitorLookup = IntPtr.Zero;
        }

        public event NetworkChangedEventHandler NetworkChanged;

        private void MonitorNetwork()
        {
            int nResult = 0;

            bool firstChange = true;
            bool internetIsConnected = false;

            while (0 == nResult)
            {
                WaitForNetworkChanges();

                bool changedValue = CheckInternetIsConnected();

                if (firstChange
                    || internetIsConnected != changedValue)
                {
                    firstChange = false;
                    internetIsConnected = changedValue;

                    NetworkChangedEventArgs eventArgs =
                        new NetworkChangedEventArgs(internetIsConnected);

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
        }

        private readonly GenericHolder<Thread> monitorThread = new GenericHolder<Thread>(null);

        public void StartNetworkMonitor()
        {
            lock (monitorThread)
            {
                if (monitorThread.Value == null)
                {
                    monitorThread.Value = new Thread(new
                      ThreadStart(this.MonitorNetwork));
                    monitorThread.Value.Name = "Network Monitor";
                    monitorThread.Value.IsBackground = true;
                    monitorThread.Value.Start();
                }
            }
        }

        public void StopNetworkMonitor()
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
                    NativeMethods.WSALookupServiceEnd(monitorLookup);
                }
            }
        }
        #endregion

        #region IsConnectedToInternet
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool CheckInternetIsConnected()
        {
            StartWSAIfNotStarted();

            bool toReturn = false;

            NativeMethods.WSAQUERYSET qsRestrictions;
            Int32 dwControlFlags;
            IntPtr valHandle = IntPtr.Zero;

            qsRestrictions = new NativeMethods.WSAQUERYSET();
            qsRestrictions.dwSize = Marshal.SizeOf(typeof(NativeMethods.WSAQUERYSET));
            qsRestrictions.dwNameSpace = NativeMethods.NAMESPACE_PROVIDER_PTYPE.NS_NLA;
            dwControlFlags = 0x0FF0; //LUP_RETURN_ALL;

            int result = NativeMethods.WSALookupServiceBegin(
                qsRestrictions,
                dwControlFlags,
                ref valHandle);

            if (result != 0)
            {
                int wsaError = NativeMethods.WSAGetLastError();

                throw new Exception("Error on internet lookup: " +
                    (Enum.IsDefined(typeof(NativeMethods.WinSockErrors), wsaError)
                        ? ((NativeMethods.WinSockErrors)wsaError).ToString()
                        : "Unknown error code " + wsaError.ToString()));
            }

            while (0 == result)
            {
                Int32 dwBufferLength = 0x10000;
                IntPtr pBuffer = Marshal.AllocHGlobal(dwBufferLength);

                try
                {
                    NativeMethods.WSAQUERYSET qsResult = new NativeMethods.WSAQUERYSET();

                    result = NativeMethods.WSALookupServiceNext(valHandle, dwControlFlags,
                    ref dwBufferLength, pBuffer);

                    if (0 == result)
                    {
                        Marshal.PtrToStructure(pBuffer, qsResult);

                        //// "qsResult.lpBlob" is a 32 bit size followed by a 32 bit pointer to the following structure (an indirection):
                        //typedef struct _NLA_BLOB {
                        //  struct {
                        //      NLA_BLOB_DATA_TYPE type;
                        //      DWORD dwSize;
                        //      DWORD nextOffset;
                        //  } header;
                        //  union {
                        //      // header.type -> NLA_RAW_DATA
                        //      CHAR rawData[1];
                        //      // header.type -> NLA_INTERFACE
                        //      struct {
                        //          DWORD dwType;
                        //          DWORD dwSpeed;
                        //          CHAR adapterName[1];
                        //      } interfaceData;
                        //      // header.type -> NLA_802_1X_LOCATION
                        //      struct {
                        //          CHAR information[1];
                        //      } locationData;
                        //      // header.type -> NLA_CONNECTIVITY
                        //      struct {
                        //          NLA_CONNECTIVITY_TYPE type;
                        //          NLA_INTERNET internet; // <-------------------------------- NLA_INTERNET_YES = 2, else no internet
                        //      } connectivity;
                        //      // header.type -> NLA_ICS
                        //      struct {
                        //          struct {
                        //              DWORD speed;
                        //              DWORD type;
                        //              DWORD state;
                        //              WCHAR machineName[256];
                        //              WCHAR sharedAdapterName[256];
                        //          } remote;
                        //      } ICS;
                        //  } data;
                        //}

                        if (qsResult.dwNameSpace == NativeMethods.NAMESPACE_PROVIDER_PTYPE.NS_NLA
                            && qsResult.lpBlob != IntPtr.Zero)
                        {
                            NativeMethods.BLOB_INDIRECTION blob = (NativeMethods.BLOB_INDIRECTION)Marshal.PtrToStructure(qsResult.lpBlob, typeof(NativeMethods.BLOB_INDIRECTION));

                            if (blob.pInfo != IntPtr.Zero)
                            {
                                IntPtr currentBlobPointer = blob.pInfo;
                                UInt32 currentPointerOffset = 0;
                                UInt32 blobSizeSum = 0;
                                NativeMethods.NLA_BLOB nlaBlob = (NativeMethods.NLA_BLOB)Marshal.PtrToStructure(currentBlobPointer, typeof(NativeMethods.NLA_BLOB));

                                while (nlaBlob != null)
                                {
                                    if (nlaBlob.header.type == NativeMethods.NLA_BLOB_DATA_TYPE.NLA_CONNECTIVITY)
                                    {
                                        NativeMethods.NLA_BLOB_CONNECTIVITY connectivityBlob = (NativeMethods.NLA_BLOB_CONNECTIVITY)Marshal.PtrToStructure(currentBlobPointer, typeof(NativeMethods.NLA_BLOB_CONNECTIVITY));

                                        toReturn = connectivityBlob.connectivity.internet == NativeMethods.NLA_INTERNET.NLA_INTERNET_YES;
                                        break;
                                    }
                                    else
                                    {
                                        blobSizeSum += nlaBlob.header.dwSize;

                                        if (nlaBlob.header.nextOffset == 0
                                            || currentPointerOffset == nlaBlob.header.nextOffset
                                            || nlaBlob.header.nextOffset >= blob.cbSize
                                            || nlaBlob.header.nextOffset < blobSizeSum)
                                        {
                                            nlaBlob = null;
                                        }
                                        else
                                        {
                                            currentPointerOffset = nlaBlob.header.nextOffset;
                                            currentBlobPointer = IntPtr.Add(blob.pInfo, (int)currentPointerOffset);

                                            nlaBlob = (NativeMethods.NLA_BLOB)Marshal.PtrToStructure(currentBlobPointer, typeof(NativeMethods.NLA_BLOB));
                                        }
                                    }
                                }

                                if (toReturn)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pBuffer);
                }
            }

            result = NativeMethods.WSALookupServiceEnd(valHandle);

            return toReturn;
        }
        #endregion
    }

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
}