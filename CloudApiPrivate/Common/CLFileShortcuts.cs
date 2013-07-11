//
//  CLFileShortcuts.cs
//  Cloud Windows
//
//  Created by BobS on 7/2/12.
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interop.Shell32;
using CloudApiPrivate.Model;

namespace CloudApiPrivate.Common
{
    public class CLFileShortcuts
    {
        /// <summary>
        /// Return the target path represented by a shortcut.
        /// </summary>
        /// <param name="shortcutFilename">File to check</param>
        /// <returns>(string) Returns target path, or string.Empty.</returns>
        public static string GetShortcutTargetFile(string shortcutFilename)
        {
            string pathOnly = System.IO.Path.GetDirectoryName(shortcutFilename);
            string filenameOnly = System.IO.Path.GetFileName(shortcutFilename);

            Shell shell = new Shell();
            Folder folder = shell.NameSpace(pathOnly);
            FolderItem folderItem = folder.ParseName(filenameOnly);
            if (folderItem != null)
            {
                Interop.Shell32.ShellLinkObject link = (Interop.Shell32.ShellLinkObject)folderItem.GetLink;
                return link.Path;
            }

            return string.Empty;
        }

        /// <summary>
        /// Determine if a given file is a shortcut (used to ignore file system events on shortcuts)
        /// </summary>
        /// <param name="path">File to check</param>
        /// <returns>Returns true if file is shortcut, otherwise false</returns>
        public static bool FileIsShortcut(string path)
        {
            // A shortcut is a file
            if (Directory.Exists(path))
            {
                return false;
            }

            // set object for gathering file info at current path
            FileInfo toCheck = new FileInfo(path);

            // check and store if file exists
            bool exists = toCheck.Exists;

            // ran into a condition where the file was moved between checking if it existed and finding its length,
            // fixed by storing the length inside a try/catch and handling the not found exception by flipping exists
            if (exists)
            {
                try
                {
                    Nullable<long> fileLength = toCheck.Length;
                }
                catch (FileNotFoundException)
                {
                    exists = false;
                }
                catch (DirectoryNotFoundException) // even though we're checking a file's length, it can give a directory exception if the parent is not there
                {
                    exists = false;
                }
                catch
                {
                }
            }
            if (!exists)
            {
                return false;
            }

            // if there is an issue establishing the Win32 shell, just assume that the ".lnk" extension means it was a shortcut;
            // so the following boolean will be set true once the file extension is found to be ".lnk" and set back to false once
            // the shell object has been instantiated (if it throws an exception while it is still true, then we cannot verify the
            // shortcut using Shell32 and have to assume it is a valid shortcut)
            bool shellCodeFailed = false;
            try
            {
                // shortcuts must have shortcut extension for OS to treat it like a shortcut
                if (toCheck.Extension.TrimStart('.').Equals(CLPrivateDefinitions.ShortcutExtension, StringComparison.InvariantCultureIgnoreCase))
                {
                    // set boolean so if Shell32 fails to retrive, assume the ".lnk" is sufficient to ensure file is a shortcut
                    shellCodeFailed = true;

                    // Get Shell interface inside try/catch cause an error means we cannot use Shell32.dll for determining shortcut status
                    // (presumes .lnk will be a shortcut in that case)

                    // Shell interface needed to verify shortcut validity
                    Interop.Shell32.Shell shell32 = new Interop.Shell32.Shell();
                    if (shell32 == null)
                    {
                        throw new Exception("System does not support Shell32, file will be assumed to be a valid shortcut");
                    }

                    // set boolean back to false since Shell32 was successfully retrieved,
                    // so it if fails after this point then the file is not a valid shortcut
                    shellCodeFailed = false;

                    // The following code will either succeed and process the boolean for a readable shortcut, or it will fail (not a valid shortcut)
                    var lnkDirectory = shell32.NameSpace(toCheck.DirectoryName);
                    var lnkItem = lnkDirectory.Items().Item(toCheck.Name);
                    var lnk = (Interop.Shell32.ShellLinkObject)lnkItem.GetLink;
                    return !string.IsNullOrEmpty(lnk.Target.Path);
                }
            }
            catch
            {
                // returns true if file is a ".lnk" and either Shell32 failed to retrieve or Shell32 determined shortcut to be valid,
                // otherwise returns false
                return shellCodeFailed;
            }
            // not a ".lnk" shortcut filetype
            return false;
        }
    }
}
