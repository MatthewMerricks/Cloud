//
//  CLSptConstants.cs
//  Cloud SDK Windows 
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudApiPublic.Model;

namespace CloudApiPublic.Support
{
    class CLSptFileCompare
    {
        /// <summary> 
        // Compare the contents of two files.
        /// </summary> 
        /// <param name="file1">The first file to compare.</param> 
        /// <param name="file2">The second file to compare.</param> 
        /// <param name="error">Output error.  Null means no error.</param> 
        /// <returns>bool. The result.  True means the files are the same.</returns> 
        /// Call like this:
        /// CLError error;
        /// bool myResult = CLSptFileCompare.FileCompare("file1.txt", "file2.txt", out error);
        /// if (myResult)
        /// {
        ///     // the files are the same.
        /// }
        public static bool FileCompare(string file1, string file2, out CLError error)
        {
            try
            {
                int file1byte;
                int file2byte;
                FileStream fs1;
                FileStream fs2;

                // Determine if the same file was referenced two times.
                if (file1 == file2)
                {
                    // Return true to indicate that the files are the same.
                    error = null;
                    return true;
                }

                // Open the two files.
                fs1 = new FileStream(file1, FileMode.Open);
                fs2 = new FileStream(file2, FileMode.Open);

                // Check the file sizes. If they are not the same, the files 
                // are not the same.
                if (fs1.Length != fs2.Length)
                {
                    // Close the file
                    fs1.Close();
                    fs2.Close();

                    // Return false to indicate files are different
                    error = null;
                    return false;
                }

                // Read and compare a byte from each file until either a
                // non-matching set of bytes is found or until the end of
                // file1 is reached.
                do
                {
                    // Read one byte from each file.
                    file1byte = fs1.ReadByte();
                    file2byte = fs2.ReadByte();
                }
                while ((file1byte == file2byte) && (file1byte != -1));

                // Close the files.
                fs1.Close();
                fs2.Close();

                // Return the success of the comparison. "file1byte" is 
                // equal to "file2byte" at this point only if the files are 
                // the same.
                error = null;
                return ((file1byte - file2byte) == 0);
            }
            catch (Exception ex)
            {
                error = ex;
                CLTrace.Instance.writeToLog(1, "CLSptFileCompare: FileCompare: ERROR: Exception. Comparing file <{0}> to file <{1}>.", file1, file2);
                return false;
            }
        }
    }
}
