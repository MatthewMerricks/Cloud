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

namespace win_client.SystemTray.HookWrapper
{
    internal static class NativeMethods
    {
        /////////////////////////////////////////////////////////////
        #region Imports - Constants, Structures and Functions

        public enum HookType : int
        {
            WH_JOURNALRECORD = 0,
            WH_JOURNALPLAYBACK = 1,
            WH_KEYBOARD = 2,
            WH_GETMESSAGE = 3,
            WH_CALLWNDPROC = 4,
            WH_CBT = 5,
            WH_SYSMSGFILTER = 6,
            WH_MOUSE = 7,
            WH_HARDWARE = 8,
            WH_DEBUG = 9,
            WH_SHELL = 10,
            WH_FOREGROUNDIDLE = 11,
            WH_CALLWNDPROCRET = 12,
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14
        }

        public const int WM_KEYUP = 0x0101;

        [StructLayout(LayoutKind.Sequential)]
        public struct CWPSTRUCT
        {
            public IntPtr lparam;
            public IntPtr wparam;
            public int message;
            public IntPtr hwnd;
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(HookType hookType, IntPtr pFunc, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        internal static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hHook);

        #endregion Imports - Constants, Structures and Functions
    }


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
