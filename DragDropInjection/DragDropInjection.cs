using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EasyHook;
using win_client.DragDropServer;
using System.Security.Permissions;


namespace DragDropInjection
{
    public class DragDropInjection : EasyHook.IEntryPoint
    {
        public DragDropInterface Interface = null;
        public LocalHook DoDragDropHook = null;
        Stack<string> Queue = new Stack<string>();

        public DragDropInjection(
            RemoteHooking.IContext InContext,
            String InChannelName)
        {
            Interface = RemoteHooking.IpcConnectClient<DragDropInterface>(InChannelName);

            Interface.Ping(RemoteHooking.GetCurrentProcessId());
        }

        public void Run(
            RemoteHooking.IContext InContext,
            String InArg1)
        {
            try
            {
                DoDragDropHook = LocalHook.Create(
                    LocalHook.GetProcAddress("ole32.dll", "DoDragDrop"),
                    new DDoDragDrop(DoDragDrop_HookCallback),
                    this);

                /*
                 * Don't forget that all hooks will start deaktivated...
                 * The following ensures that all threads are intercepted:
                 */
                DoDragDropHook.ThreadACL.SetExclusiveACL(new Int32[1]);
            }
            catch (Exception e)
            {
                /*
                    Now we should notice our host process about this error...
                 */
                Interface.ReportError(RemoteHooking.GetCurrentProcessId(), e);

                return;
            }


            // wait for host process termination...
            try
            {
                while (Interface.Ping(RemoteHooking.GetCurrentProcessId()))
                {
                    Thread.Sleep(50);

                    // transmit newly monitored file accesses...
                    lock (Queue)
                    {
                        if (Queue.Count > 0)
                        {
                            String[] Package = null;

                            Package = Queue.ToArray();

                            Queue.Clear();

                            Interface.OnDoDragDropHookActions(RemoteHooking.GetCurrentProcessId(), Package);
                        }
                    }
                }
            }
            catch
            {
                // NET Remoting will raise an exception if host is unreachable
            }
        }

        //TODO: Needed? [UIPermissionAttribute(SecurityAction.Demand, Clipboard = UIPermissionClipboard.OwnClipboard)]
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate Int32 DDoDragDrop(
            IntPtr InData,
            IntPtr InDropSource,
            UInt32 InOkEffects,
            out UInt32[] OutEffect);

        // just use a P-Invoke implementation to get native API access from C# (this step is not necessary for C++.NET)
        [DllImport("ole32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern Int32 DoDragDrop(
            IntPtr InData,
            IntPtr InDropSource,
            UInt32 InOkEffects,
            out UInt32[] OutEffect);

        // This is where the hook is driven.
        static Int32 DoDragDrop_HookCallback(
            IntPtr InData,
            IntPtr InDropSource,
            UInt32 InOkEffects,
            out UInt32[] OutEffect)
        {
            // Tell the application that DoDragDrop has started.
            try
            {
                DragDropInjection This = (DragDropInjection)HookRuntimeInfo.Callback;

                lock (This.Queue)
                {
                    if (This.Queue.Count < 1000)
                        This.Queue.Push("Enter");
                }
            }
            catch
            {
            }

            // call original API...
            Int32 rc = DoDragDrop(InData, InDropSource, InOkEffects, out OutEffect);

            // Tell the application that DoDragDrop has stopped.
            try
            {
                DragDropInjection This = (DragDropInjection)HookRuntimeInfo.Callback;

                lock (This.Queue)
                {
                    if (This.Queue.Count < 1000)
                        This.Queue.Push("Leave");
                }
            }
            catch
            {
            }

            // Return the result of the base DoDragDrop call
            return rc;
        }
    }
}
