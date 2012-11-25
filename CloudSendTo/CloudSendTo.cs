//
//  CloudSendTo.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using CloudApiPrivate.Common;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Model;
using CloudApiPublic.Model;
using System.IO.Pipes;
using System.Security.Principal;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using ContextMenuNET;
using Microsoft.Win32.SafeHandles;
using CloudSendTo.Static;
using System.IO;
using System.Drawing;


namespace CloudSendTo
{
    class CloudSendTo
    {
        /// <summary>
        /// CloudSendTo.exe is launched by Explorer when the user selects a number of file/folder items in an Explorer window,
        /// right-clicks one of the selected items, selects the "Send-to" context menu item, and then the "Cloud Folder" item
        /// in the "Send-to" list.  Explorer simply passes a list of parameters which are the full paths of the selected items.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Trace.WriteLine("CloudSendTo: Main: Entry.");
            for (int i = 0; i < args.Length; i++)
            {
                Trace.WriteLine(String.Format("CloudSendTo: Main: Arg[{0}] = <{1}>.", i, args[i]));
            }

            // Just exit if there are no arguments
            if (args.Length <= 0)
            {
                Trace.WriteLine("CloudSendTo: Main: ERROR: No arguments.");
                return;
            }

            SafeFileHandle clientPipeHandle = null;
	        int createRetryCount = 3;
            string cloudExeFile = CLShortcuts.Get32BitProgramFilesFolderPath() + CLPrivateDefinitions.CloudFolderInProgramFiles + "\\" + CLPrivateDefinitions.CloudAppName + ".exe";
	
	        try
	        {
			    bool cloudProcessStarted = false;
			    int cloudStartTries = 0;
			    bool pipeConnectionFailed = false;

    	        // Check to see if the Cloud.exe program is there.  Don't add the menu item if we can't start Cloud.
		        Trace.WriteLine("CloudSendTo: Main: Check to make sure the Cloud.exe is present.");
                if (!File.Exists(cloudExeFile))
                {
			        Trace.WriteLine("CloudSendTo: Main: Cloud.exe is not present.  Return.");
                    return;
		        }

			    // Build the pipe name.  This will be (no escapes): "\\.\Pipe\<UserName>/BadgeCOM/ContextMenu"
			    string pipeName = "\\\\.\\Pipe\\" + Environment.UserName + "/BadgeCOM/ContextMenu";

			    // Try to open the named pipe identified by the pipe name.
			    while (!pipeConnectionFailed)
			    {
				    Trace.WriteLine("CloudSendTo: Main: Top of CreateFile loop.  Try to open the pipe.");
                    clientPipeHandle =  NativeMethods.CreateFile(
                                        fileName: pipeName, // Pipe name
					                    fileAccess: FileAccess.Write, // Write access
					                    fileShare: FileShare.None,       // No sharing
					                    securityAttributes: IntPtr.Zero, // Default security attributes
					                    creationDisposition: FileMode.Open, // Opens existing pipe
					                    flags: 0, // Default attributes
					                    template: IntPtr.Zero // No template file
					                    );
			
				    // If the pipe handle is opened successfully then break out to continue
				    if (clientPipeHandle != null && !clientPipeHandle.IsInvalid)
				    {
					    Trace.WriteLine("CloudSendTo: Main: Opened successfully.");
					    break;
				    }
				    // Pipe open not successful, find out if it should try again
				    else
				    {
					    // store not successful reason
                        int lastError = Marshal.GetLastWin32Error();
					    Trace.WriteLine(String.Format("CloudSendTo: Main: Open failed with code {0}.", lastError));

					    // This is the normal path when the application is not running (lastError will equal ERROR_FILE_NOT_FOUND)
					    // Start the cloud process on the first attempt or increment a retry counter up to a certain point;
					    // after 10 seconds of retrying, display an error message and stop trying
                        if (NativeMethods.ERROR_FILE_NOT_FOUND == lastError)
					    {
						    Trace.WriteLine("CloudSendTo: Main: The file was not found.  Have we started Cloud?");
						    if (!cloudProcessStarted)
						    {
							    // We haven't tried to start Cloud yet.  Maybe it is already running??
							    Trace.WriteLine("CloudSendTo: Main: See if Cloud is running.");
							    if (isCloudAppRunning())
							    {
								    Trace.WriteLine("CloudSendTo: Main: Cloud is running.");
								    cloudProcessStarted = true;
							    }
							    else
							    {
								    // Try to start Cloud.exe so it will open the other end of the pipe.
                                    Process processCloudExe = null;
                                    string errorMsg = String.Empty;
                                    try 
	                                {	        
								        Trace.WriteLine("CloudSendTo: Main: Cloud was not running.  Start it.");
                                        ProcessStartInfo startInfo = new ProcessStartInfo();
                                        startInfo.UseShellExecute = false;
                                        startInfo.FileName = cloudExeFile;
                                        startInfo.Arguments = String.Empty;
                                        processCloudExe = Process.Start(startInfo);
	                                }
	                                catch (Exception ex)
	                                {
                                        errorMsg = ex.Message;
								        Trace.WriteLine(String.Format("CloudSendTo: Main: ERROR: Exception. Msg: <{0}>.", errorMsg));
                                        processCloudExe = null;   // should not be necessary
	                                }

								    if (processCloudExe != null)
								    {
									    Trace.WriteLine("CloudSendTo: Main: Start was successful.");
									    cloudProcessStarted = true;
								    }
								    else
								    {
									    // Error from ExecuteProcess
									    Trace.WriteLine("CloudSendTo: Main: Error <{0}> from ExecuteProcess.  Tell the user.", errorMsg);
                                        MessageBox.Show("Cloud could not be started, operation cancelled. " + errorMsg, "Oh Snap!");

									    // Exit now
		    						    pipeConnectionFailed = true;
									    break;
								    }
							    }
						    }
						    else if (cloudStartTries > 99)
						    {
							    Trace.WriteLine("CloudSendTo: Main: Too many retries, and Cloud should be running.  Tell the user.");
                                MessageBox.Show("Cloud did not respond after ten seconds, operation cancelled.", "Oh Snap!");

							    // Exit now
		    				    pipeConnectionFailed = true;
							    break;
						    }
						    else
						    {
							    Trace.WriteLine("CloudSendTo: Main: Wait 100 ms.");
							    cloudStartTries++;
							    Thread.Sleep(100);
						    }

						    // Close the pipe handle.  It will be recreated when we loop back up.
                            clientPipeHandle.Close();
                            clientPipeHandle = null;
						    Trace.WriteLine("CloudSendTo: Main: Loop back up for a retry.");
					    }
					    // pipe is busy
                        else if (NativeMethods.ERROR_PIPE_BUSY == lastError)
					    {
						    // if waiting for a pipe does not complete in 2 seconds, exit  (by setting pipeConnectionFailed to true)
			    		    Trace.WriteLine("CloudSendTo: Main: Error is pipe_busy. Wait for it for 2 seconds.");
                            if (!NativeMethods.WaitNamedPipe(pipeName, 2000))
						    {
							    lastError = Marshal.GetLastWin32Error();
		    				    Trace.WriteLine(String.Format("CloudSendTo: Main: ERROR: after wait.  Code: {0}.  Maybe we should retry.", lastError));
							    if (createRetryCount-- > 0)
							    {
								    // We should retry
								    Trace.WriteLine("CloudSendTo: Main: Loop to retry the CreateFile.");
                                    clientPipeHandle.Close();
                                    clientPipeHandle = null;
							    }
							    else
							    {
								    Trace.WriteLine("CloudSendTo: Main: ERROR: Out of retries.  CreateFile failed.  Tell the user.");

								    // Tell the user with an ugly MessageBox!!!
                                    MessageBox.Show("Cloud is busy, operation cancelled: Error: " + lastError.ToString(), "Oh Snap!");

								    // Exit now
			    				    pipeConnectionFailed = true;
								    break;
							    }
						    }
						    else
						    {
							    // The wait succeeded.  We should retry the CreateFile
							    Trace.WriteLine("CloudSendTo: Main: Wait successful.  Loop to retry the CreateFile.");
                                clientPipeHandle.Close();
                                clientPipeHandle = null;
						    }
					    }
					    // unknown error
					    else
					    {
						    // Tell the user with an ugly MessageBox!!!
						    Trace.WriteLine("CloudSendTo: Main: Unknown error.  Tell the user.");
                            MessageBox.Show("Cloud is busy, operation cancelled: Error: " + lastError.ToString(), "Oh Snap!");

						    // Exit now
						    pipeConnectionFailed = true;
						    break;
					    }
				    }
			    }

			    // Gather and send the information to ContextMenuNET in Cloud.exe if we got a connection.
			    if (!pipeConnectionFailed)
			    {
				    // Get the coordinates of the current Explorer window
				    Trace.WriteLine("CloudSendTo: Main: Pipe connection successful.  Get the window info.");
                    IntPtr hwnd = NativeMethods.GetActiveWindow();

                    global::CloudSendTo.Static.NativeMethods.RECT rMyRect;
                    NativeMethods.GetClientRect(hwnd, out rMyRect);
                    Point rMyTopLeft = new Point(rMyRect.left, rMyRect.top);
                    Point rMyBottomRight = new Point(rMyRect.right, rMyRect.bottom);
                    NativeMethods.ClientToScreen(hwnd, ref rMyTopLeft);
                    NativeMethods.ClientToScreen(hwnd, ref rMyBottomRight);

				    // Put the information into a JSON object.  The formatted JSON will look like this:
				    // {
				    //		// Screen coordinates of the Explorer window.
				    //		"window_coordinates" : { "left" : 100, "top" : 200, "right" : 300, "bottom" : 400 },
				    //
				    //		"selected_paths" : [
				    //			"path 1",
				    //			"path 2",
				    //			"path 3"
				    //		]
				    // }

				    Trace.WriteLine(String.Format("CloudSendTo: Main: Add the window screen rectangle (LTRB): {0},{1},{2},{3}.", rMyRect.left, rMyRect.top, rMyRect.right, rMyRect.bottom));
                    ContextMenuObject root = new ContextMenuObject();
                    root.rectExplorerWindowCoordinates = new ContextMenuNET.RECT();
                    root.rectExplorerWindowCoordinates.left = rMyRect.left;
                    root.rectExplorerWindowCoordinates.top = rMyRect.top;
                    root.rectExplorerWindowCoordinates.right = rMyRect.right;
                    root.rectExplorerWindowCoordinates.bottom = rMyRect.bottom;

				    // Add the selected paths
				    Trace.WriteLine("CloudSendTo: Main: Add the selected paths.");
                    root.asSelectedPaths = new List<string>();
                    for (int i = 0; i < args.Length; i++)
                    {
                        root.asSelectedPaths.Add(args[i]);
                    }

                    // Serialize this object to JSON
                    string payloadToSend = JsonConvert.SerializeObject(root);

				    // Write it to Cloud.exe ContextMenuNET.
                    bool success = true;
                    try 
	                {
				        Trace.WriteLine("CloudSendTo: Main: Write the JSON to the pipe.");
                        NamedPipeServerStream pipeStream = new NamedPipeServerStream(direction: PipeDirection.Out, isAsync: false, isConnected: true, safePipeHandle: new SafePipeHandle(clientPipeHandle.DangerousGetHandle(), true));
                        StreamWriter writer = new StreamWriter(pipeStream);
                        Trace.WriteLine(String.Format("CloudSendTo: Main: Payload: <{0}>.", payloadToSend));
                        writer.WriteLine(payloadToSend);
                        writer.Flush();
	                }
	                catch (Exception ex)
	                {
				        Trace.WriteLine(String.Format("CloudSendTo: Main: ERROR: Exception(2). Msg: {0}.", ex.Message));
                        success = false;
	                }
                    
                    if (success)
				    {
					    // Successful
					    Trace.WriteLine("CloudSendTo: Main: Write successful.");
				    }
				    else
				    {
					    // Error writing to the pipe
                        int lastError = Marshal.GetLastWin32Error();
					    Trace.WriteLine(String.Format("CloudSendTo: Main: ERROR: Writing to pipe.  Code: {0}. Tell the user.", lastError));
                        MessageBox.Show("An error occurred while communicating with Cloud (2), operation cancelled: Error: " + lastError.ToString(), "Oh Snap!");

				    }
			    }
		    }

	        catch (Exception ex)
	        {
                Trace.WriteLine(String.Format("CloudSendTo: Main: ERROR: Exception. Msg: {0}.", ex.Message));
	        }

	        // Close the pipe handle
            if (clientPipeHandle != null)
	        {
		        Trace.WriteLine("CloudSendTo: Main: Close the pipe handle.");
                clientPipeHandle.Close();
                clientPipeHandle = null;
	        }

	        Trace.WriteLine("CloudSendTo: Main: Exit.");
        }

        /// <summary>
        /// Check to see if we are already running.
        /// </summary>
        private static bool isCloudAppRunning()
        {
            bool isCloudAppRunning = false;

            Process[] processes = Process.GetProcessesByName("Cloud.exe");
            string currentOwner = WindowsIdentity.GetCurrent().Name.ToString();
            var query = from p in processes
                        where currentOwner.ToLowerInvariant().
                        Contains(GetProcessOwner(p.Id).ToLowerInvariant())
                        select p;
            int instance = query.Count();
            if (instance > 1)
            {
                isCloudAppRunning = true;
            }

            return isCloudAppRunning;
        }

        /// <summary>
        /// Get the owner of this process.
        /// </summary>
        static string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    searcher.Dispose();
                    return argList[0];
                }
            }
            searcher.Dispose();
            return string.Empty;
        }
    }
}
