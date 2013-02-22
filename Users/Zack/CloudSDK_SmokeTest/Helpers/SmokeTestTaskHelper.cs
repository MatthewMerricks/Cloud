﻿using CloudApiPublic.Model;
using CloudSDK_SmokeTest.Events.CLEventArgs;
using CloudSDK_SmokeTest.Events.ManagerEventArgs;
using CloudSDK_SmokeTest.Interfaces;
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
                    returnValue = SyncBoxManager.RunCreateSyncBoxTask(paramSet, smokeTask, ref ProcessingErrorHolder );
                    break;
                case SmokeTaskType.Creation:
                    if (smokeTask.ObjectType.type == ModificationObjectType.File || smokeTask.ObjectType.type == ModificationObjectType.Folder)
                        returnValue = FileManager.RunFileCreationTask(paramSet, smokeTask, ref manager, ref ProcessingErrorHolder);
                    else if (smokeTask.ObjectType.type == ModificationObjectType.Plan)
                        returnValue = PlanManager.RunCreatePlan(paramSet, smokeTask, ref ProcessingErrorHolder);
                    else if (smokeTask.ObjectType.type == ModificationObjectType.SyncBox)
                        returnValue = SyncBoxManager.RunCreateSyncBoxTask(paramSet, smokeTask, ref ProcessingErrorHolder);
                    else if (smokeTask.ObjectType.type == ModificationObjectType.Session)
                        returnValue = SessionManager.RunCreateSessionTask(paramSet, smokeTask, ref ProcessingErrorHolder);
                    break;
                case SmokeTaskType.DownloadAllSyncBoxContent:
                    returnValue = ManualSyncManager.RunDownloadAllSyncBoxContentTask(paramSet, smokeTask, ref manager, ref ProcessingErrorHolder);
                    break;
                case SmokeTaskType.Deletion:
                    if (smokeTask.ObjectType.type == ModificationObjectType.File || smokeTask.ObjectType.type == ModificationObjectType.Folder)
                        returnValue = FileManager.RunFileDeletionTask(paramSet, smokeTask, ref manager, ref ProcessingErrorHolder);
                    else if (smokeTask.ObjectType.type == ModificationObjectType.SyncBox)
                        returnValue = SyncBoxManager.RunSyncBoxDeletionTask(paramSet, smokeTask, ref ProcessingErrorHolder);
                    else if (smokeTask.ObjectType.type == ModificationObjectType.Session)
                        returnValue = SessionManager.RunSessionDeletionTask(paramSet, smokeTask, ref ProcessingErrorHolder);
                    else if (smokeTask.ObjectType.type == ModificationObjectType.Plan)
                        returnValue = PlanManager.RunPlanDeletionTask(paramSet, smokeTask, ref ProcessingErrorHolder);
                    break;
                case SmokeTaskType.Rename:
                    if (smokeTask.ObjectType.type == ModificationObjectType.File || smokeTask.ObjectType.type == ModificationObjectType.Folder)
                        returnValue = FileManager.RunFileRenameTask(paramSet, smokeTask, ref manager, ref ProcessingErrorHolder);
                    if (smokeTask.ObjectType.type == ModificationObjectType.SyncBox)
                        returnValue = SyncBoxManager.RunSyncBoxRenameTask(paramSet, smokeTask, ref ProcessingErrorHolder);
                    break;
                case SmokeTaskType.ListItems:
                    returnValue = ItemsListManager.RunListItemsTask(paramSet, smokeTask, ref ProcessingErrorHolder);
                    break;
                case SmokeTaskType.HttpTest:
                    RunHttpTestTask();
                    break;

            }
            return returnValue;
        }

        public static long RouteToTask(SmokeTestManagerEventArgs e)
        {
            
            long responseCode = 0;
            ISmokeTaskManager manager = SmokeTaskManager.SelectManager(e.CurrentTask);
            switch (e.CurrentTask.type)
            { 
                case SmokeTaskType.Creation:
                    responseCode = manager.Create(e);
                    break;
                case SmokeTaskType.Deletion:
                    responseCode = manager.Delete(e);
                    break;
                case SmokeTaskType.Rename:
                    responseCode = manager.Rename(e);
                    break;
                case SmokeTaskType.DownloadAllSyncBoxContent:
                    responseCode = manager.Download(e);
                    break;
                case SmokeTaskType.ListItems:
                    responseCode = manager.ListItems(e);
                    break;
                case SmokeTaskType.CreateSyncBox:
                    responseCode = manager.Create(e);
                    break;
                default:
                    responseCode = (int)FileManagerResponseCodes.InvalidTaskType;
                    break;

            }
            return responseCode;
        }

        

        public static int RunHttpTestTask()
        {
            int responseCode = -1;
            throw new NotImplementedException("RunHttpTestTask in SmokeTestTaskHelper is Not Implemented");
            return responseCode;
        }
        #endregion         
    }
}
