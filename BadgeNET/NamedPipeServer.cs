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
    abstract public class NamedPipeServer
    {
        private bool running;
        private Thread runningThread;
        private EventWaitHandle terminateHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        public string PipeName { get; set; }
        public object UserState { get; set; }

        void ServerLoop()
        {
            CLTrace.Instance.writeToLog(1, "NamedPipeServer: ServerLoop: Entry.");
            while (running)
            {
                CLTrace.Instance.writeToLog(1, "NamedPipeServer: ServerLoop: Call ProcessNextClient.");
                ProcessNextClient();
                CLTrace.Instance.writeToLog(1, "NamedPipeServer: ServerLoop: Back from ProcessNextClient.");
            }

            CLTrace.Instance.writeToLog(1, "NamedPipeServer: ServerLoop: Signal Stop.");
            terminateHandle.Set();
            CLTrace.Instance.writeToLog(1, "NamedPipeServer: ServerLoop: Exit thread.");
        }

        public void Run()
        {
            CLTrace.Instance.writeToLog(1, "NamedPipeServer: Run: Entry.");
            running = true;
            runningThread = new Thread(ServerLoop);
            runningThread.Start();
        }

        public void Stop()
        {
            CLTrace.Instance.writeToLog(1, "NamedPipeServer: Stop: Entry.");
            running = false;
            bool receivedSignal = terminateHandle.WaitOne(150);           // Don't hang forever
            if (!receivedSignal)
            {
                // Timed out.
                CLTrace.Instance.writeToLog(1, "NamedPipeServer: Stop: Thread timed out."); 
            }
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
                NamedPipeServerStream pipeStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 254);
                CLTrace.Instance.writeToLog(1, "NamedPipeServer: ProcessNextClient: WAIT for the next client connection.");
                pipeStream.WaitForConnection();

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
