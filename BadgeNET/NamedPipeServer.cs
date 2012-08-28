//
// NamedPipeServer.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using CloudApiPublic.Support;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace BadgeNET
{
    public static class NamedPipeServerExtensions
    {
        /// <summary>
        /// Extension method for NamedPipeServerStream to allow WaitForConnection to be cancelled so the thread can exit. 
        /// </summary>
        /// <param name="stream">The subject NamedPipeServerStream class to be extended.</param>
        /// <param name="cancelEvent">The cancellation event.  Set this event to cancel the wait for client connection.</param>
        public static void WaitForConnectionEx(this NamedPipeServerStream stream, ManualResetEvent cancelEvent) 
        { 
            Exception e = null; 
            AutoResetEvent connectEvent = new AutoResetEvent(false); 
            stream.BeginWaitForConnection(ar => 
            { 
                try 
                { 
                    stream.EndWaitForConnection(ar); 
                } 
                catch (Exception er) 
                { 
                    e = er; 
                } 

                connectEvent.Set(); 
            }, null);

            if (WaitHandle.WaitAny(new WaitHandle[] { connectEvent, cancelEvent }) == 1)
            {
                stream.Close();
                connectEvent.Dispose();
            }

            if (e != null)
            {
                throw e; // rethrow exception 
            }
        }
    }


    /// <summary>
    /// NamedPipeServer abstract class.  Derive from this class and implement the ProcessClientCommunication method.
    /// </summary>
    abstract public class NamedPipeServer
    {
        private bool _running;
        private readonly object _runningLocker = new object();
        private EventWaitHandle _terminateHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private ManualResetEvent _resetEvent = null;

        public string PipeName { get; set; }
        public object UserState { get; set; }

        /// <summary>
        /// Check to see if the server thread is running.  Check under a lock.
        /// </summary>
        /// <returns></returns>
        private bool CheckRunning()
        {
            lock (_runningLocker)
            {
                return _running;
            }
        }

        /// <summary>
        /// The main server loop.  This loop runs under the server thread started by the Run() method.  It loops accepting
        /// connections from clients.  Each incoming connection will spin off another thread to process that client connection.
        /// After spinning off the connection processing thread, the server thread loops back to wait for a connection from
        /// another client.
        /// </summary>
        private void ServerLoop()
        {
            CLTrace.Instance.writeToLog(1, "NamedPipeServer: ServerLoop: Entry.");
            while (CheckRunning())
            {
                CLTrace.Instance.writeToLog(1, "NamedPipeServer: ServerLoop: Call ProcessNextClient.");
                ProcessNextClient();
                CLTrace.Instance.writeToLog(1, "NamedPipeServer: ServerLoop: Back from ProcessNextClient.");
            }

            CLTrace.Instance.writeToLog(1, "NamedPipeServer: ServerLoop: Signal Stop.");
            _terminateHandle.Set();
            CLTrace.Instance.writeToLog(1, "NamedPipeServer: ServerLoop: Exit thread.");
        }

        
        /// <summary>
        /// Call this method to start the named pipe server thread.
        /// </summary>
        public void Run()
        {
            CLTrace.Instance.writeToLog(1, "NamedPipeServer: Run: Entry.");
            _running = true;
            _resetEvent = new ManualResetEvent(false);

            // Start the server loop
            (new Thread(ServerLoop)).Start();
        }

        /// <summary>
        /// Call this method to stop the server thread and any running clients.
        /// </summary>
        public void Stop()
        {
            CLTrace.Instance.writeToLog(1, "NamedPipeServer: Stop: Entry.");
            lock (_runningLocker)
            {
                _running = false;
                if (_resetEvent != null)
                {
                    _resetEvent.Set();                                          // let the WaitForConnection exit so the thread can stop
                }
            }
            bool receivedSignal = _terminateHandle.WaitOne(150);           // Don't hang forever
            if (!receivedSignal)
            {
                // Timed out.
                CLTrace.Instance.writeToLog(1, "NamedPipeServer: Stop: Thread timed out."); 
            }
            _terminateHandle.Dispose();
        }

        public void ProcessClientThread(object o)
        {
            CLTrace.Instance.writeToLog(1, "NamedPipeServer: ProcessClientThread: Entry.");
            NamedPipeServerStream pipeStream = (NamedPipeServerStream)o;

            // Handle the client communication here.
            ProcessClientCommunication(pipeStream, UserState);

            pipeStream.Close();
            pipeStream.Dispose();
            CLTrace.Instance.writeToLog(1, "NamedPipeServer: ProcessClientThread: Exit thread.");
        }

        abstract public void ProcessClientCommunication(NamedPipeServerStream pipeStream, object userState);

        public void ProcessNextClient()
        {
            try
            {
                CLTrace.Instance.writeToLog(1, "NamedPipeServer: ProcessNextClient: Entry.");
                NamedPipeServerStream pipeStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 254, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                CLTrace.Instance.writeToLog(1, "NamedPipeServer: ProcessNextClient: WAIT for the next client connection.");

                lock(_runningLocker)
                {
                    _resetEvent = new ManualResetEvent(false);
                }

                pipeStream.WaitForConnectionEx(_resetEvent);

                lock(_runningLocker)
                {
                    _resetEvent.Dispose();
                    _resetEvent = null;
                }
                
                //Spawn a new thread for each request and continue waiting 
                CLTrace.Instance.writeToLog(1, "NamedPipeServer: ProcessNextClient: Got a client connection.  Start a thread to process it.");
                Thread t = new Thread(ProcessClientThread);
                t.Start(pipeStream);
            }
            catch (Exception ex)
            {
                // If there are no more avail connections (254 is in use already) then just keep looping until one is avail 
                CLError error = ex;
                CLTrace.Instance.writeToLog(1, "NamedPipeServer: ProcessNextClient: ERROR: Exception.  Msg: {0}. Code: {1}. Wait 50 ms and try again.", error.errorDescription, error.errorCode);
                Thread.Sleep(50);
            }
        }
    }
}
