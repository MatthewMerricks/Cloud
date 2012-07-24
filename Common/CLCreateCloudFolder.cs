//
//  CLCreateCloudFolder.cs
//  Cloud Windows
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
using CloudApiPublic.Static;
using win_client.AppDelegate;

namespace win_client.Common
{
    public class CLCreateCloudFolder
    {
        /// <summary>
        /// Perform one-time installation (cloud folder, and any OS support)
        /// </summary>
        public static void CreateCloudFolder(string cloudFolderPath, out DateTime creationTime, out CLError error)
        {
            error = null;

            try
            {
                if (!Directory.Exists(cloudFolderPath))
                {
                    Directory.CreateDirectory(cloudFolderPath);
                    Directory.CreateDirectory(cloudFolderPath + @"\Public");
                    Directory.CreateDirectory(cloudFolderPath + @"\Pictures");
                }

                // The creation time is the newly created time (above), or the existing directory creation time.
                creationTime = Directory.GetCreationTimeUtc(cloudFolderPath);

                // TODO: Assign our own icon to the newly created Cloud folder
                // TODO: Assign our own icon to the newly created Cloud\Public folder
                // TODO: Assign our own icon to the newly created Cloud\Pictures folder
                // TODO: Set a shortcut to the Cloud folder into Explorer toolbar
                // TODO: Set a shortcut to the Cloud folder onto the Desktop.
                // TODO: Add our Cloud app menu and icon to the System Tray.  Set it to be always visible.
            }
            catch (Exception e)
            {
                CLError err = new CLError();
                err.errorDomain = CLError.ErrorDomain_Application;
                err.errorDescription = CLAppDelegate.Instance.ResourceManager.GetString("appDelegateExceptionCreatingFolder");
                err.errorCode = (int)CLError.ErrorCodes.Exception;
                err.errorInfo = new Dictionary<string, object>();
                err.errorInfo.Add(CLError.ErrorInfo_Exception, e);
                error = err;
                creationTime = (DateTime)Helpers.DefaultForType(typeof(DateTime));
                return;
            }
        }
    }
}
