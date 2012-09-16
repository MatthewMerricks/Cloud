//
//  CLShortcuts.cs
//  Cloud Windows
//
//  Created by BobS.
//  Changes Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CloudApiPrivate.Model;
using IWshRuntimeLibrary;
using CloudApiPublic.Support;
using CloudApiPublic.Model;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Common;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

namespace CloudApiPrivate.Common
{
    public static class CLShortcuts
    {
        private static CLTrace _trace = CLTrace.Instance;

        /// <summary>
        /// Add a shortcut to the a target folder.
        /// 
        /// </summary>
        /// <param name="cloudFolderPath">The path to the user's Cloud folder.</param>
        /// <param name="targetFolder">The target folder.  For example, the IE Favorites, or the Explorer Favorites folder.
        /// The IE Favorites folder is accessed by Environment.GetFolderPath(Environment.SpecialFolder.Favorites);
        /// The Explorer Favorites folder is at c:\Users\<user>\Links.  Apparently there is no special folder enumeration for this.</param>
        /// <param name="targetDescription">The description of the shortcut.</param>
        /// <param name="iconExeFilePath">The path to an executable file containing the icon to display in the shortcut.  May be null for the default icon.</param>
        /// <param name="iconOffset">The offset of the icon in the executable file.</param>
        public static void AddCloudFolderShortcutToFolder(string cloudFolderPath, string targetFolder, string targetDescription, string iconExeFilePath = null, int iconOffset = 0)
        {
            try
            {
                // Get the path to the user's Favorites folder.
                _trace.writeToLog(9, "CLShortcuts: AddCloudFolderToExplorerFavorites: Entry.  Target folder: <{0}>.", targetFolder);

                // Remove it first, in case it exists
                RemoveCloudFolderShortcutFromFolder(targetFolder);

                // The code below creates a Folder Shortcut inside the "Favourites" folder. 
                //
                // The following is the general technique:
                //
                // 1. There are 3 entities involved here: 
                //      * The target folder (e.g. "c:\foldername").
                //      * The shortcut file (.lnk file) that points 
                //          to the target folder.
                //      * The "Favourites" folder itself.
                // 
                // 2. You need to create the shortcut file that will point to the target folder (e.g. "c:\foldername").
                //
                // 3. After that, save the shortcut file in the "Favourites" folder.
                // Specify the path to the shortcut (.lnk) file.  We append the shortcut filename to the already discovered "Favourites" path.
                targetFolder += CLPrivateDefinitions.CloudFolderShortcutFilenameExt;

                // Create a WshShell object.
                WshShell wsh = new WshShell();

                // Create a shortcut object and specify the shortcut file path. The shortcut file path must be inside the "Favourites" folder.
                IWshShortcut shortcut = (IWshShortcut)wsh.CreateShortcut(targetFolder.ToString());

                // Specify the target folder to shortcut to.
                shortcut.TargetPath = cloudFolderPath;
                if (iconExeFilePath != null)
                {
                    shortcut.IconLocation = String.Format("{0}, {1}", iconExeFilePath, iconOffset.ToString());
                }
                shortcut.Description = targetDescription;

                // Save the shortcut file.
                shortcut.Save();
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(9, "CLShortcuts: AddCloudFolderToExplorerFavorites: ERROR: Exception: Msg: {0}, Code: {1}.", error.errorDescription, error.errorCode);
            }
        }

        /// <summary>
        /// Remove a possible shortcut to the Cloud folder from the target folder.
        /// </summary>
        /// <param name="targetFolder">The target folder.  For example, the IE Favorites, or the Explorer Favorites folder.
        /// The IE Favorites folder is accessed by Environment.GetFolderPath(Environment.SpecialFolder.Favorites);
        /// The Explorer Favorites folder is at c:\Users\<user>\Links.  Apparently there is no special folder enumeration for this.</param>
        public static void RemoveCloudFolderShortcutFromFolder(string targetFolder)
        {
            try
            {
                // Get the path to the user's Favorites folder.
                _trace.writeToLog(9, "CLShortcuts: RemoveCloudFolderShortcutToFolder: Entry. Target folder: <{0}>.", targetFolder);

                // Specify the path to the shortcut (.lnk) file.  We append the shortcut filename to the already discovered "Favourites" path.
                targetFolder += CLPrivateDefinitions.CloudFolderShortcutFilenameExt;

                // Delete the file
                System.IO.File.Delete(targetFolder);

            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(9, "CLShortcuts: RemoveCloudFolderShortcutToFolder: ERROR: Exception: Msg: {0}, Code: {1}.", error.errorDescription, error.errorCode);
            }
        }

        /// <summary>
        /// Set or reset the icon for the cloud folder.  
        /// </summary>
        /// <param name="cloudFolderPath">The path to the user's Cloud folder.</param>
        /// <param name="iconPath">The path to the icon file.  Set this to null to reset the icon to the default folder icon.</param>
        /// <param name="toolTip">The tooltip to display when hovering over the icon.  Can be String.Empty.</param>
        /// <param name="useCloudIcon"></param>
        public static void SetCloudFolderIcon(string cloudFolderPath, string iconPath, string toolTip = null)
        {
            // The Cloud folder should have the Cloud icon.
            CLFolderIcon folderIcon = new CLFolderIcon(cloudFolderPath);
            folderIcon.CreateFolderIcon(iconPath, toolTip);
        }

        /// <summary>
        /// Write an embedded resource file out to the file system as a real file
        /// </summary>
        /// <param name="assembly">The assembly containing the resource.</param>
        /// <param name="resourceName">The name of the resource.</param>
        /// <param name="targetFileFullPath">The full path of the target file.</param>
        /// <returns>int: 0: success.  Otherwise, error code.</returns>
        public static int WriteResourceFileToFilesystemFile(Assembly storeAssembly, string resourceName, string targetFileFullPath)
        {
            try
            {
                _trace.writeToLog(9, String.Format("CLShortcuts: WriteResourceFileToFilesystemFile: Entry: resource: {0}. targetFileFullPath: {1}.", resourceName, targetFileFullPath));
                _trace.writeToLog(9, String.Format("CLShortcuts: WriteResourceFileToFilesystemFile: storeAssembly.GetName(): <{0}>.", storeAssembly.GetName()));
                _trace.writeToLog(9, String.Format("CLShortcuts: WriteResourceFileToFilesystemFile: storeAssembly.GetName().Name: <{0}>.", storeAssembly.GetName() != null ? storeAssembly.GetName().Name : "ERROR: Not Set!"));
                using (Stream txtStream = storeAssembly.GetManifestResourceStream(storeAssembly.GetName().Name + ".Resources." + resourceName))
                {
                    if (txtStream == null)
                    {
                        _trace.writeToLog(1, "CLShortcuts: WriteResourceFileToFilesystemFile: ERROR: txtStream null.");
                        return 1;
                    }
                    using (FileStream tempStream = new FileStream(targetFileFullPath, FileMode.Create))
                    {
                        if (tempStream == null)
                        {
                            _trace.writeToLog(1, "CLShortcuts: WriteResourceFileToFilesystemFile: ERROR: tempStream null.");
                            return 2;
                        }

                        byte[] streamBuffer = new byte[4096];
                        int readAmount;

                        while ((readAmount = txtStream.Read(streamBuffer, 0, 4096)) > 0)
                        {
                            _trace.writeToLog(9, String.Format("CLShortcuts: WriteResourceFileToFilesystemFile: Write {0} bytes to the .vbs file.", readAmount));
                            tempStream.Write(streamBuffer, 0, readAmount);
                        }
                        _trace.writeToLog(9, "CLShortcuts: WriteResourceFileToFilesystemFile: Finished writing the .vbs file.");
                    }
                }

                // For some reason, Windows is dozing (WinDoze?).  The file we just wrote does not immediately appear in the
                // file system, and the process we will launch next won't find it.  Wait until we can see it in the file system.  ????
                for (int i = 0; i < 10; i++)
                {
                    if (System.IO.File.Exists(targetFileFullPath))
                    {
                        break;
                    }

                    Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: WriteResourceFileToFilesystemFile: ERROR: Exception.  Msg: <{0}>.", ex.Message));
                return 3;
            }

            _trace.writeToLog(1, "CLShortcuts: WriteResourceFileToFilesystemFile: Exit successfully.");
            return 0;
        }

        /// <summary>
        /// Add shortcuts to the Cloud folder to various quick-access locations
        /// </summary>
        /// <param name="cloudFolderPath">The path to the Cloud folder.</param>
        public static void AddCloudFolderShortcuts(string cloudFolderPath)
        {
            try
            {
                _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: Entry. cloudFolderPath: <{0}>.", cloudFolderPath));
                if (!String.IsNullOrWhiteSpace(cloudFolderPath))
                {
                    // Add or update the various Cloud folder shortcuts because the Cloud folder path has just changed.
                    _trace.writeToLog(9, "CLShortcuts: AddCloudFolderShortcuts: Get the location of the Cloud icon.");
                    string cloudIconPath = Assembly.GetEntryAssembly().Location;
                    _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: Cloud icon path: <{0}>.", cloudIconPath));

                    string explorerFavoritesPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Favorites)) + "\\Links";
                    _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: ExplorerFavoritesPath: <{0}>.", explorerFavoritesPath));
                    CLShortcuts.AddCloudFolderShortcutToFolder(cloudFolderPath, Environment.GetFolderPath(Environment.SpecialFolder.Favorites), Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);
                    CLShortcuts.AddCloudFolderShortcutToFolder(cloudFolderPath, Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);
                    CLShortcuts.AddCloudFolderShortcutToFolder(cloudFolderPath, explorerFavoritesPath, Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);
                    // CLShortcuts.AddCloudFolderShortcutToFolder(cloudFolderPath, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);
                
                    // Set the folder Cloud icon.
                    _trace.writeToLog(9, "CLShortcuts: AddCloudFolderShortcuts: Set the folder Cloud icon.");
                    CLShortcuts.SetCloudFolderIcon(Settings.Instance.CloudFolderPath, cloudIconPath, "View the Cloud folder");

                    // Pin the Cloud folder path shortcut to the taskbar.  First, write a VBScript file to handle this task.
                    // Stream the CloudClean.vbs file out to the user's temp directory
                    // Locate the user's temp directory.
                    string userTempDirectory = Path.GetTempPath();
                    string vbsPath = userTempDirectory + "\\PinToTaskbar.vbs";
                    _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: Target location of .vbs file: <{0}>.", vbsPath));

                    // Get the assembly containing the .vbs resource.
                    _trace.writeToLog(9, "CLShortcuts: AddCloudFolderShortcuts: Get the assembly containing the .vbs resource.");
                    System.Reflection.Assembly storeAssembly = System.Reflection.Assembly.GetAssembly(typeof(CloudApiPrivate.Common.CLShortcuts));
                    if (storeAssembly == null)
                    {
                        _trace.writeToLog(1, "CLShortcuts: AddCloudFolderShortcuts: ERROR: storeAssembly null");
                        return;
                    }

                    // Stream the CloudClean.vbs file out to the temp directory
                    _trace.writeToLog(9, "CLShortcuts: AddCloudFolderShortcuts: Call WriteResourceFileToFilesystemFile.");
                    int rc = WriteResourceFileToFilesystemFile(storeAssembly, "PinToTaskbar", vbsPath);
                    if (rc != 0)
                    {
                        _trace.writeToLog(1, "CLShortcuts: AddCloudFolderShortcuts: ERROR: From WriteResourceFileToFilesystemFile.");
                        return;
                    }
                
                    // Now create a new process to run the VBScript file.
                    _trace.writeToLog(9, "CLShortcuts: AddCloudFolderShortcuts: Build the paths for launching the VBScript file.");
                    string systemFolderPath = GetSystemFolderPathForBitness();
                    string cscriptPath = systemFolderPath + "\\cscript.exe";
                    _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: Cscript executable path: <{0}>.", cscriptPath));

                    string parm1Path = GetProgramFilesFolderPathForBitness();
                    _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: Parm 1: <{0}>.", parm1Path));

                    string parm2Path = Environment.GetEnvironmentVariable("SystemRoot");
                    _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: Parm 2: <{0}>.", parm2Path));

                    string argumentsString = @" //B //T:30 //Nologo """ + vbsPath + @"""" + @" """ + parm1Path + @""" """ + parm2Path + @"""";
                    _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: Launch the VBScript file.  Launch: <{0}>.", argumentsString));
            
                    // Launch the process
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = false;
                    startInfo.FileName = cscriptPath;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.Arguments = argumentsString;
                    Process.Start(startInfo);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Remove the shortcuts to the Cloud folder from various quick-access locations.
        /// </summary>
        /// <param name="cloudFolderPath"></param>
        public static void RemoveCloudFolderShortcuts(string cloudFolderPath)
        {
            // Remove all of the Cloud folder shortcuts.
            if (!String.IsNullOrWhiteSpace(cloudFolderPath) && Directory.Exists(cloudFolderPath))
            {
                // Reset the Cloud folder Cloud icon.
                CLShortcuts.SetCloudFolderIcon(cloudFolderPath, null);
            }

            // Remove the shortcuts
            string explorerFavoritesPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Favorites)) + "\\Links";
            CLShortcuts.RemoveCloudFolderShortcutFromFolder(Environment.GetFolderPath(Environment.SpecialFolder.Favorites));
            CLShortcuts.RemoveCloudFolderShortcutFromFolder(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            CLShortcuts.RemoveCloudFolderShortcutFromFolder(explorerFavoritesPath);
        }

        public static string GetProgramFilesFolderPathForBitness()
        {
            // Determine whether 32-bit or 64-bit architecture
            if (IntPtr.Size == 4)
            {
                // 32-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }
            else
            {
                // 64-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            }
        }

        public static string GetSystemFolderPathForBitness()
        {
            // Determine whether 32-bit or 64-bit architecture
            if (IntPtr.Size == 4)
            {
                // 32-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.System);
            }
            else
            {
                // 64-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            }
        }
    }
}
