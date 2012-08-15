using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif // #if DEBUG
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Interop;
using win_client.SystemTray.HookWrapper;

namespace win_client.SystemTray.TrayIcon
{

    internal static class NativeMethods
    {
        /////////////////////////////////////////////////////////////
        #region Imports - Constants, Structures and Functions

        public const int NIM_ADD = 0x00;
        public const int NIM_MODIFY = 0x01;
        public const int NIM_DELETE = 0x02;

        public const int NIF_MESSAGE = 0x01;
        public const int NIF_ICON = 0x02;
        public const int NIF_TIP = 0x04;

        public const int ID_TRAY_APP_ICON = 5000;
        public const int ID_TRAY_EXIT_CONTEXT_MENU_ITEM = 3000;
        public const int WM_USER = 0x0400;
        public const int WM_TRAYICON = (WM_USER + 1);

        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_LBUTTONDBLCLK = 0x0203;

        [StructLayout(LayoutKind.Sequential)]
        public struct NOTIFYICONDATA
        {
            public System.Int32 cbSize; // DWORD
            public System.IntPtr hWnd; // HWND
            public System.Int32 uID; // UINT
            public UInt32 uFlags; // UINT
            public System.Int32 uCallbackMessage; // UINT
            public System.IntPtr hIcon; // HICON
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public System.String szTip; // char[128]
            public System.Int32 dwState; // DWORD
            public System.Int32 dwStateMask; // DWORD
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public System.String szInfo; // char[256]
            public System.Int32 uTimeoutOrVersion; // UINT
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public System.String szInfoTitle; // char[64]
            public System.Int32 dwInfoFlags; // DWORD
        }

        [DllImport("shell32.dll")]
        public static extern bool Shell_NotifyIcon(uint dwMessage, [In] ref NOTIFYICONDATA pnid);

        #endregion Imports - Constants, Structures and Functions
    }

    /// <summary>
    /// Declaration of TrayIcon class
    /// </summary>
    public class TrayIcon : IDisposable
    {


        /////////////////////////////////////////////////////////////l
        // Events and Delegates

        public event EventHandler LeftDoubleClick;

        /////////////////////////////////////////////////////////////
        // Attributes

        // The system-level notify-icon structure
        private NativeMethods.NOTIFYICONDATA m_notifyIconData;

        // Retain the last icon that was set
        private Icon m_currentTrayIcon;

        // The main window of the application
        private Window m_mainWindow;

        // A hook into the main window so
        // we can find out when the icon was clicked
        private win_client.SystemTray.HookWrapper.HookWrapper m_hook;

        // Is the icon visible, or not
        private bool m_bIsIconVisible = false;
        public bool IsIconVisible
        {
            get { return m_bIsIconVisible; }
        }

        // Is this item disposed?
        private bool m_bDisposed = false;

        /////////////////////////////////////////////////////////////
        // Construction

        public TrayIcon(Window mainWindow)
        {

            // Initialize member variables
            m_mainWindow = mainWindow;

            // Get handle of application window
            WindowInteropHelper helper = new WindowInteropHelper(mainWindow);
#if DEBUG
            Debug.Assert(helper != null);
#endif // #if DEBUG
            IntPtr hMainWindow = helper.Handle;
#if DEBUG
            Debug.Assert(hMainWindow != IntPtr.Zero);
#endif // #if DEBUG

            // Construct the notify icon data structure
            m_notifyIconData = new NativeMethods.NOTIFYICONDATA();
            m_notifyIconData.cbSize = Marshal.SizeOf(m_notifyIconData);
            m_notifyIconData.hWnd = hMainWindow;
            m_notifyIconData.uID = NativeMethods.ID_TRAY_APP_ICON;
            m_notifyIconData.uFlags = NativeMethods.NIF_MESSAGE;
            m_notifyIconData.uCallbackMessage = NativeMethods.WM_TRAYICON;

        }

        ~TrayIcon()
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

                // Update member flag
                m_bDisposed = true;

                // If disposing equals true, dispose all managed and unmanaged resources.
                if (disposing)
                {

                    // Dispose of managed resources
                    m_hook.Dispose();

                }

                // Dispose of un-managed resources
                if (m_bIsIconVisible)
                    this.Hide();

            }

        }

        /////////////////////////////////////////////////////////////
        // Operations

        public void Show(Icon trayIcon, string strTip)
        {

            // SHOW THE TRAY ICON

            // Shortcuts
            if (m_bIsIconVisible)
            {
#if DEBUG
                Debug.Assert(false);
#endif // #if DEBUG
                return;
            }

            // Retain the current icon
            m_currentTrayIcon = trayIcon;

            // Prep the notify-icon structure
            _prepStructure_Icon(trayIcon);
            _prepStructure_Tip(strTip);

            // Add the icon to the tray with a system-level call
            if (NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref m_notifyIconData))
            {

                // Track the visiblity
                m_bIsIconVisible = true;

                // Hook the window for icon
                // Set filter so only find out about WM_TRAYICON messages
                m_hook = new win_client.SystemTray.HookWrapper.HookWrapper(m_mainWindow, _fnHookCallback, NativeMethods.WM_TRAYICON);

            }
#if DEBUG
            else
                Debug.Assert(false);
#endif // #if DEBUG

        }

        public void SetTip(string strTip)
        {

            // UPDATE THE TOOLTIP FOR THE ICON

            // shortcuts
            if (!m_bIsIconVisible)
            {
#if DEBUG
                Debug.Assert(false);
#endif // #if DEBUG
                return;
            }

            // Prep the structure (keep the current icon)
            _prepStructure_Icon(m_currentTrayIcon);
            _prepStructure_Tip(strTip);

            // Modify the icon with a system call
            if (!NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_MODIFY, ref m_notifyIconData))
            {
#if DEBUG
                Debug.Assert(false);
#endif // #if DEBUG
            }

        }

        public void Hide()
        {

            // HIDE THE TRAY ICON BY REMOVING IT

            // shortcuts
            if (!m_bIsIconVisible)
            {
#if DEBUG
                Debug.Assert(false);
#endif // #if DEBUG
                return;
            }

            // Prep structure for delete
            _prepStructure_Icon(null);
            _prepStructure_Tip(null);

            // Remove the icon with a system-level call
            if (NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref m_notifyIconData))
            {

                // Update flag
                m_bIsIconVisible = false;

                // Loose the hook
                if (m_hook != null)
                {
                    m_hook.Dispose();
                    m_hook = null;
                }

            }
#if DEBUG
            else
                Debug.Assert(false);
#endif // #if DEBUG

        }

        private void _prepStructure_Icon(Icon trayIcon)
        {

            // UPDATE THE NOTIFY_ICON STRUCTURE FOR AN ICON

            // If there is an icon...
            if (trayIcon != null)
            {
                m_notifyIconData.uFlags |= NativeMethods.NIF_ICON;
                m_notifyIconData.hIcon = trayIcon.Handle;
            }
            else
            {
                if ((m_notifyIconData.uFlags & NativeMethods.NIF_ICON) == NativeMethods.NIF_ICON)
                    m_notifyIconData.uFlags ^= NativeMethods.NIF_ICON;
                m_notifyIconData.hIcon = IntPtr.Zero;
            }

        }

        private void _prepStructure_Tip(string strTip)
        {

            // UPDATE THE NOTIFY_ICON STRUCTURE FOR A TOOLTIP

            // If there is a top...
            if (!string.IsNullOrEmpty(strTip))
            {
                m_notifyIconData.uFlags |= NativeMethods.NIF_TIP;
                m_notifyIconData.szTip = strTip.Substring(0, Math.Min(127, strTip.Length));
            }
            else
            {
                if ((m_notifyIconData.uFlags & NativeMethods.NIF_TIP) == NativeMethods.NIF_TIP)
                    m_notifyIconData.uFlags ^= NativeMethods.NIF_TIP;
                m_notifyIconData.szTip = null;
            }

        }

        private void _fnHookCallback(int msg, IntPtr wParam, IntPtr lParam)
        {
#if DEBUG
            Debug.Assert(msg == NativeMethods.WM_TRAYICON);
#endif // #if DEBUG

            // CALLBACK FUNCTION FOR WINDOW HOOK			

            // Get the notification event
            int iEvent = lParam.ToInt32() & 0xFFFF;
            if (iEvent == NativeMethods.WM_LBUTTONDBLCLK)
            {

                // Fire an event
                if (LeftDoubleClick != null)
                    LeftDoubleClick(this, new EventArgs());

            }

        }

    }

}
