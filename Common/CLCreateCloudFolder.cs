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
using CloudApiPublic.SQLIndexer;
using win_client.Services.FileSystemMonitoring;
using win_client.Resources;
using CloudApiPublic.Support;

namespace win_client.Common
{
    public class CLCreateCloudFolder
    {
        private static CLTrace _trace = CLTrace.Instance;

        /// <summary>
        /// Perform one-time installation (cloud folder, and any OS support)
        /// </summary>
        public static void CreateCloudFolder(string cloudFolderPath, out DateTime creationTime, out CLError error)
        {
            error = null;

            try
            {
                _trace.writeToLog(9, "CreateCloudFolder: Entry: Create cloud folder at <{0}>.", cloudFolderPath);

                if (!IsPathInDirectory(cloudFolderPath, Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))))
                {
                    throw new Exception(String.Format("The Cloud directory must be located somewhere in your user home directory ({0}).", 
                                        Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))));
                }

                if (!Directory.Exists(cloudFolderPath))
                {
                    // This directory does not exist.  Create a new cloud folder here.
                    _trace.writeToLog(9, "CreateCloudFolder: Creating a new cloud folder.");
                    Directory.CreateDirectory(cloudFolderPath);

                    // The server does this now.
                    //Directory.CreateDirectory(cloudFolderPath + "\\" + Resources.Resources.CloudFolderDocumentsFolder);
                    //Directory.CreateDirectory(cloudFolderPath + "\\" + Resources.Resources.CloudFolderVideosFolder);
                    //Directory.CreateDirectory(cloudFolderPath + "\\" + Resources.Resources.CloudFolderPicturesFolder);

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


        /// <summary>
        /// Determine whether a path is the same as or contained in another directory.
        /// </summary>
        /// <param name="testPath">Does this test path exist at or in 'inPath'?</param>
        /// <param name="inPath">The containing path.</param>
        /// <returns></returns>
        public static bool IsPathInDirectory(string testPath, string inPath)
        {
            DirectoryInfo diInPath = new DirectoryInfo(inPath); 
            DirectoryInfo diTestPath = new DirectoryInfo(testPath); 
            bool isInDir = false;

            if (diTestPath.FullName == diInPath.FullName)
            {
                return true;
            }

            while (diTestPath.Parent != null)
            {
                if (diTestPath.Parent.FullName == diInPath.FullName)
                {
                    isInDir = true;
                    break;
                }
                else
                {
                    diTestPath = diTestPath.Parent;
                }
            }

            return isInDir;
        }

        /// <summary>
        /// Tests whether a new cloud folder location is allowed.  It must be located in the user
        /// home directory, and it must not be the same as or inside the current cloud folder path.
        /// The target location must not exist and this user must have the proper rights to
        /// create a folder there.  Test that by actually creating and deleting a directory at
        /// the target location.
        /// </summary>
        /// <returns></returns>
        public static bool IsNewCloudFolderLocationValid(string existingCloudFolderPath, string newCloudFolderPath)
        {
            // The new path must be in the user home directory.
            _trace.writeToLog(9, "CLCreateCloudFolder: IsNewCloudFolderLocationValid: Entry. existingPath: <{0}>. newPath: <{1}>.", existingCloudFolderPath, newCloudFolderPath);
            if (!IsPathInDirectory(newCloudFolderPath, Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))))
            {
                return false;
            }

            // The new path must not be at or in the existing path
            if (IsPathInDirectory(newCloudFolderPath, existingCloudFolderPath))
            {
                return false;
            }

            // Test that we can actually create a folder at the target location.
            try
            {
                Directory.CreateDirectory(newCloudFolderPath);
                Directory.Delete(newCloudFolderPath);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(9, "CLCreateCloudFolder: IsNewCloudFolderLocationValid: ERROR. Exception.  Msg: <{0}>.", ex.Message);
                return false;
            }

            return true;
        }
    }
}
