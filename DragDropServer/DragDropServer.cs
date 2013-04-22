//
//  DragDropServer.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

#if TRASH
using Cloud.Support;
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
using Cloud.Model;
using win_client.Common;
using System.Windows;


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

        static extern bool OpenProcessToken(AccessThisMethodFromwin_client.Static.NativeMethods readThis);

        static extern bool CloseHandle(AccessThisMethodFromwin_client.Static.NativeMethods readThis);

        static uint TOKEN_QUERY = 0x0008;

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
            _trace.writeToLog(9, "DragDropServer: DragDropServer: Entry.");
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
                _trace.writeToLog(9, "DragDropServer: StartDragDropServer: Entry.");
                if (IsStarted)
                {
                    _trace.writeToLog(9, "DragDropServer: StartDragDropServer: Return. Already started.");
                    return;
                }
                IsStarted = true;

                _currentUser = RemoteHooking.GetProcessIdentity(RemoteHooking.GetCurrentProcessId()).Name;
                _trace.writeToLog(9, "DragDropServer: StartDragDropServer: Current user: {0}.", _currentUser);

                // Start a timer to watch this user's processes and inject them as they start.
                _timerProcessUpdate = new System.Threading.Timer(new System.Threading.TimerCallback(OnTimerProcessUpdate), null, 0, 250);

                // Start a timer to process the inbound queue of injection messages.
                _timerCheckInjectionMsgQueue = new System.Threading.Timer(new System.Threading.TimerCallback(OnTimerCheckInjectionMsgQueue), null, 100, 100);

                // Create the server end of the IPC notification channel.
                _trace.writeToLog(9, "DragDropServer: StartDragDropServer: Create the server IPC channel end.");
                _hookServer = RemoteHooking.IpcCreateServer<DragDropInterface>(ref _channelName, WellKnownObjectMode.Singleton);
                if (_hookServer == null)
                {
                    _trace.writeToLog(9, "DragDropServer: StartDragDropServer: ERROR: Server IPC channel end null.");
                }
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
                        _trace.writeToLog(9, "DragDropServer: OnTimerCheckInjectionMsgQueue: Send 'Enter' message to PageInvisible.");
                        CLAppMessages.Message_DragDropServer_ShouldShowSystrayDropWindow.Send(operation);
                    }
                    else if (operation.Action.Equals("Leave", StringComparison.InvariantCulture))
                    {
                        // Send a message to the systray drop window to hide itself.
                        _trace.writeToLog(9, "DragDropServer: OnTimerCheckInjectionMsgQueue: Send 'Leave' message to systray Drop window.");
                        CLAppMessages.Message_DragDropServer_ShouldHideSystrayDropWindow.Send(operation);
                    }
                    else
                    {
                        _trace.writeToLog(9, "DragDropServer: OnTimerCheckInjectionMsgQueue: ERROR: Invalid message: {0].", operation.Action);
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
            _timerProcessUpdate.Change(Timeout.Infinite, Timeout.Infinite);        // disable the timer

            // Just return if not started.
            if (!IsStarted)
            {
                return;
            }

            // Enumerate all of the processes for this user.  Inject any new processes.
            //Replaced: ProcessInfo[] processArray = (ProcessInfo[])RemoteHooking.ExecuteAsService<DragDropServer>("EnumProcesses", _currentUser);
            ProcessInfo[] processArray = EnumProcesses(_currentUser);
            CurrentProcesses.Clear();
            for (int i = 0; i < processArray.Length; i++)
            {
                CurrentProcesses.Add(processArray[i]);  // Add this process to the current snapshot

                // Inject this process if it is new
                bool processAdded = false;
                lock (Locker)
                {
                    if (!HookedProcesses.Contains(processArray[i].Id))
                    {
                        _trace.writeToLog(9, "DragDropServer: OnTimerProcessUpdate: New process <{0}> with ID: {1}.", processArray[i].FileName, processArray[i].Id);
                        HookedProcesses.Add(processArray[i].Id); // this will ensure that Ping() returns true...
                        processAdded = true;
                    }
                }

                // Inject this process if we should
                if (processAdded)
                {
                    // Inject the enumerated process.  Get the full path of the dll.
                    string fullPathDragDropInjectionDll = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
                    fullPathDragDropInjectionDll = fullPathDragDropInjectionDll.Replace("file:\\", String.Empty);  // remove the file:\ from the beginning to get c:\....
                    string fullPathDragDropInjectionDll64 = fullPathDragDropInjectionDll + "\\DragDropInjectionx64.dll";
                    string fullPathDragDropInjectionDll86 = fullPathDragDropInjectionDll + "\\DragDropInjectionx86.dll";
                    _trace.writeToLog(9, "DragDropServer: OnTimerProcessUpdate: Injection dll path64: <{0}>. path86: <{1}>.", fullPathDragDropInjectionDll64, fullPathDragDropInjectionDll86);

                    try
                    {
                        _trace.writeToLog(9, "DragDropServer: OnTimerProcessUpdate: Inject the process.");
                        RemoteHooking.Inject(
                            processArray[i].Id,
                            fullPathDragDropInjectionDll86,   // 32-bit version (the same because AnyCPU)
                            fullPathDragDropInjectionDll64,   // 64-bit version (the same because AnyCPU)
                            // the optional parameter list...
                            _channelName);
                        _trace.writeToLog(9, "DragDropServer: OnTimerProcessUpdate: After injecting the process.");
                    }
                    catch(Exception ex)
                    {
                        lock (Locker)
                        {
                            HookedProcesses.Remove(processArray[i].Id);
                        }
                        CLError error = ex;
                        _trace.writeToLog(1, "DragDropServer: OnTimerProcessUpdate: ERROR. Exception.  Msg: <{0}>. Code: {1}.", error.PrimaryException.Message, error.PrimaryException.Code);
                    }
                }
            }

            // Enumerate all of the processes that have already been hooked.  Remove the item if the process has exited.
            Int32[] pidsToRemove = new Int32[0];
            lock (Locker)
            {
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
            }

            // Now remove the pids from the hooked list.
            lock (Locker)
            {
                for (Int32 i = 0; i < pidsToRemove.Length; i++)
                {
                    _trace.writeToLog(1, "DragDropServer: OnTimerProcessUpdate: Remove process with ID: {0}.", pidsToRemove[i]);
                    HookedProcesses.Remove(pidsToRemove[i]);
                }
            }

            // Let the timer fly again.
            _timerProcessUpdate.Change(250, 250);           // enable the timer
        }

        /// <summary>
        /// Stop the service.
        /// </summary>
        public void StopDragDropServer()
        {
            lock (Locker)
            {
                _trace.writeToLog(1, "DragDropServer: StopDragDropServer: Entry.");
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

        public static ProcessInfo[] EnumProcesses(string currentUser)
        {
            List<ProcessInfo> Result = new List<ProcessInfo>();
            Process[] ProcList = Process.GetProcesses();

            for (int i = 0; i < ProcList.Length; i++)
            {
                Process Proc = ProcList[i];

                try
                {
                    ProcessInfo Info = new ProcessInfo();

                    // This might cause an exception for an unprivileged user.  Ignore it
                    Info.FileName = Proc.MainModule.FileName;
                    Info.Id = Proc.Id;
                    Info.Is64Bit = RemoteHooking.IsX64Process(Proc.Id);

                    // Get the user name (owner of this process)
                    // The OpenProcessToken() function might cause an exception for an unprivileged user.  Ignore it.
                    IntPtr ph = IntPtr.Zero;
                    OpenProcessToken(Proc.Handle, TOKEN_QUERY, out ph);
                    WindowsIdentity wi = new WindowsIdentity(ph);

                    // Changed: Info.User = RemoteHooking.GetProcessIdentity(Proc.Id).Name;
                    Info.User = wi.Name;

                    if (Info.User == currentUser)
                    {
                        Result.Add(Info);
                    }
                }
                catch
                {
                    // Ignore.  All processes that are not owned by this user will take an exception and be ignored.
                }
            }

            return Result.ToArray();
        }
    }
}
#endif // TRASH
