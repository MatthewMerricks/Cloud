//
//  CLIniWriter.cs
//  Cloud Windows
//
//  Created by BobS.
//  Changes Copyright (c) Cloud.com. All rights reserved.

/*-----------------------------------------------------------------------------
File:           IniWriter.cs
Copyright:      (c) 2005, Evan Stone, All Rights Reserved
Author:         Evan Stone
Description:    Simple class to write a value to a .ini file.
Version:        1.0
Date:           January 17, 2005
Comments: 
EULA:           THIS SOURCE CODE MAY NOT BE DISTRIBUTED IN ANY FASHION WITHOUT
                THE PRIOR CONSENT OF THE AUTHOR. THIS SOURCE CODE IS LICENSED 
                “AS IS” WITHOUT WARRANTY AS TO ITS PERFORMANCE AND THE 
                COPYRIGHT HOLDER MAKES NO WARRANTIES OF ANY KIND, EXPRESSED 
                OR IMPLIED, INCLUDING BUT NOT LIMITED TO IMPLIED WARRANTIES 
                OF MERCHANTABILITY OR FITNESS FOR A PARTICULAR PURPOSE. 
                IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, 
                INDIRECT, INCIDENTAL, SPECIAL, PUNITIVE OR CONSEQUENTIAL 
                DAMAGES OR LOST PROFITS, EVEN IF THE END USER HAS OR HAS NOT 
                BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
From:           http://www.codeproject.com/Articles/9331/Create-Icons-for-Folders-in-Windows-Explorer-Using
-----------------------------------------------------------------------------*/
using System;
using System.Runtime.InteropServices;

namespace CloudApiPrivate.Common
{
	/// <summary>
	/// Wrapper class for WritePrivateProfileString Win32 API function.
	/// </summary>
	public class CLIniWriter
	{
        // For convenience's sake, I'm using the WritePrivateProfileString
        // Win32 API function here. Feel free to write your own .ini file
        // writing function if you wish.
        [DllImport("kernel32")] 
        private static extern int WritePrivateProfileString(
                string iniSection, 
                string iniKey, 
                string iniValue, 
                string iniFilePath);		
        
        /// <summary>
        /// Adds to (or modifies) a value to an .ini file. If the file does not exist,
        /// it will be created.
        /// </summary>
        /// <param name="iniSection">The section to which to add or modify a value.If the section does not exist,
        /// it will be created.</param>
        /// <param name="iniKey">The key to which to add or modify a value.If the key does not exist,
        /// it will be created.</param>
        /// <param name="iniValue">The value to write to the .ini file</param>
        /// <param name="iniFilePath">The path to the .ini file to modify.</param>
        /// <returns></returns>
        public static void WriteValue(string iniSection, 
                                     string iniKey, 
                                     string iniValue,
                                     string iniFilePath)
        {
            WritePrivateProfileString(iniSection, iniKey, iniValue, iniFilePath);
        }
        
	}
}
