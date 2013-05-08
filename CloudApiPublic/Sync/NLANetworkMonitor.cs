using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Cloud.Static;
using Cloud.Model;
using Cloud.Support; 

namespace Cloud.Sync
{
    internal sealed class NLANetworkMonitor : NetworkMonitorBase
    {
        private bool _WSAInitialized;
        private Thread _monitorThread = null;
        private ManualResetEvent _stopEvent = null;
        private readonly IntPtr NLA_SERVICE_CLASS_GUID;
        private Nullable<bool> _lastConnected = null;
        private readonly object _lock = new object(); // "this" private lock


        public NLANetworkMonitor()
        {
            NativeMethods.WSAData data;
            Int32 error = NativeMethods.WSAStartup(0x0202, out data);
            _WSAInitialized = (error == 0);

            Guid guid = new Guid(0x37e515, 0xb5c9, 0x4a43, 0xba, 0xda, 0x8b, 0x48, 0xa8, 0x7a, 0xd2, 0x39);
            byte[] guid_ = guid.ToByteArray();
            this.NLA_SERVICE_CLASS_GUID = Marshal.AllocHGlobal(guid_.Length);
            Marshal.Copy(guid_, 0, NLA_SERVICE_CLASS_GUID, guid_.Length);

        }

        protected override void DisposeManagedObjects()
        {
            if (_monitorThread != null)
            {
                _monitorThread = null;
            }

            if (_stopEvent != null)
            {
                _stopEvent.Dispose();
                _stopEvent = null;
            }
        }

        protected override void DisposeUnmanagedObjects()
        {
            Marshal.FreeHGlobal(this.NLA_SERVICE_CLASS_GUID);

            if (_WSAInitialized)
            {
                NativeMethods.WSACleanup();
                _WSAInitialized = false;
            }
        }

        protected override void NotifyNetworkChanged(bool connected)
        {
            bool connectedChanged;

            lock (_lock)
            {
                connectedChanged = (_lastConnected == null || _lastConnected != connected);
                if (connectedChanged)
                {
                    _lastConnected = connected;
                }
            }

            if (connectedChanged)
            {
                base.NotifyNetworkChanged(connected);
            }
        }


        public override bool CheckInternetIsConnected()
        {
            if (!_WSAInitialized)
            {
                throw new Exception("WSA not initialized");
            }

            NativeMethods.WSAQUERYSET restrictions = new NativeMethods.WSAQUERYSET();
            restrictions.dwSize = Marshal.SizeOf(typeof(NativeMethods.WSAQUERYSET));
            restrictions.dwNameSpace = NativeMethods.NAMESPACE_PROVIDER_PTYPE.NS_NLA;
            restrictions.lpServiceClassId = NLA_SERVICE_CLASS_GUID;

            IntPtr lookup = IntPtr.Zero;
            int error = NativeMethods.WSALookupServiceBegin(restrictions, NativeMethods.LUP_RETURN_BLOB, ref lookup);
            if (error == (int)NativeMethods.WinSockErrors.SOCKET_ERROR)
            {
                error = NativeMethods.WSAGetLastError();
                throw new Win32Exception(error);
            }

            bool foundAdapters = false;
            bool foundInternet = false;

            Int32 bufferSize = 0;
            IntPtr buffer = IntPtr.Zero;

            bool succeeded = false;

            // for every network...
            while (true)
            {
                error = NativeMethods.WSALookupServiceNext(lookup, NativeMethods.LUP_RETURN_BLOB, ref bufferSize, buffer);
                if (error == (int)NativeMethods.WinSockErrors.SOCKET_ERROR)
                {
                    error = NativeMethods.WSAGetLastError();
                    if (error == (int)NativeMethods.WinSockErrors.WSA_E_NO_MORE || error == (int)NativeMethods.WinSockErrors.WSAENOMORE)
                    {
                        succeeded = true;
                        break; // done!
                    }
                    else if (error == (int)NativeMethods.WinSockErrors.WSAEFAULT)
                    {
                        // buffer is too small, reallocate and retry

                        if (buffer != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(buffer);
                        }
                        buffer = Marshal.AllocHGlobal(bufferSize);
                    }
                    else
                    {
                        break; // error
                    }
                }
                else
                {
                    foundAdapters = true;

                    NativeMethods.WSAQUERYSET result = (NativeMethods.WSAQUERYSET)Marshal.PtrToStructure(buffer, typeof(NativeMethods.WSAQUERYSET));
                    NativeMethods.BLOB_INDIRECTION indirection = (NativeMethods.BLOB_INDIRECTION)Marshal.PtrToStructure(result.lpBlob, typeof(NativeMethods.BLOB_INDIRECTION));
                    IntPtr info = indirection.pInfo;
                    while (info != IntPtr.Zero)
                    {
                        NativeMethods.NLA_BLOB blob = (NativeMethods.NLA_BLOB)Marshal.PtrToStructure(info, typeof(NativeMethods.NLA_BLOB));
                        if (blob.header.type == NativeMethods.NLA_BLOB_DATA_TYPE.NLA_CONNECTIVITY)
                        {
                            NativeMethods.NLA_BLOB_CONNECTIVITY connectivity = (NativeMethods.NLA_BLOB_CONNECTIVITY)Marshal.PtrToStructure(info, typeof(NativeMethods.NLA_BLOB_CONNECTIVITY));
                            if (connectivity.connectivity.internet == NativeMethods.NLA_INTERNET.NLA_INTERNET_YES)
                            {
                                foundInternet = true;
                            }
                        }

                        info = blob.header.nextOffset == 0 ? IntPtr.Zero : IntPtr.Add(info, (int)blob.header.nextOffset);
                    }
                }
            }

            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }

            NativeMethods.WSALookupServiceEnd(lookup);

            if (!succeeded)
            {
                throw new Win32Exception(error);
            }

            OperatingSystem osInfo = System.Environment.OSVersion;
            bool isVistaOrLater = (6 <= osInfo.Version.Major);
            bool isConnected = isVistaOrLater ? foundInternet : foundAdapters;

            return isConnected;
        }



        protected override void InternalStartNetworkMonitor()
        {
            if (!_WSAInitialized)
            {
                throw new Exception("WSA not initialized");
            }

            _stopEvent = new ManualResetEvent(false);

            _lastConnected = null;

            _monitorThread = new Thread(new ThreadStart(MonitorNetwork_));
            _monitorThread.Name = "Network Monitor";
            _monitorThread.IsBackground = true;
            _monitorThread.Start();
        }

        protected override void InternalStopNetworkMonitor()
        {
            _stopEvent.Set();
            _monitorThread.Join();
        }

        private void MonitorNetwork_()
        {
            try
            {
                InternalMonitorNetwork_();
            }
            catch (Exception ex)
            {
                ((CLError)ex).LogErrors(CLTrace.Instance.TraceLocation, CLTrace.Instance.LogErrors);
            }
        }

        private void InternalMonitorNetwork_()
        {
            ManualResetEvent networkChangedEvent = new ManualResetEvent(false);

            NativeMethods.OVERLAPPED overlapped_ = new NativeMethods.OVERLAPPED();
            overlapped_.hEvent = networkChangedEvent.SafeWaitHandle.DangerousGetHandle();
            IntPtr overlapped = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeMethods.OVERLAPPED)));
            Marshal.StructureToPtr(overlapped_, overlapped, false);
            overlapped_ = null; // release

            NativeMethods.WSACOMPLETION completion_ = new NativeMethods.WSACOMPLETION();
            completion_.Type = NativeMethods.WSACOMPLETIONTYPE.NSP_NOTIFY_EVENT;
            completion_.Parameters.Event.lpOverlapped = overlapped;
            IntPtr completion = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeMethods.WSACOMPLETION)));
            Marshal.StructureToPtr(completion_, completion, false);
            completion_ = null; // release

            bool notifyNetworkChanged = false; 

            // for every change...
            while (true)
            {
                bool isStopped = (_stopEvent.WaitOne(0) == true);
                if (isStopped)
                {
                    break; // done!
                }

                NativeMethods.WSAQUERYSET restrictions = new NativeMethods.WSAQUERYSET();
                restrictions.dwSize = Marshal.SizeOf(typeof(NativeMethods.WSAQUERYSET));
                restrictions.dwNameSpace = NativeMethods.NAMESPACE_PROVIDER_PTYPE.NS_NLA;
                restrictions.lpServiceClassId = NLA_SERVICE_CLASS_GUID;

                IntPtr lookup = IntPtr.Zero;
                int error = NativeMethods.WSALookupServiceBegin(restrictions, NativeMethods.LUP_RETURN_BLOB, ref lookup);
                if (error == (int)NativeMethods.WinSockErrors.SOCKET_ERROR)
                {
                    error = NativeMethods.WSAGetLastError();
                    break; // error...
                }
                else
                {
                    bool succeeded = false;

                    int bytesWritten = 0;
                    networkChangedEvent.Reset();
                    error = NativeMethods.WSANSPIoctl(lookup, NativeMethods.SIO_NSP_NOTIFY_CHANGE,
                                                        IntPtr.Zero, 0, IntPtr.Zero, 0, // ignored
                                                        ref bytesWritten, completion);
                    if (error != (int)NativeMethods.WinSockErrors.SOCKET_ERROR ||
                        NativeMethods.WSAGetLastError() != (int)NativeMethods.WinSockErrors.WSA_IO_PENDING)
                    {
                        //unexpected...
                    }
                    else
                    {
                        if (notifyNetworkChanged)
                        {
                            NotifyNetworkChanged(CheckInternetIsConnected());
                        }

                        WaitHandle[] events = { _stopEvent, networkChangedEvent };
                        WaitHandle.WaitAny(events);

                        notifyNetworkChanged = true;

                        succeeded = true;
                    }

                    NativeMethods.WSALookupServiceEnd(lookup);

                    if (!succeeded)
                    {
                        break; //error...
                    }
                }
            }

            Marshal.FreeHGlobal(completion);
            Marshal.FreeHGlobal(overlapped);

            networkChangedEvent.Dispose();
        }

    }

}
