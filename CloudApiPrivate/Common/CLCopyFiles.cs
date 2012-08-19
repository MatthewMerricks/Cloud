//
//  CLCopyFiles.cs
//  Cloud Windows
//
//  Created by BobS on 7/2/12.
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using System.Threading.Tasks;
using System.IO;
using CloudApiPublic.Model;
using CloudApiPublic.Support;

namespace CloudApiPrivate.Common
{
    public class CLCopyFiles
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
                CLError err = ex;
                CLTrace.Instance.writeToLog(1, "CLCopyFiles: CopyFileOrDirectoryWithUi: ERROR: Exception. Msg: <{0}>, Code: {1} while copying file <{2}> to file <{3}>.", 
                    err.errorDescription, err.errorCode, source, target);
	        }
        }
    }
}
