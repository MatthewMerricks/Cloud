using CloudApiPublic.Model;
using CloudSDK_SmokeTest.Managers;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Helpers
{
    public sealed class SmokeTestTaskHelper
    {
        #region Constants
        public const string FileCreationString = "FileCreation";
        #endregion 

        #region Static
        public static long RouteToTaskMethod(InputParams paramSet, SmokeTask smokeTask, GenericHolder<CLError> ProcessingErrorHolder)
        {
            long returnValue = -1;
            ManualSyncManager manager = new ManualSyncManager(paramSet);
            switch (smokeTask.type)
            { 
                case SmokeTaskType.CreateSyncBox:
                    returnValue = RunCreateSyncBoxTask(paramSet, smokeTask, ref ProcessingErrorHolder );
                    break;
                case SmokeTaskType.Creation:
                    RunCreationTask(paramSet, smokeTask, ref manager, ref ProcessingErrorHolder);
                    break;
                case SmokeTaskType.DownloadAllSyncBoxContent:
                    RunDownloadAllSyncBoxContentTask(paramSet, smokeTask, ref manager, ref ProcessingErrorHolder);
                    break;
                case SmokeTaskType.Deletion:
                    RunFileDeletionTask(paramSet, smokeTask, ref manager, ref ProcessingErrorHolder);
                    break;
                case SmokeTaskType.Rename:
                    RunFileRenameTask(paramSet, smokeTask, ref manager, ref ProcessingErrorHolder);
                    break;

            }
            return returnValue;
        }

        public static long RunCreateSyncBoxTask(InputParams paramSet, SmokeTask smokeTask, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            long? newBoxId;
            CreateSyncBox createTask = smokeTask as CreateSyncBox;
            if (createTask != null && createTask.CreateNew == true)
            {
                newBoxId = SyncBoxManager.StartCreateNewSyncBox(paramSet, ref ProcessingErrorHolder);
                if (newBoxId == (long)0)
                {
                    Exception newSyncBoxException = new Exception("There was an error creating a new Sync Box.");
                    lock (ProcessingErrorHolder)
                    {
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + newSyncBoxException;
                    }
                }
                    
            }
            else 
            { 
                SyncBoxMapper.SyncBoxes.Add(SyncBoxMapper.SyncBoxes.Count(), paramSet.ManualSyncBoxID);
                newBoxId = paramSet.ManualSyncBoxID;
            }        
            return newBoxId.HasValue ? newBoxId.Value : 0;
        }

        public static void RunCreationTask(InputParams paramSet, SmokeTask smokeTask, ref ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            string fullPath = string.Empty;
            FileInfo fi = null;
            Creation creation = smokeTask as Creation;
            try
            {

                CreateFilePathString(creation, out fullPath);
                if (!string.IsNullOrEmpty(fullPath))
                {
                    fi = new FileInfo(fullPath);
                }
                if (fi != null)
                {
                    TryCreate(paramSet, smokeTask, fi, creation.Name, ref ProcessingErrorHolder, ref manager);
                }
            }
            catch (Exception ex)
            {
                Exception outerException = new Exception();
                if (creation != null)
                {
                    outerException = new Exception(
                                                        string.Format("There was an error obtaining the Local File to Create. FilePath: {0}, FileName={1}",
                                                        creation.Path ?? "<filepath>",
                                                        creation.Name ?? "<filename>"),
                                                        ex
                                                    );
                }
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + outerException;
                }
            }
            
        }        
        public static void RunDownloadAllSyncBoxContentTask(InputParams paramSet, SmokeTask smokeTask, ref ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            try
            {
                manager.InitiateDownloadAll(smokeTask, ref ProcessingErrorHolder); 
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
        }

        public static void RunFileDeletionTask(InputParams paramSet, SmokeTask smokeTask, ref ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int deleteReturnCode = 0;
            try
            {
                if (!(smokeTask is Deletion))
                    throw new Exception("Task Passed to File Deletion was not of type FileDeletion");

                deleteReturnCode = manager.Delete(paramSet, smokeTask);
                
                
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
        }

        public static void RunFileRenameTask(InputParams paramSet, SmokeTask smokeTask,  ref ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            try
            {
                if (!(smokeTask is Rename))
                    throw new Exception("Task Passed to Rename File is not of type FileRename.");

                Rename task = smokeTask as Rename;
                if (task == null)
                    throw new Exception("There was an error casting the FileRename SmokeTask to type FileRename");

                manager.Rename(paramSet, task, task.RelativeDirectoryPath, task.OldName, task.NewName);
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
        }

        
        #endregion         

        #region Private

        private static bool pathEndsWithSlash(string path)
        {
            if (path.LastIndexOf('\\') == (path.Count() - 1))
                return true;
            else
                return false;
        }

        private static void CreateFilePathString(Creation creation, out string fullPath)
        {
            string modPath = string.Empty;
            string modName = string.Empty;
            if (creation.Name != null && creation.Path != null)
            {
                modPath = creation.Path.Replace("\"", "");
                modName = creation.Name.Replace("\"", "");
                if (pathEndsWithSlash(modPath))
                    modPath = modPath.Remove(modPath.Count() - 1, 1);

                fullPath = modPath + "\\" + modName;
            }
            else
            {
                fullPath = string.Empty;
                Console.WriteLine("Both FileName and FilePath are Required");
            }
        }

        private static void TryCreate(InputParams paramSet, SmokeTask smokeTask, FileInfo fi, string fileName, ref GenericHolder<CLError> ProcessingErrorHolder, ref ManualSyncManager manager)
        {
            try
            {
               int responseCode =  manager.Create(paramSet, smokeTask, fi, fileName, ref ProcessingErrorHolder);
            }
            catch (Exception ex)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex ;
                }
            }
        }

        #endregion 
    }
}
