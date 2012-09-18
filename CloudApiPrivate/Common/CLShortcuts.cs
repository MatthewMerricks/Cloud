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
using Microsoft.Win32;
using System.Windows;

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
                targetFolder += "\\" + CLPrivateDefinitions.CloudFolderShortcutFilename + ".lnk";

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
                targetFolder += "\\" + CLPrivateDefinitions.CloudFolderShortcutFilename + ".lnk";

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

        private static readonly Encoding UTF8WithoutBOM = new UTF8Encoding(false);

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

                    using (TextReader txtReader = new StreamReader(txtStream,
                        Encoding.Unicode,
                        true,
                        4096))
                    {
                        if (txtReader == null)
                        {
                            _trace.writeToLog(1, "CLShortcuts: WriteResourceFileToFilesystemFile: ERROR: txtReader null.");
                            return 2;
                        }

                        using (StreamWriter tempStream = new StreamWriter(targetFileFullPath, false, UTF8WithoutBOM, 4096))
                        {
                            if (tempStream == null)
                            {
                                _trace.writeToLog(1, "CLShortcuts: WriteResourceFileToFilesystemFile: ERROR: tempStream null.");
                                return 3;
                            }

                            char[] streamBuffer = new char[4096];
                            int readAmount;

                            while ((readAmount = txtReader.ReadBlock(streamBuffer, 0, 4096)) > 0)
                            {
                                _trace.writeToLog(9, String.Format("CLShortcuts: WriteResourceFileToFilesystemFile: Write {0} bytes to the .vbs file.", readAmount));
                                tempStream.Write(streamBuffer, 0, readAmount);
                            }

                            _trace.writeToLog(9, "CLShortcuts: WriteResourceFileToFilesystemFile: Finished writing the .vbs file.");
                        }
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
                return 4;
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
                    // Add the various Cloud folder shortcuts.
                    _trace.writeToLog(9, "CLShortcuts: AddCloudFolderShortcuts: Get the location of the Cloud icon.");
                    string cloudIconPath = Assembly.GetEntryAssembly().Location;
                    _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: Cloud icon path: <{0}>.", cloudIconPath));

                    // Add a folder shortcut to the Explorer Favorites list.
                    string explorerFavoritesPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Favorites)) + "\\Links";
                    _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: ExplorerFavoritesPath: <{0}>.", explorerFavoritesPath));
                    CLShortcuts.AddCloudFolderShortcutToFolder(cloudFolderPath, explorerFavoritesPath, Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);

                    // Add a folder shortcut to the Internet Explorer favorites list
                    CLShortcuts.AddCloudFolderShortcutToFolder(cloudFolderPath, Environment.GetFolderPath(Environment.SpecialFolder.Favorites), Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);

                    // Add a folder shortcut to the desktop
                    CLShortcuts.AddCloudFolderShortcutToFolder(cloudFolderPath, Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);

                    // Set the custom folder Cloud icon.  That will show anywhere the cloud folder appears.
                    _trace.writeToLog(9, "CLShortcuts: AddCloudFolderShortcuts: Set the folder Cloud icon.");
                    CLShortcuts.SetCloudFolderIcon(Settings.Instance.CloudFolderPath, cloudIconPath, "View the Cloud folder");

                    // Pin the Cloud folder path shortcut to the taskbar.  First, write a VBScript file to handle this task.
                    // Stream the CloudClean.vbs file out to the user's temp directory
                    // Locate the user's temp directory.
                    PinShowCloudFolderToTaskbar(shouldProcessTaskbar: true, shouldPin: true, scriptShouldSelfDestruct: false, shouldWaitForCompletion: true);
                    PinShowCloudFolderToTaskbar(shouldProcessTaskbar: false, shouldPin: true, scriptShouldSelfDestruct: true, shouldWaitForCompletion: false);

                    // Add an autostart shortcut to the startup folder:
                    AddCloudAutostartShortcut();
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
            _trace.writeToLog(9, "CLShortcuts: AddCloudFolderShortcuts: Exit.");
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

            // Remove the shortcuts.  Remove the Windows Explorer Favorites list item
            string explorerFavoritesPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Favorites)) + "\\Links";
            CLShortcuts.RemoveCloudFolderShortcutFromFolder(explorerFavoritesPath);

            // Remove the Internet Explorer favorites list item.
            CLShortcuts.RemoveCloudFolderShortcutFromFolder(Environment.GetFolderPath(Environment.SpecialFolder.Favorites));

            // Remove the Desktop item
            CLShortcuts.RemoveCloudFolderShortcutFromFolder(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        }


        /// <summary>
        /// Autostart Cloud.exe
        /// </summary>
        private static void AddCloudAutostartShortcut()
        {
            _trace.writeToLog(9, "CLShortcuts: AddCloudAutostartShortcut: Entry.");
            try
            {
                RegistryKey rkApp = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);   // Run for all users at startup
                rkApp.SetValue(CLPrivateDefinitions.CloudAppName, GetProgramFilesFolderPathForBitness() + CLPrivateDefinitions.CloudFolderInProgramFiles + "\\" + CLPrivateDefinitions.CloudAppName + ".exe");
            }
            catch (Exception ex)
            {
                _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudAutostartShortcut: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Remove Autostart Cloud.exe
        /// </summary>
        public static void RemoveCloudAutostartShortcut()
        {
            _trace.writeToLog(9, "CLShortcuts: RemoveCloudAutostartShortcut: Entry.");
            try
            {
                RegistryKey rkApp = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);   // Run for all users at startup
                rkApp.DeleteValue(CLPrivateDefinitions.CloudAppName, false);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(9, String.Format("CLShortcuts: RemoveCloudAutostartShortcut: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Update all of the shortcuts to match the current settings.
        /// </summary>
        public static void UpdateAllShortcuts()
        {
            UpdateShouldStartCloudAppWithSystem(Settings.Instance.StartCloudAppWithSystem);
            UpdateShouldShowCloudFolderOnDesktop(Settings.Instance.ShouldAddShowCloudFolderOnDesktop);
            UpdateShouldShowCloudFolderInExplorerFavorites(Settings.Instance.ShouldAddShowCloudFolderInExplorerFavorites);
            UpdateShouldShowCloudFolderInInternetExplorerFavorites(Settings.Instance.ShouldAddShowCloudFolderInInternetExplorerFavorites);
            UpdateShouldShowCloudFolderOnTaskbar(Settings.Instance.ShouldAddShowCloudFolderOnTaskbar);
            UpdateShouldShowCloudFolderInStartMenu(Settings.Instance.ShouldAddShowCloudFolderInStartMenu);
            UpdateShouldUseCloudIconForCloudFolder(Settings.Instance.UseColorIconForCloudFolder);
        }

        /// <summary>
        /// Update the Cloud.exe autostart start-up setting.
        /// </summary>
        /// <param name="shouldBeActive"></param>
        public static void UpdateShouldStartCloudAppWithSystem(bool shouldBeActive)
        {
            if (shouldBeActive)
            {
                AddCloudAutostartShortcut();
            }
            else
            {
                RemoveCloudAutostartShortcut();
            }
        }

        /// <summary>
        /// Update the status of the Desktop cloud folder shortcut.
        /// </summary>
        /// <param name="shouldBeActive"></param>
        public static void UpdateShouldShowCloudFolderOnDesktop(bool shouldBeActive)
        {
            if (shouldBeActive)
            {
                // Get the cloud icon path.
                _trace.writeToLog(9, "CLShortcuts: AddCloudFolderShortcuts: Get the location of the Cloud icon.");
                string cloudIconPath = Assembly.GetEntryAssembly().Location;

                // Add a folder shortcut to the desktop
                CLShortcuts.AddCloudFolderShortcutToFolder(Settings.Instance.CloudFolderPath, Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);
            }
            else
            {
                // Remove the Desktop item
                CLShortcuts.RemoveCloudFolderShortcutFromFolder(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            }
        }

        /// <summary>
        /// Update the status of the Explorer favorites cloud folder shortcut.
        /// </summary>
        /// <param name="shouldBeActive"></param>
        public static void UpdateShouldShowCloudFolderInExplorerFavorites(bool shouldBeActive)
        {
            if (shouldBeActive)
            {
                // Get the cloud icon path.
                _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderInExplorerFavorites: Get the location of the Cloud icon.");
                string cloudIconPath = Assembly.GetEntryAssembly().Location;

                // Add a folder shortcut to the Explorer Favorites list.
                string explorerFavoritesPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Favorites)) + "\\Links";
                _trace.writeToLog(9, String.Format("CLShortcuts: UpdateShouldShowCloudFolderInExplorerFavorites: ExplorerFavoritesPath: <{0}>.", explorerFavoritesPath));
                CLShortcuts.AddCloudFolderShortcutToFolder(Settings.Instance.CloudFolderPath, explorerFavoritesPath, Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);
            }
            else
            {
                // Remove the shortcuts.  Remove the Windows Explorer Favorites list item
                string explorerFavoritesPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Favorites)) + "\\Links";
                CLShortcuts.RemoveCloudFolderShortcutFromFolder(explorerFavoritesPath);
            }
        }
        /// <summary>
        /// Update the status of the Internet Explorer favorites cloud folder shortcut.
        /// </summary>
        /// <param name="shouldBeActive"></param>
        public static void UpdateShouldShowCloudFolderInInternetExplorerFavorites(bool shouldBeActive)
        {
            if (shouldBeActive)
            {
                // Get the cloud icon path.
                _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderInInternetExplorerFavorites: Get the location of the Cloud icon.");
                string cloudIconPath = Assembly.GetEntryAssembly().Location;

                // Add a folder shortcut to the Internet Explorer favorites list
                CLShortcuts.AddCloudFolderShortcutToFolder(Settings.Instance.CloudFolderPath, Environment.GetFolderPath(Environment.SpecialFolder.Favorites), Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);
            }
            else
            {
                // Remove the Internet Explorer favorites list item.
                CLShortcuts.RemoveCloudFolderShortcutFromFolder(Environment.GetFolderPath(Environment.SpecialFolder.Favorites));
            }
        }

        /// <summary>
        /// Update the status of the taskbar cloud folder shortcut.
        /// </summary>
        /// <param name="shouldBeActive"></param>
        public static void UpdateShouldShowCloudFolderOnTaskbar(bool shouldBeActive)
        {
            if (shouldBeActive)
            {
                PinShowCloudFolderToTaskbar(shouldProcessTaskbar: true, shouldPin: true, scriptShouldSelfDestruct: true, shouldWaitForCompletion: false);
            }
            else
            {
                PinShowCloudFolderToTaskbar(shouldProcessTaskbar: true, shouldPin: false, scriptShouldSelfDestruct: true, shouldWaitForCompletion: false);
            }
        }

        /// <summary>
        /// Update the status of the Start menu cloud folder shortcut.
        /// </summary>
        /// <param name="shouldBeActive"></param>
        public static void UpdateShouldShowCloudFolderInStartMenu(bool shouldBeActive)
        {
            if (shouldBeActive)
            {
                PinShowCloudFolderToTaskbar(shouldProcessTaskbar: false, shouldPin: true, scriptShouldSelfDestruct: true, shouldWaitForCompletion: false);
            }
            else
            {
                PinShowCloudFolderToTaskbar(shouldProcessTaskbar: false, shouldPin: false, scriptShouldSelfDestruct: true, shouldWaitForCompletion: false);
            }
        }

        /// <summary>
        /// Update the status of the user choice to use the cloud icon as a custom icon for the cloud folder.
        /// </summary>
        /// <param name="shouldBeActive"></param>
        public static void UpdateShouldUseCloudIconForCloudFolder(bool shouldBeActive)
        {
            string cloudFolderPath = Settings.Instance.CloudFolderPath;
            if (shouldBeActive)
            {
                if (!String.IsNullOrWhiteSpace(cloudFolderPath) && Directory.Exists(cloudFolderPath))
                {
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldUseCloudIconForCloudFolder: Get the location of the Cloud icon.");
                    string cloudIconPath = Assembly.GetEntryAssembly().Location;
                    _trace.writeToLog(9, String.Format("CLShortcuts: UpdateShouldUseCloudIconForCloudFolder: Cloud icon path: <{0}>.", cloudIconPath));

                    // Set the custom folder Cloud icon.  That will show anywhere the cloud folder appears.
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldUseCloudIconForCloudFolder: Set the folder Cloud icon.");
                    CLShortcuts.SetCloudFolderIcon(cloudFolderPath, cloudIconPath, "View the Cloud folder");

                }
            }
            else
            {
                // Remove all of the Cloud folder shortcuts.
                if (!String.IsNullOrWhiteSpace(cloudFolderPath) && Directory.Exists(cloudFolderPath))
                {
                    // Reset the Cloud folder Cloud icon.
                    CLShortcuts.SetCloudFolderIcon(cloudFolderPath, null);
                }
            }
        }

        /// <summary>
        /// Replace all of the cloud folder shortcuts with a new cloud folder location.
        /// </summary>
        /// <param name="newCloudFolderPath"></param>
        public static void ModifyCloudFolderShortcuts(string newCloudFolderPath)
        {
            try
            {
                _trace.writeToLog(9, "CLShortcuts: ModifyCloudFolderShortcuts: Entry: newCloudFolderPath: <{0}>.", newCloudFolderPath);

                // Modify the Explorer Favorites shortcut.
                string explorerFavoritesPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Favorites)) + "\\Links";
                _trace.writeToLog(9, String.Format("CLShortcuts: ModifyCloudFolderShortcuts: ExplorerFavoritesPath: <{0}>.", explorerFavoritesPath));
                ModifyShortcutTargetPath(explorerFavoritesPath, CLPrivateDefinitions.CloudFolderShortcutFilename, newCloudFolderPath);

                // Modify the cloud folder shortcut in the Internet Explorer favorites list
                ModifyShortcutTargetPath(Environment.GetFolderPath(Environment.SpecialFolder.Favorites), CLPrivateDefinitions.CloudFolderShortcutFilename, newCloudFolderPath);

                // Modify the cloud folder shortcut in the Desktop.
                ModifyShortcutTargetPath(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), CLPrivateDefinitions.CloudFolderShortcutFilename, newCloudFolderPath);

            }
            catch (Exception ex)
            {
                _trace.writeToLog(9, String.Format("CLShortcuts: ModifyCloudFolderShortcuts: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Change the target path in a shortcut file.
        /// </summary>
        /// <param name="pathFolderContainingShortcut">Full path of the folder that contains the shortcut .lnk file (without trailing backslash).</param>
        /// <param name="shortcutNameWithoutLnkExtension"></param>
        /// <param name="newTargetLocation">The full path of the new target item.</param>
        private static void ModifyShortcutTargetPath(string pathFolderContainingShortcut, string shortcutNameWithoutLnkExtension, string newTargetLocation)
        {
            try 
	        {
                _trace.writeToLog(9, "CLShortcuts: ModifyShortcutTargetPath: Entry: pathFolderContainingShortcut: <{0}>. shortcutNameWithoutLnkExtension: <{1}>. newTargetLocation: <{2>.", pathFolderContainingShortcut, shortcutNameWithoutLnkExtension, newTargetLocation);
                Shell32.Shell shl = new Shell32.Shell();
                Shell32.Folder dir = shl.NameSpace(pathFolderContainingShortcut);
                Shell32.FolderItem itm = dir.Items().Item(shortcutNameWithoutLnkExtension + ".lnk");
                Shell32.ShellLinkObject lnk = (Shell32.ShellLinkObject)itm.GetLink;
                lnk.SetIconLocation(newTargetLocation, 1);
                lnk.Save(null);
	        }
	        catch (Exception ex)
	        {
                _trace.writeToLog(9, String.Format("CLShortcuts: ModifyShortcutTargetPath: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }
        
        /// <summary>
        /// Pin a shortcut to the ShowCloudFolder.exe program in the Cloud Program Files folder to the taskbar and to the start menu.
        /// </summary>
        /// <param name="isTaskbar">true: Pin to the taskbar.</param>
        /// <param name="shouldPin">true: Pin.  false: Unpin.</param>
        private static void PinShowCloudFolderToTaskbar(bool shouldProcessTaskbar, bool shouldPin, bool scriptShouldSelfDestruct, bool shouldWaitForCompletion)
        {
            try
            {
                string userTempDirectory = Path.GetTempPath();
                string vbsPath = userTempDirectory + "PinToTaskbar.vbs";
                _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderShortcuts: Target location of .vbs file: <{0}>.", vbsPath));

                // Get the assembly containing the .vbs resource.
                _trace.writeToLog(9, "CLShortcuts: PinShowCloudFolderToTaskbar: Get the assembly containing the .vbs resource.");
                System.Reflection.Assembly storeAssembly = System.Reflection.Assembly.GetAssembly(typeof(CloudApiPrivate.Common.CLShortcuts));
                if (storeAssembly == null)
                {
                    _trace.writeToLog(1, "CLShortcuts: PinShowCloudFolderToTaskbar: ERROR: storeAssembly null");
                    return;
                }

                // Stream the PinToTaskbar.vbs file out to the temp directory
                _trace.writeToLog(9, "CLShortcuts: AddCloudFolderShortcuts: Call WriteResourceFileToFilesystemFile.");
                int rc = WriteResourceFileToFilesystemFile(storeAssembly, "PinToTaskbar", vbsPath);
                if (rc != 0)
                {
                    _trace.writeToLog(1, "CLShortcuts: PinShowCloudFolderToTaskbar: ERROR: From WriteResourceFileToFilesystemFile.");
                    return;
                }

                // Now create a new process to run the VBScript file.
                _trace.writeToLog(9, "CLShortcuts: PinShowCloudFolderToTaskbar: Build the paths for launching the VBScript file.");
                string systemFolderPath = GetSystemFolderPathForBitness();
                string cscriptPath = systemFolderPath + "\\cscript.exe";
                _trace.writeToLog(9, String.Format("CLShortcuts: PinShowCloudFolderToTaskbar: Cscript executable path: <{0}>.", cscriptPath));

                // Parm 1 should be the full path of the Program Files Cloud installation directory.
                string parm1Path = GetProgramFilesFolderPathForBitness() + CLPrivateDefinitions.CloudFolderInProgramFiles;
                _trace.writeToLog(9, String.Format("CLShortcuts: PinShowCloudFolderToTaskbar: Parm 1: <{0}>.", parm1Path));

                // Parm 2 should be the filename of the .exe or .lnk file that will be pinned to the taskbar (without the extension)
                string parm2Path = CLPrivateDefinitions.ShowCloudFolderProgramFilenameOnly;
                _trace.writeToLog(9, String.Format("CLShortcuts: PinShowCloudFolderToTaskbar: Parm 2: <{0}>.", parm2Path));

                // Parm 3 should be the action ("P": Pin.  "U": Unpin)
                string parm3 = shouldPin ? "P" : "U";
                _trace.writeToLog(9, String.Format("CLShortcuts: PinShowCloudFolderToTaskbar: Parm 3: <{0}>.", parm3));

                // Parm 4 should be the target ("T": Taskbar.  "S": Start menu)
                string parm4 = shouldProcessTaskbar ? "T" : "S";
                _trace.writeToLog(9, String.Format("CLShortcuts: PinShowCloudFolderToTaskbar: Parm 4: <{0}>.", parm4));

                // Parm 5 should be whether the script self-destructs ("D": Self-destruct.  "R": Remain.  Don't delete. menu)
                string parm5 = scriptShouldSelfDestruct ? "D" : "R";
                _trace.writeToLog(9, String.Format("CLShortcuts: PinShowCloudFolderToTaskbar: Parm 5: <{0}>.", parm5));

                string argumentsString = @" //B //T:30 //Nologo """ + vbsPath + @""" """ + parm1Path + @""" """ + parm2Path + @""" " + parm3 + " " + parm4 + " " + parm5;
                _trace.writeToLog(9, String.Format("CLShortcuts: PinShowCloudFolderToTaskbar: Launch the VBScript file.  Launch: <{0}>.", argumentsString));

                // Launch the process to pin to the taskbar.
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = cscriptPath;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = argumentsString;
                Process p = Process.Start(startInfo);

                // Wait if we should
                if (shouldWaitForCompletion)
                {
                    p.WaitForExit(3000);            // wait for this action to complete
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: PinShowCloudFolderToTaskbar: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
            _trace.writeToLog(9, "CLShortcuts: AddCloudFolderShortcuts: Exit.");
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
