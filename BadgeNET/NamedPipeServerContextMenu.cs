//
// NamedPipeServerContextMenu.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPrivate.Common;
using CloudApiPublic.Model;
using CloudApiPublic.Support;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace BadgeNET
{
    public class NamedPipeServerContextMenu_UserState
    {
        public FilePath FilePathCloudDirectory;
    }

    public class NamedPipeServerContextMenu : NamedPipeServer
    {
        CLTrace _trace = CLTrace.Instance;

        public override void ProcessClientCommunication(NamedPipeServerStream pipeStream, object userState)
        {
            _trace.writeToLog(1, "NamedPipeServerContextMenu: ProcessClientCommunication: Entry.");
            NamedPipeServerContextMenu_UserState UserState = userState as NamedPipeServerContextMenu_UserState;
            if (UserState != null)
            {
                // try/catch which silences errors and stops badging functionality (should never error here)
                try
                {
                    // We got a connection.  Read the JSON from the pipe and deserialize it to a POCO.
                    StreamReader reader = new StreamReader(pipeStream);
                    ContextMenuObject msg = JsonConvert.DeserializeObject<ContextMenuObject>(reader.ReadLine());

                    // Copy the files to the Cloud root directory.
                    ContextMenuCopyFiles(msg, UserState.FilePathCloudDirectory);
                }
                catch (Exception ex)
                {
                    CLError error = ex;
                    _trace.writeToLog(1, "IconOverlay: RunServerPipeContextMenu: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                }
            }
            else
            {
                throw new NullReferenceException("userState must be castable to NamedPipeServerContextMenu_UserState");
            }
        }

        /// <summary>
        /// Copy the selected files to the Cloud root directory.
        /// </summary>
        /// <param name="returnParams"></param>
        private void ContextMenuCopyFiles(ContextMenuObject msg, FilePath filePathCloudDirectory)
        {
            foreach (string path in msg.asSelectedPaths)
            {
                // Remove any trailing backslash
                string source = path.TrimEnd(new char[] { '\\', '/' });

                // Get the filename.ext of the source path.
                string filenameExt = Path.GetFileName(source);

                // Build the target path
                string target = filePathCloudDirectory.ToString() + "\\" + filenameExt;

                // Copy it.
                Dispatcher mainDispatcher = Application.Current.Dispatcher;
                mainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    CLCopyFiles.CopyFileOrDirectoryWithUi(source, target);
                }));
            }
        }
    }
}
