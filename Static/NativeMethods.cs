//
// NativeMethods.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace win_client.Static
{
    internal static class NativeMethods
    {
        #region window placement
        [DllImport("user32.dll")]
        public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        public static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

        // POINT structure required by WINDOWPLACEMENT structure
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        // WINDOWPLACEMENT stores the position, size, and state of a window
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT minPosition;
            public POINT maxPosition;
            public RECT normalPosition;
        }
        #endregion

        #region Animating Window to System Tray
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
            public override string ToString()
            {
                return ("Left :" + left.ToString() + "," + "Top :" + top.ToString() + "," + "Right :" + right.ToString() + "," + "Bottom :" + bottom.ToString());
            }
        }
        public struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public ABEdge uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        public enum ABMsg
        {
            ABM_NEW = 0,
            ABM_REMOVE = 1,
            ABM_QUERYPOS = 2,
            ABM_SETPOS = 3,
            ABM_GETSTATE = 4,
            ABM_GETTASKBARPOS = 5,
            ABM_ACTIVATE = 6,
            ABM_GETAUTOHIDEBAR = 7,
            ABM_SETAUTOHIDEBAR = 8,
            ABM_WINDOWPOSCHANGED = 9,
            ABM_SETSTATE = 10
        }

        public enum ABNotify
        {
            ABN_STATECHANGE = 0,
            ABN_POSCHANGED,
            ABN_FULLSCREENAPP,
            ABN_WINDOWARRANGE
        }

        public enum ABEdge
        {
            ABE_LEFT = 0,
            ABE_TOP,
            ABE_RIGHT,
            ABE_BOTTOM
        }

        [DllImport("shell32.dll", EntryPoint = "SHAppBarMessage", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern int SHAppBarMessage(int dwMessage, ref APPBARDATA pData);
        public const int ABM_GETTASKBARPOS = 0x5;
        #endregion    // end (Animating Window)

        #region Hook wrapper
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
        #endregion

        #region tray icon
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
        #endregion

        #region drag drop server
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);
        #endregion
    }
}