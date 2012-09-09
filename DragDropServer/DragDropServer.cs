//
//  DragDropServer.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Diagnostics;
using System.Threading;
using EasyHook;
using System.Security.Principal;
using CloudApiPublic.Model;
using win_client.Common;


namespace win_client.DragDropServer
{
    public sealed class DragDropServer
    {
        private static DragDropServer _instance = null;
        private static object _instanceLocker = new object();
        private static CLTrace _trace = CLTrace.Instance;
        private String _channelName = null;
        private IpcServerChannel _hookServer;
        private System.Threading.Timer _timerProcessUpdate = null;
        private System.Threading.Timer _timerCheckInjectionMsgQueue = null;
        private string _currentUser = null;

        public List<ProcessInfo> CurrentProcesses { get; private set; }
        public List<Int32> HookedProcesses { get; private set; }
        public object Locker { get; private set; }
        public bool IsStarted { get; private set; }
        public Queue<DragDropOperation> InjectionQueue { get; set; }



        /// <summary>
        /// Access Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static DragDropServer Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new DragDropServer();

                        // Initialize at first Instance access here
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private DragDropServer()
        {
            // Initialize members, etc. here (at static initialization time).
            this.CurrentProcesses = new List<ProcessInfo>();
            this.Locker = new object();
            this. HookedProcesses = new List<Int32>();
            this.IsStarted = false;
            this.InjectionQueue = new Queue<DragDropOperation>();
        }

        /// <summary>
        /// Start the service.
        /// </summary>
        public void StartDragDropServer()
        {
            lock (Locker)
            {
                IsStarted = true;
                _currentUser = RemoteHooking.GetProcessIdentity(RemoteHooking.GetCurrentProcessId()).Name;

                // Start a timer to watch this user's processes and inject them as they start.
                _timerProcessUpdate = new System.Threading.Timer(new System.Threading.TimerCallback(OnTimerProcessUpdate), null, 0, 250);

                // Start a timer to process the inbound queue of injection messages.
                _timerCheckInjectionMsgQueue = new System.Threading.Timer(new System.Threading.TimerCallback(OnTimerCheckInjectionMsgQueue), null, 100, 100);

                // Create the server end of the IPC notification channel.
                _hookServer = RemoteHooking.IpcCreateServer<DragDropInterface>(ref _channelName, WellKnownObjectMode.Singleton);
            }
        }

        /// <summary>
        /// Timer callback: Check the injection message queue for inbound messages.
        /// </summary>
        /// <param name="state"></param>
        private void OnTimerCheckInjectionMsgQueue(object state)
        {
            lock (Locker)
            {
                _timerCheckInjectionMsgQueue.Change(Timeout.Infinite, Timeout.Infinite);        // disable the timer

                // Just return if not started.
                if (!IsStarted)
                {
                    return;
                }

                // Loop removing items enqueued by the injection dll.
                while (InjectionQueue.Count > 0)
                {
                    DragDropOperation operation = InjectionQueue.Dequeue();

                    // Send the appropriate message
                    if (operation.Action.Equals("Enter", StringComparison.InvariantCulture))
                    {
                      // Send a message to PageInvisible to show the systray drop window.
                      CLAppMessages.Message_DragDropServer_ShouldShowSystrayDropWindow.Send(operation);
                    }
                    else if (operation.Action.Equals("Leave", StringComparison.InvariantCulture))
                    {
                      // Send a message to the systray drop window to hide itself.
                      CLAppMessages.Message_DragDropServer_ShouldHideSystrayDropWindow.Send(operation);
                    }
                }

                _timerProcessUpdate.Change(100, 100);           // enable the timer
            }
        }

        /// <summary>
        /// Timer callback: Update our process list.  Inject new processes.  Stop tracking old processes.
        /// </summary>
        /// <param name="state"></param>
        private void OnTimerProcessUpdate(object state)
        {
            lock (Locker)
            {
                _timerProcessUpdate.Change(Timeout.Infinite, Timeout.Infinite);        // disable the timer

                // Just return if not started.
                if (!IsStarted)
                {
                    return;
                }

                // Enumerate all of the processes for this user.  Inject any new processes.
                ProcessInfo[] processArray = (ProcessInfo[])RemoteHooking.ExecuteAsService<DragDropServer>("EnumProcesses");
                CurrentProcesses.Clear();
                for (int i = 0; i < processArray.Length; i++)
                {
                    CurrentProcesses.Add(processArray[i]);  // Add this process to the current snapshot

                    // Inject this process if it is new
                    if (!HookedProcesses.Contains(processArray[i].Id))
                    {
                        // Inject the enumerated process
                        HookedProcesses.Add(processArray[i].Id); // this will ensure that Ping() returns true...
                        try
                        {
                            RemoteHooking.Inject(
                                processArray[i].Id,
                                "DragDropInjection.dll", // 32-bit version (the same because AnyCPU)
                                "DragDropInjection.dll", // 64-bit version (the same because AnyCPU)
                                // the optional parameter list...
                                _channelName);
                        }
                        catch(Exception ex)
                        {
                            HookedProcesses.Remove(processArray[i].Id);
                            CLError error = ex;
                            _trace.writeToLog(1, "DragDropServer: OnTimerProcessUpdate: ERROR. Exception.  Msg: <[0]>. Code: {1}.", error.errorDescription, error.errorCode);
                        }
                    }
                }

                // Enumerate all of the processes that have already been hooked.  Remove the item if the process has exited.
                Int32[] pidsToRemove = new Int32[0];
                foreach (Int32 pid in HookedProcesses)
                {
                    // Look for this PID in the current process list snapshot.
                    bool found = false;
                    foreach (ProcessInfo pInfo in CurrentProcesses)
                    {
                        if (pInfo.Id == pid)
                        {
                            found = true;
                            break;
                        }
                    }

                    // Add this pid to the list of pids to remove from the hooked process list if it is not in the current snapshot.
                    if (!found)
                    {
                        pidsToRemove[pidsToRemove.Length] = pid;
                    }
                }

                // Now remove the pids from the hooked list.
                for (Int32 i = 0; i < pidsToRemove.Length; i++)
                {
                    HookedProcesses.Remove(pidsToRemove[i]);
                }

                _timerProcessUpdate.Change(250, 250);           // enable the timer
            }
        }

        /// <summary>
        /// Stop the service.
        /// </summary>
        public void StopDragDropServer()
        {
            lock (Locker)
            {
                IsStarted = false;
            }
        }

        [Serializable]
        public class ProcessInfo
        {
            public String FileName;
            public Int32 Id;
            public Boolean Is64Bit;
            public String User;
        }

        public static ProcessInfo[] EnumProcesses()
        {
            List<ProcessInfo> Result = new List<ProcessInfo>();
            Process[] ProcList = Process.GetProcesses();

            for (int i = 0; i < ProcList.Length; i++)
            {
                Process Proc = ProcList[i];

                try
                {
                    ProcessInfo Info = new ProcessInfo();

                    Info.FileName = Proc.MainModule.FileName;
                    Info.Id = Proc.Id;
                    Info.Is64Bit = RemoteHooking.IsX64Process(Proc.Id);
                    Info.User = RemoteHooking.GetProcessIdentity(Proc.Id).Name;

                    Result.Add(Info);
                }
                catch
                {
                }
            }

            return Result.ToArray();
        }
    }
}
