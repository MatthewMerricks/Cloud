//
// NamedPipeServerContextMenu.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPrivate.Common;
using CloudApiPrivate.Model.Settings;
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
            //_trace.writeToLog(9, "NamedPipeServerContextMenu: ProcessClientCommunication: Entry.");
            NamedPipeServerContextMenu_UserState UserState = userState as NamedPipeServerContextMenu_UserState;
            if (UserState != null)
            {
                // try/catch which silences errors and stops badging functionality (should never error here)
                try
                {
                    // We got a connection.  Read the JSON from the pipe and deserialize it to a POCO.
                    //_trace.writeToLog(9, "NamedPipeServerContextMenu: ProcessClientCommunication: Read the info from the pipe.");
                    StreamReader reader = new StreamReader(pipeStream);
                    ContextMenuObject msg = JsonConvert.DeserializeObject<ContextMenuObject>(reader.ReadLine());

                    // Copy the files to the Cloud root directory.
                    //_trace.writeToLog(9, "NamedPipeServerContextMenu: ProcessClientCommunication: Got the info.  Copy the files.");
                    ContextMenuCopyFiles(msg, UserState.FilePathCloudDirectory);
                }
                catch (Exception ex)
                {
                    CLError error = ex;
                    _trace.writeToLog(9, "NamedPipeServerContextMenu: ProcessClientCommunication: ERROR: Exception: Msg: <{0}>, Code: {1}.", error.errorDescription, error.errorCode);
                }
            }
            else
            {
                _trace.writeToLog(9, "NamedPipeServerContextMenu: ProcessClientCommunication: ERROR: Throw: userState must be castable to NamedPipeServerContextMenu_UserState");
                throw new NullReferenceException("userState must be castable to NamedPipeServerContextMenu_UserState");
            }
        }

        /// <summary>
        /// Copy the selected files to the Cloud root directory.
        /// </summary>
        /// <param name="returnParams"></param>
        private void ContextMenuCopyFiles(ContextMenuObject msg, FilePath filePathCloudDirectory)
        {
            //_trace.writeToLog(9, "NamedPipeServerContextMenu: ContextMenuCopyFiles: Entry.");
            foreach (string path in msg.asSelectedPaths)
            {
                // Remove any trailing backslash
                //_trace.writeToLog(9, "NamedPipeServerContextMenu: ContextMenuCopyFiles: Process path <{0}>.", path);
                string source = path.TrimEnd(new char[] { '\\', '/' });

                // Get the filename.ext of the source path.
                string filenameExt = Path.GetFileName(source);

                // Build the target path
                string target = filePathCloudDirectory.ToString() + "\\" + filenameExt;

                // Copy it.
                //_trace.writeToLog(9, "NamedPipeServerContextMenu: ContextMenuCopyFiles: Schedule the copy.");
                Dispatcher mainDispatcher = Application.Current.Dispatcher;
                mainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    //_trace.writeToLog(9, "NamedPipeServerContextMenu: ContextMenuCopyFiles: Copy the file from <{0}> to <{1}>.", source, target);
                    CLCopyFiles.CopyFileOrDirectoryWithUi(source, target);
                }));
            }

            // Show the cloud folder
            //_trace.writeToLog(9, "NamedPipeServerContextMenu: ContextMenuCopyFiles: Show the Cloud folder.");
            CLShortcuts.LaunchExplorerToFolder(Settings.Instance.CloudFolderPath);

            //_trace.writeToLog(9, "NamedPipeServerContextMenu: ContextMenuCopyFiles: Return.");
        }
    }
}
