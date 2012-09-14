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
        /// Add shortcuts to the Cloud folder to various quick-access locations
        /// </summary>
        /// <param name="cloudFolderPath">The path to the Cloud folder.</param>
        public static void AddCloudFolderShortcuts(string cloudFolderPath)
        {
            if (!String.IsNullOrWhiteSpace(cloudFolderPath))
            {
                // Add or update the various Cloud folder shortcuts because the Cloud folder path has just changed.
                string cloudIconPath = Assembly.GetEntryAssembly().Location;

                string explorerFavoritesPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Favorites)) + "\\Links";
                CLShortcuts.AddCloudFolderShortcutToFolder(cloudFolderPath, Environment.GetFolderPath(Environment.SpecialFolder.Favorites), Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);
                CLShortcuts.AddCloudFolderShortcutToFolder(cloudFolderPath, Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);
                CLShortcuts.AddCloudFolderShortcutToFolder(cloudFolderPath, explorerFavoritesPath, Resources.Resources.CloudFolderIconDescription, cloudIconPath, 0);

                // Set the folder Cloud icon.
                CLShortcuts.SetCloudFolderIcon(Settings.Instance.CloudFolderPath, cloudIconPath, "View the Cloud folder");
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
    }
}
