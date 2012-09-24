//
// HookWrapper.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif // #if DEBUG
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using win_client.Static;

namespace win_client.SystemTray.HookWrapper
{
    /// <summary>
    /// Declaration of HookWrapper class
    /// </summary>
    public class HookWrapper : IDisposable
    {
        /////////////////////////////////////////////////////////////
        // Delegates

        // This is an internally used delegate, passed to the hook system functions
        private delegate int HookProcDelegate(int code, IntPtr wParam, IntPtr lParam);

        // This is a delegate for the owner to receive message notifications
        // We could have passed the notifications back as events, but a straight function
        // call is more efficient.
        public delegate void NotifyDelegate(int msg, IntPtr wParam, IntPtr lParam);

        /////////////////////////////////////////////////////////////
        // Constants and Enumerations

        // Use this filter value to disable filtering
        public static int NoFilter = 0;

        /////////////////////////////////////////////////////////////
        // Attributes

        // Handle the system hook
        private IntPtr m_hHook;

        // Callback function for hook
        private HookProcDelegate m_fnHookProc;

        // Function provided by owner to process their messages
        private NotifyDelegate m_fnNotify;

        // Is this item disposed?
        private bool m_bDisposed = false;

        // Only pass a particular message to the owner
        private int m_iMessageFilter;

        /////////////////////////////////////////////////////////////
        // Construction

        public HookWrapper(Window targetWindow, NotifyDelegate fnNotify, int iMessageFilter)
        {
#if DEBUG
            Debug.Assert(fnNotify != null);
#endif // #if DEBUG

            // Initialize member variables
            m_fnNotify = fnNotify;
            m_iMessageFilter = iMessageFilter;

            // Get the thread id (and disable the warning about deprecated functions)
            // Use GetCurrentThreadId against Microsoft advice, as there is chance of the system
            // switching thread-id, but it is small for UI thread.
#pragma warning disable 0618
            uint ThreadId = (uint)AppDomain.GetCurrentThreadId();
#pragma warning restore 0618

            // Create an instance of the delegate
            // DO NOT BE TEMPTED TO REDUCE THIS, OR THE SYSTEM WILL DISPOSE OF THE FUNCTION
            // AND LEAVE YOU DANGLING
            m_fnHookProc = new HookProcDelegate(_localCallbackFunction);
            IntPtr pFunc = Marshal.GetFunctionPointerForDelegate(m_fnHookProc);

            // Setup the hook by calling an inported system function
            m_hHook = NativeMethods.SetWindowsHookEx(
                NativeMethods.HookType.WH_CALLWNDPROC,
                pFunc,
                IntPtr.Zero,
                ThreadId
            );

        }

        ~HookWrapper()
        {
            Dispose(false);
        }

        /////////////////////////////////////////////////////////////
        // Implementation of IDisposable interface

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {

            // Check to see if Dispose has already been called
            if (!this.m_bDisposed)
            {

                // We are now disposing/disposed
                m_bDisposed = true;

                // If disposing equals true, dispose all managed and unmanaged resources.
                if (disposing)
                {

                    // DISPOSE OF MANAGED RESOURCES

                }

                // DISPOSE OF UN-MANAGED RESOURCES
                if (m_hHook != IntPtr.Zero)
                {
                    NativeMethods.UnhookWindowsHookEx(m_hHook);
                    m_hHook = IntPtr.Zero;
                }

            }

        }

        /////////////////////////////////////////////////////////////
        // Operations

        private int _localCallbackFunction(int code, IntPtr wParam, IntPtr lParam)
        {

            // THIS IS THE CALLBACK FUNCTION THAT THE SYSTEM WILL CALL WHEN
            // A MESSAGE IS PUMPED FOR OUR THREAD

            // Declare return variable
            int iFtmp = 0;

            // If passing function to next hook without processing...
            if (code < 0)
            {

                // Pass the values to the next hook
                iFtmp = NativeMethods.CallNextHookEx(m_hHook, code, wParam, lParam).ToInt32();

            }
            else
            {

                // Marshall the data from the callback
                NativeMethods.CWPSTRUCT cwp = (NativeMethods.CWPSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.CWPSTRUCT));

                // If passes filter...
                if ((m_iMessageFilter == NoFilter) || (m_iMessageFilter == cwp.message))
                {

                    // Pass to the owner
                    m_fnNotify(cwp.message, cwp.wparam, cwp.lparam);

                }

                // Pass the values to the next hook
                iFtmp = NativeMethods.CallNextHookEx(m_hHook, code, wParam, lParam).ToInt32();

            }

            // Return variable
            return iFtmp;

        }
    }
}