﻿//
//  CLCopyFiles.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using System.Threading.Tasks;
using System.IO;
using Cloud.Model;

namespace Cloud.Support
{
    /// <summary>
    /// Static helper class providing methods to handle file/directory copying with full visual basic-style dialogs
    /// </summary>
    public static class CLCopyFiles
    {
        /// <summary>
        /// Copies a file or directory to the target.  Call with the full path of each.
        /// For example:
        ///   Copy a file:      CopyFileOrDirectoryWithUi("C:\A\B.txt", C:\User\Cloud\B.txt"
        ///   Copy a directory: CopyFileOrDirectoryWithUi("C:\A\B", C:\User\Cloud\B"
        /// Copies directories recursively.
        /// Note: This function puts up the Explorer "Copy File" UI, so it must be run on the UI thread.
        /// </summary>
        /// <param name="source">Full path of the source object.</param>
        /// <param name="target">Full path of the target object.</param>
        public static void CopyFileOrDirectoryWithUi(string source, string target)
        {
            try 
            {	        
                if (Directory.Exists(source))
                {
                    // This is a directory
                    FileSystem.CopyDirectory(source, target, UIOption.AllDialogs);
                }
                else if (File.Exists(source))
                {
                    // This is a file
                    FileSystem.CopyFile(source, target, UIOption.AllDialogs);
                }
                else
                {
                    // It doesn't exist.  Just log it.
                    CLTrace.Instance.writeToLog(1, "CLCopyFiles: CopyFileOrDirectoryWithUi: Source file <{0}> doesn't exist.", source);
                }
            }
	        catch (Exception ex)
	        {
                CLError error = ex;
                error.Log(CLTrace.Instance.TraceLocation, CLTrace.Instance.LogErrors);
                CLTrace.Instance.writeToLog(1,
                    "CLCopyFiles: CopyFileOrDirectoryWithUi: ERROR: Exception. Msg: <{0}>, Code: {1} while copying file <{2}> to file <{3}>.",
                    error.PrimaryException.Message,
                    error.PrimaryException.Code,
                    source,
                    target);
	        }
        }
    }
}