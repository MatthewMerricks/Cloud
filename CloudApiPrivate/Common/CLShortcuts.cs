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
                _trace.writeToLog(9, String.Format("CLShortcuts: AddCloudFolderToExplorerFavorites: Entry.  Target folder: <{0}>.", targetFolder));

                // Just exit if the cloud path is not valid
                if (String.IsNullOrWhiteSpace(cloudFolderPath))
                {
                    _trace.writeToLog(9, "CLShortcuts: AddCloudFolderToExplorerFavorites: ERROR: Cloud folder path null.");
                }

                // If the .lnk file exists, modify it with the new target path.  Otherwise, create it.
                string targetFile = targetFolder + "\\" + CLPrivateDefinitions.CloudFolderShortcutFilename + ".lnk";
                if (System.IO.File.Exists(targetFile))
                {
                    // The file already exists.  Modify it.
                    ModifyShortcutTargetPath(targetFolder, CLPrivateDefinitions.CloudFolderShortcutFilename, cloudFolderPath, iconExeFilePath);
                }
                else
                {
                    // The file doesn't exist.  Create it.
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

                    // Create a WshShell object.
                    WshShell wsh = new WshShell();

                    // Create a shortcut object and specify the shortcut file path. The shortcut file path must be inside the "Favourites" folder.
                    IWshShortcut shortcut = (IWshShortcut)wsh.CreateShortcut(targetFile);

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
            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "CLShortcuts: AddCloudFolderToExplorerFavorites: ERROR: Exception: Msg: {0}, Code: {1}.", error.errorDescription, error.errorCode);
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
                string targetFile = targetFolder + "\\" + CLPrivateDefinitions.CloudFolderShortcutFilename + ".lnk";

                // Delete the file
                System.IO.File.Delete(targetFile);

            }
            catch (Exception ex)
            {
                CLError error = ex;
                _trace.writeToLog(1, "CLShortcuts: RemoveCloudFolderShortcutToFolder: ERROR: Exception: Msg: {0}, Code: {1}.", error.errorDescription, error.errorCode);
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
            try
            {
                // The Cloud folder should have the Cloud icon.
                _trace.writeToLog(9, "CLShortcuts: SetCloudFolderIcon: Entry.");
                if (!String.IsNullOrWhiteSpace(cloudFolderPath) && Directory.Exists(cloudFolderPath))
                {
                    _trace.writeToLog(9, "CLShortcuts: SetCloudFolderIcon: Create or remove the custom icon.");
                    CLFolderIcon folderIcon = new CLFolderIcon(cloudFolderPath);
                    folderIcon.CreateFolderIcon(iconPath, toolTip);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CLShortcuts: SetCloudFolderIcon: ERROR: Exception: Msg: {0}.", ex.Message);
            }
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
        /// Remove the shortcuts to the Cloud folder from various quick-access locations.
        /// </summary>
        /// <param name="cloudFolderPath">The full path of the cloud folder.</param>
        public static void RemoveCloudFolderShortcuts(string cloudFolderPath)
        {
            try
            {
                // Remove all of the Cloud folder shortcuts.
                _trace.writeToLog(9, "CLShortcuts: RemoveCloudFolderShortcuts: Entry.");
                UpdateShouldStartCloudAppWithSystem(false, cloudFolderPath);
                UpdateShouldShowCloudFolderOnDesktop(false, cloudFolderPath);
                UpdateShouldShowCloudFolderInExplorerFavorites(false, cloudFolderPath);
                UpdateShouldShowCloudFolderInInternetExplorerFavorites(false, cloudFolderPath);
                UpdateShouldShowCloudFolderOnTaskbar(false, cloudFolderPath, shouldWaitForCompletion: true);
                UpdateShouldShowCloudFolderInStartMenu(false, cloudFolderPath, shouldWaitForCompletion: false);
                UpdateShouldUseCloudIconForCloudFolder(false, cloudFolderPath);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: RemoveCloudFolderShortcuts: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
            _trace.writeToLog(9, "CLShortcuts: RemoveCloudFolderShortcuts: Exit.");
        }


        /// <summary>
        /// Autostart Cloud.exe
        /// </summary>
        private static void AddCloudAutostartShortcut()
        {
            try
            {
                _trace.writeToLog(9, "CLShortcuts: AddCloudAutostartShortcut: Entry.");
                RegistryKey rkApp = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);   // Run for all users at startup
                rkApp.SetValue(CLPrivateDefinitions.CloudAppName, GetProgramFilesFolderPathForBitness() + CLPrivateDefinitions.CloudFolderInProgramFiles + "\\" + CLPrivateDefinitions.CloudAppName + ".exe");
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: AddCloudAutostartShortcut: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Remove Autostart Cloud.exe
        /// </summary>
        public static void RemoveCloudAutostartShortcut()
        {
            try
            {
                _trace.writeToLog(9, "CLShortcuts: RemoveCloudAutostartShortcut: Entry.");
                RegistryKey rkApp = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);   // Run for all users at startup
                rkApp.DeleteValue(CLPrivateDefinitions.CloudAppName, false);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: RemoveCloudAutostartShortcut: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Update all of the shortcuts to match the current settings.
        /// </summary>
        /// <param name="cloudFolderPath">The full path to the user's cloud folder.</param>
        public static void UpdateAllShortcuts(string cloudFolderPath)
        {
            try
            {
                _trace.writeToLog(9, "CLShortcuts: UpdateAllShortcuts: Entry.");
                UpdateShouldStartCloudAppWithSystem(Settings.Instance.StartCloudAppWithSystem, cloudFolderPath);
                UpdateShouldShowCloudFolderOnDesktop(Settings.Instance.ShouldAddShowCloudFolderOnDesktop, cloudFolderPath);
                UpdateShouldShowCloudFolderInExplorerFavorites(Settings.Instance.ShouldAddShowCloudFolderInExplorerFavorites, cloudFolderPath);
                UpdateShouldShowCloudFolderInInternetExplorerFavorites(Settings.Instance.ShouldAddShowCloudFolderInInternetExplorerFavorites, cloudFolderPath);
                UpdateShouldShowCloudFolderOnTaskbar(Settings.Instance.ShouldAddShowCloudFolderOnTaskbar, cloudFolderPath, shouldWaitForCompletion: true);
                UpdateShouldShowCloudFolderInStartMenu(Settings.Instance.ShouldAddShowCloudFolderInStartMenu, cloudFolderPath, shouldWaitForCompletion: false);
                UpdateShouldUseCloudIconForCloudFolder(Settings.Instance.UseColorIconForCloudFolder, cloudFolderPath);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: UpdateAllShortcuts: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Update the Cloud.exe autostart start-up setting.
        /// </summary>
        /// <param name="shouldBeActive">true: Cloud should be auto-started.</param>
        /// <param name="cloudFolderPath">The full path to the user's cloud folder.</param>
        public static void UpdateShouldStartCloudAppWithSystem(bool shouldBeActive, string cloudFolderPath)
        {
            try
            {
                _trace.writeToLog(9, "CLShortcuts: UpdateShouldStartCloudAppWithSystem: Entry.");
                if (shouldBeActive)
                {
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldStartCloudAppWithSystem: Add autostart.");
                    AddCloudAutostartShortcut();
                }
                else
                {
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldStartCloudAppWithSystem: Remove autostart.");
                    RemoveCloudAutostartShortcut();
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: UpdateShouldStartCloudAppWithSystem: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Update the status of the Desktop cloud folder shortcut.
        /// </summary>
        /// <param name="shouldBeActive">true: The shortcut should be placed.  False, the shortcut should be removed.</param>
        /// <param name="cloudFolderPath">The full path to the user's cloud folder.</param>
        public static void UpdateShouldShowCloudFolderOnDesktop(bool shouldBeActive, string cloudFolderPath)
        {
            try
            {
                _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderOnDesktop: Entry.");
                if (shouldBeActive)
                {
                    // Get the cloud icon path.
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderOnDesktop: Add the cloud folder icon.");
                    string cloudIconPath = Assembly.GetEntryAssembly().Location;

                    // Add a folder shortcut to the desktop
                    AddCloudFolderShortcutToFolder(cloudFolderPath, Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);
                }
                else
                {
                    // Remove the Desktop item
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderOnDesktop: Remove the cloud folder icon.");
                    RemoveCloudFolderShortcutFromFolder(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: UpdateShouldShowCloudFolderOnDesktop: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Update the status of the Explorer favorites cloud folder shortcut.
        /// </summary>
        /// <param name="shouldBeActive">true: The shortcut should be placed.  False, the shortcut should be removed.</param>
        /// <param name="cloudFolderPath">The full path to the user's cloud folder.</param>
        public static void UpdateShouldShowCloudFolderInExplorerFavorites(bool shouldBeActive, string cloudFolderPath)
        {
            try
            {
                _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderInExplorerFavorites: Entry.");
                if (shouldBeActive)
                {
                    // Get the cloud icon path.
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderInExplorerFavorites: Add the shortcut.");
                    string cloudIconPath = Assembly.GetEntryAssembly().Location;

                    // Add a folder shortcut to the Explorer Favorites list.
                    string explorerFavoritesPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Favorites)) + "\\Links";
                    AddCloudFolderShortcutToFolder(cloudFolderPath, explorerFavoritesPath, Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);
                }
                else
                {
                    // Remove the shortcuts.  Remove the Windows Explorer Favorites list item
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderInExplorerFavorites: Remove the shortcut.");
                    string explorerFavoritesPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Favorites)) + "\\Links";
                    RemoveCloudFolderShortcutFromFolder(explorerFavoritesPath);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: UpdateShouldShowCloudFolderInExplorerFavorites: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }
        /// <summary>
        /// Update the status of the Internet Explorer favorites cloud folder shortcut.
        /// </summary>
        /// <param name="shouldBeActive">true: The shortcut should be placed.  False, the shortcut should be removed.</param>
        /// <param name="cloudFolderPath">The full path to the user's cloud folder.</param>
        public static void UpdateShouldShowCloudFolderInInternetExplorerFavorites(bool shouldBeActive, string cloudFolderPath)
        {
            try
            {
                _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderInInternetExplorerFavorites: Entry.");
                if (shouldBeActive)
                {
                    // Get the cloud icon path.
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderInInternetExplorerFavorites: Add the shortcut.");
                    string cloudIconPath = Assembly.GetEntryAssembly().Location;

                    // Add a folder shortcut to the Internet Explorer favorites list
                    AddCloudFolderShortcutToFolder(cloudFolderPath, Environment.GetFolderPath(Environment.SpecialFolder.Favorites), Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);
                }
                else
                {
                    // Remove the Internet Explorer favorites list item.
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderInInternetExplorerFavorites: Remove the shortcut.");
                    RemoveCloudFolderShortcutFromFolder(Environment.GetFolderPath(Environment.SpecialFolder.Favorites));
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: UpdateShouldShowCloudFolderInInternetExplorerFavorites: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Update the status of the taskbar cloud folder shortcut.
        /// </summary>
        /// <param name="shouldBeActive">true: The shortcut should be placed.  False, the shortcut should be removed.</param>
        /// <param name="cloudFolderPath">The full path to the user's cloud folder.</param>
        /// <param name="shouldWaitForCompletion">true: wait for this task to complete.</param>
        public static void UpdateShouldShowCloudFolderOnTaskbar(bool shouldBeActive, string cloudFolderPath, bool shouldWaitForCompletion)
        {
            try
            {
                _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderOnTaskbar: Entry.");
                if (shouldBeActive)
                {
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderOnTaskbar: Add the shortcut.");
                    PinShowCloudFolderToTaskbar(shouldProcessTaskbar: true, shouldPin: true, scriptShouldSelfDestruct: true, shouldWaitForCompletion: shouldWaitForCompletion);
                }
                else
                {
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderOnTaskbar: Remove the shortcut.");
                    PinShowCloudFolderToTaskbar(shouldProcessTaskbar: true, shouldPin: false, scriptShouldSelfDestruct: true, shouldWaitForCompletion: shouldWaitForCompletion);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: UpdateShouldShowCloudFolderOnTaskbar: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Update the status of the Start menu cloud folder shortcut.
        /// </summary>
        /// <param name="shouldBeActive">true: The shortcut should be placed.  False, the shortcut should be removed.</param>
        /// <param name="cloudFolderPath">The full path to the user's cloud folder.</param>
        /// <param name="shouldWaitForCompletion">true: wait for this task to complete.</param>
        public static void UpdateShouldShowCloudFolderInStartMenu(bool shouldBeActive, string cloudFolderPath, bool shouldWaitForCompletion)
        {
            try
            {
                _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderInStartMenu: Entry.");
                if (shouldBeActive)
                {
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderInStartMenu: Add the shortcut.");
                    PinShowCloudFolderToTaskbar(shouldProcessTaskbar: false, shouldPin: true, scriptShouldSelfDestruct: true, shouldWaitForCompletion: shouldWaitForCompletion);
                }
                else
                {
                    _trace.writeToLog(9, "CLShortcuts: UpdateShouldShowCloudFolderInStartMenu: Remove the shortcut.");
                    PinShowCloudFolderToTaskbar(shouldProcessTaskbar: false, shouldPin: false, scriptShouldSelfDestruct: true, shouldWaitForCompletion: shouldWaitForCompletion);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: UpdateShouldShowCloudFolderInStartMenu: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Update the status of the user choice to use the cloud icon as a custom icon for the cloud folder.
        /// </summary>
        /// <param name="shouldBeActive">true: The shortcut should be placed.  False, the shortcut should be removed.</param>
        /// <param name="cloudFolderPath">The full path to the user's cloud folder.</param>
        public static void UpdateShouldUseCloudIconForCloudFolder(bool shouldBeActive, string cloudFolderPath)
        {
            try
            {
                _trace.writeToLog(9, "CLShortcuts: UpdateShouldUseCloudIconForCloudFolder: Entry.");
                if (shouldBeActive)
                {
                    if (!String.IsNullOrWhiteSpace(cloudFolderPath) && Directory.Exists(cloudFolderPath))
                    {
                        _trace.writeToLog(9, "CLShortcuts: UpdateShouldUseCloudIconForCloudFolder: Add the shortcut.");
                        string cloudIconPath = Assembly.GetEntryAssembly().Location;

                        // Set the custom folder Cloud icon.  That will show anywhere the cloud folder appears.
                        SetCloudFolderIcon(cloudFolderPath, cloudIconPath, "View the Cloud folder");
                    }
                }
                else
                {
                    // Remove all of the Cloud folder shortcuts.
                    if (!String.IsNullOrWhiteSpace(cloudFolderPath) && Directory.Exists(cloudFolderPath))
                    {
                        // Reset the Cloud folder Cloud icon.
                        _trace.writeToLog(9, "CLShortcuts: UpdateShouldUseCloudIconForCloudFolder: Remove the shortcut.");
                        SetCloudFolderIcon(cloudFolderPath, null);
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: UpdateShouldUseCloudIconForCloudFolder: ERROR: Exception.  Msg: <{0}>.", ex.Message));
            }
        }

        /// <summary>
        /// Change the target path in a shortcut file.
        /// </summary>
        /// <param name="pathFolderContainingShortcut">Full path of the folder that contains the shortcut .lnk file (without trailing backslash).</param>
        /// <param name="shortcutNameWithoutLnkExtension"></param>
        /// <param name="newTargetLocation">The full path of the new target item.</param>
        /// <param name="iconExeFilePath">The path to an executable file containing the icon to display in the shortcut.  May be null for the default icon.</param>
        /// <param name="iconOffset">The offset of the icon in the executable file (optional, default 0);</param>
        private static void ModifyShortcutTargetPath(string pathFolderContainingShortcut, string shortcutNameWithoutLnkExtension, string newTargetLocation, string iconExeFilePath = null, int iconOffset = 0)
        {
            try 
	        {
                _trace.writeToLog(9, "CLShortcuts: ModifyShortcutTargetPath: Entry: pathFolderContainingShortcut: <{0}>. shortcutNameWithoutLnkExtension: <{1}>. newTargetLocation: <{2}>.", pathFolderContainingShortcut, shortcutNameWithoutLnkExtension, newTargetLocation);
                Shell32.Shell shl = new Shell32.Shell();
                Shell32.Folder dir = shl.NameSpace(pathFolderContainingShortcut);
                Shell32.FolderItem itm = dir.Items().Item(shortcutNameWithoutLnkExtension + ".lnk");
                Shell32.ShellLinkObject lnk = (Shell32.ShellLinkObject)itm.GetLink;
                if (iconExeFilePath != null)
                {
                    lnk.SetIconLocation(iconExeFilePath, iconOffset);
                }
                lnk.Path = newTargetLocation;
                lnk.Save();
	        }
	        catch (Exception ex)
	        {
                _trace.writeToLog(1, String.Format("CLShortcuts: ModifyShortcutTargetPath: ERROR: Exception.  Msg: <{0}>.", ex.Message));
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

        /// <summary>
        /// Launch the default web browser to browse to a URL page.
        /// </summary>
        /// <param name="urlTarget"></param>
        public static void StartBrowserToUrl(string urlTarget)
        {
            try
            {
                _trace.writeToLog(9, String.Format("CLShortcuts: StartBrowserToUrl: Entry. Url: <{0}>.", urlTarget));
                System.Diagnostics.Process.Start(urlTarget);
            }
            catch(System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                {
                    _trace.writeToLog(1, String.Format("CLShortcuts: StartBrowserToUrl: ERROR: Exception.  Msg: <{0}>.", noBrowser.Message));
                }
            }
            catch (System.Exception other)
            {
                _trace.writeToLog(1, String.Format("CLShortcuts: StartBrowserToUrl: ERROR: Exception(2).  Msg: <{0}>.", other.Message));
            }
        }
    }
}
