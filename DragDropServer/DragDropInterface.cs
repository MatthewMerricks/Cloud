//
//  DragDropInterface.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace win_client.DragDropServer
{
    public class DragDropOperation
    {
        public readonly String Action;
        public readonly DateTime Timestamp = DateTime.Now;
        public readonly Int32 ClientPID;

        public DragDropOperation(
            Int32 InClientPID,
            String InAction)
        {
            ClientPID = InClientPID;
            Action = InAction;
        }
    }

    /*
     * This is the class where our clients will connect to!
     * 
     * Please note that setting any breakpoint here will cause the related
     * thread in the client process to block until you continue execution!
     * So don't wonder if your browser (for example) hangs when you set a 
     * breakpoint ;-)... Let's say you can debug a part of the code the client
     * is executing (that's not technically correct)
     * 
     * In Windows 2000 debugging the following seems to cause problems. 
     */
    public class DragDropInterface : MarshalByRefObject
    {
        public void ReportError(
            Int32 InClientPID,
            Exception e)
        {
            MessageBox.Show(e.ToString(), "The DragDrop injection process (" + InClientPID + ") has reported an error...", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
        }

        public bool Ping(Int32 InClientPID)
        {
            /*
             * We should just check if the client is still in our list
             * of hooked processes...
             */
            lock (DragDropServer.Instance.Locker)
            {
                return DragDropServer.Instance.HookedProcesses.Contains(InClientPID);
            }
        }

        // This event is driven by the injected code.  The actions are a list of
        // the strings "Enter" and "Leave".  "Enter" means DoDragDrop is active.
        // "Leave" means that DoDragDrop is no longer active.  Send a message
        // for each of these actions.
        public void OnDoDragDropHookActions(
            Int32 InClientPID,
            String[] InActions)
        {
            if (DragDropServer.Instance.IsStarted)
            {
                lock (DragDropServer.Instance.Locker)
                {
                    for (int i = 0; i < InActions.Length; i++)
                    {
                        DragDropServer.Instance.InjectionQueue.Enqueue(
                            new DragDropOperation(
                                InClientPID,
                                InActions[i]
                            ));
                    }
                }
            }
        }
    }
}
