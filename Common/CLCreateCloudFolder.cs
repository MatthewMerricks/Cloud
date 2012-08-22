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
using CloudApiPrivate.Model.Settings;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using win_client.AppDelegate;
using SQLIndexer;
using win_client.Services.FileSystemMonitoring;
using win_client.Resources;
using CloudApiPublic.Support;

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
            CLTrace _trace = CLAppDelegate.Instance.GetTrace();

            try
            {
                _trace.writeToLog(9, "CreateCloudFolder: Create cloud folder at <{0}>.", cloudFolderPath);

                if (!Directory.Exists(cloudFolderPath))
                {
                    // This directory does not exist.  Create a new cloud folder here.
                    _trace.writeToLog(9, "CreateCloudFolder: Creating a new cloud folder.");
                    Directory.CreateDirectory(cloudFolderPath);
                    Directory.CreateDirectory(cloudFolderPath + "\\" + Resources.Resources.CloudFolderPublicFolder);
                    Directory.CreateDirectory(cloudFolderPath + "\\" + Resources.Resources.CloudFolderPicturesFolder);

                    // Reset the index to a new clean sync point so it will scan this new folder.
                    CLFSMonitoringService.Instance.IndexingAgent.WipeIndex(cloudFolderPath);
                }
                else
                {
                    // The cloud directory already exists here.  It could be the cloud folder we have been
                    // using (perhaps moved or renamed), or a new cloud directory (new to the index).
                    _trace.writeToLog(9, "CreateCloudFolder: That cloud folder already exists.");
                    if (Settings.Instance.CloudFolderCreationTimeUtc == Directory.GetCreationTimeUtc(cloudFolderPath))
                    {
                        // This is the cloud folder we have been using.  It may be in a different location though.
                        _trace.writeToLog(9, "CreateCloudFolder: The creation times match.  We will use that existing folder.");
                        if (cloudFolderPath.Equals(Settings.Instance.CloudFolderPath, StringComparison.InvariantCulture))
                        {
                            // We lost the Cloud folder somehow, and the user may have put it back into the same location.
                            // The index is OK.  We will just do nothing and use the folder in this location.
                            _trace.writeToLog(9, "CreateCloudFolder: The path matches the settings path.  Just use the existing folder.");
                        }
                        else
                        {
                            // The cloud folder was moved or renamed.  Rebase the index to this new location.
                            _trace.writeToLog(9, "CreateCloudFolder: The path does not matche the settings path.  Rebase the index from the old path <{0}>.", Settings.Instance.CloudFolderPath);
                            long syncCounter;
                            CLFSMonitoringService.Instance.IndexingAgent.RecordCompletedSync(null, new long[0], out syncCounter, cloudFolderPath);
                        }
                    }
                    else
                    {
                        // This is a new cloud folder.  Reset the index to a new clean sync point so it will
                        // scan this new folder.
                        _trace.writeToLog(9, "CreateCloudFolder: The creation times don't match.  This is a new folder.  Rebase the index..");
                        CLFSMonitoringService.Instance.IndexingAgent.WipeIndex(cloudFolderPath);
                    }
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
                err.errorDescription = Resources.Resources.appDelegateExceptionCreatingFolder;
                err.errorCode = (int)CLError.ErrorCodes.Exception;
                err.errorInfo.Add(CLError.ErrorInfo_Exception, e);
                error = err;
                creationTime = (DateTime)Helpers.DefaultForType(typeof(DateTime));
                return;
            }
        }
    }
}
