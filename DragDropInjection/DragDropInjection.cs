//
//  DragDropInjection.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.
//

#if TRASH
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using EasyHook;
using win_client.DragDropServer;
using System.Security.Permissions;
using Cloud.Support;


namespace DragDropInjection
{
    public class DragDropInjection : EasyHook.IEntryPoint
    {
        public DragDropInterface Interface = null;
        public LocalHook DoDragDropHook = null;
        private static CLTrace _trace = CLTrace.Instance;
        Stack<string> Queue = new Stack<string>();

        public DragDropInjection(
            RemoteHooking.IContext InContext,
            String InChannelName)
        {
            _trace.writeToLog(9, "DragDropInjection: DragDropInjection: Entry. Connect to server IPC.");
            Interface = RemoteHooking.IpcConnectClient<DragDropInterface>(InChannelName);

            _trace.writeToLog(9, "DragDropInjection: DragDropInjection: Ping server.");
            Interface.Ping(RemoteHooking.GetCurrentProcessId());
            _trace.writeToLog(9, "DragDropInjection: DragDropInjection: Exit.");
        }

        public void Run(
            RemoteHooking.IContext InContext,
            String InArg1)
        {
            try
            {
                _trace.writeToLog(9, "DragDropInjection: Run. Entry.");
                DoDragDropHook = LocalHook.Create(
                    LocalHook.GetProcAddress("ole32.dll", "DoDragDrop"),
                    new DDoDragDrop(DoDragDrop_HookCallback),
                    this);

                /*
                 * Don't forget that all hooks will start deaktivated...
                 * The following ensures that all threads are intercepted:
                 */
                _trace.writeToLog(9, "DragDropInjection: Run. Activate the hook.");
                DoDragDropHook.ThreadACL.SetExclusiveACL(new Int32[1]);
            }
            catch (Exception e)
            {
                /*
                    Now we should notice our host process about this error...
                 */
                _trace.writeToLog(9, "DragDropInjection: Run. ERROR: Exception. Msg: {0}.", e.Message);
                Interface.ReportError(RemoteHooking.GetCurrentProcessId(), e);

                _trace.writeToLog(9, "DragDropInjection: Run. Return in catch.");
                return;
            }


            // wait for host process termination...
            try
            {
                _trace.writeToLog(9, "DragDropInjection: Run. Wait for host process termination.");
                while (Interface.Ping(RemoteHooking.GetCurrentProcessId()))
                {
                    Thread.Sleep(50);

                    // transmit newly monitored file accesses...
                    lock (Queue)
                    {
                        if (Queue.Count > 0)
                        {
                            _trace.writeToLog(9, "DragDropInjection: Run. Send %d messages to the server.", Queue.Count);
                            String[] Package = null;

                            Package = Queue.ToArray();

                            Queue.Clear();

                            Interface.OnDoDragDropHookActions(RemoteHooking.GetCurrentProcessId(), Package);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // NET Remoting will raise an exception if host is unreachable
                _trace.writeToLog(9, "DragDropInjection: Run. ERROR. Exception (2). Msg: {0}.", ex.Message);
            }
            _trace.writeToLog(9, "DragDropInjection: Run. Return.");
        }

        static extern bool DDoDragDrop(AccessThisMethodFromDragDropInjection.Static.NativeMethods readThis);

        static extern bool DoDragDrop(AccessThisMethodFromDragDropInjection.Static.NativeMethods readThis);

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
                _trace.writeToLog(9, "DragDropInjection: DoDragDrop_HookCallback. Entry.");
                DragDropInjection This = (DragDropInjection)HookRuntimeInfo.Callback;

                lock (This.Queue)
                {
                    if (This.Queue.Count < 1000)
                    {
                        _trace.writeToLog(9, "DragDropInjection: DoDragDrop_HookCallback. Enqueue 'Enter'.");
                        This.Queue.Push("Enter");
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(9, "DragDropInjection: DoDragDrop_HookCallback. ERROR. Exception. Msg: {0}.", ex.Message);
            }

            // call original API...
            _trace.writeToLog(9, "DragDropInjection: DoDragDrop_HookCallback. Call base DoDragDrop.");
            Int32 rc = DoDragDrop(InData, InDropSource, InOkEffects, out OutEffect);
            _trace.writeToLog(9, "DragDropInjection: DoDragDrop_HookCallback. Back from base DoDragDrop.");

            // Tell the application that DoDragDrop has stopped.
            try
            {
                DragDropInjection This = (DragDropInjection)HookRuntimeInfo.Callback;

                lock (This.Queue)
                {
                    if (This.Queue.Count < 1000)
                    {
                        _trace.writeToLog(9, "DragDropInjection: DoDragDrop_HookCallback. Enqueue 'Leave'.");
                        This.Queue.Push("Leave");
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(9, "DragDropInjection: DoDragDrop_HookCallback. ERROR. Exception (2). Msg: {0}.", ex.Message);
            }

            // Return the result of the base DoDragDrop call
            _trace.writeToLog(9, "DragDropInjection: DoDragDrop_HookCallback. Return result %d.", rc);
            return rc;
        }
    }
}
#endif // TRASH
