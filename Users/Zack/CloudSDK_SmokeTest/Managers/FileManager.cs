using CloudApiPublic.Model;
using CloudSDK_SmokeTest.Helpers;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public class FileManager
    {
        public static int RunFileCreationTask(InputParams paramSet, SmokeTask smokeTask, ref ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            Creation creation = smokeTask as Creation;
            if (creation == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            int responseCode = -1;
            string fullPath = string.Empty;            
            FileHelper.CreateFilePathString(creation, out fullPath);
            try
            {
                Console.WriteLine(string.Format("Entering Creation Task. Current Creation Type: {0}", smokeTask.ObjectType.type.ToString()));
                responseCode = manager.Create(paramSet, smokeTask, new FileInfo(fullPath), creation.Name, ref ProcessingErrorHolder);
                Console.WriteLine("Exiting Creation Task.");
            }
            catch (Exception ex)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                }
            }
            return responseCode;

        }

        public static int RunFileDeletionTask(InputParams paramSet, SmokeTask smokeTask, ref ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int deleteReturnCode = 0;
            try
            {
                if (!(smokeTask is Deletion))
                    return (int)FileManagerResponseCodes.InvalidTaskType;

                Console.WriteLine(string.Format("Entering Delete {0}", smokeTask.ObjectType.type.ToString()));
                deleteReturnCode = manager.Delete(paramSet, smokeTask);
                Console.WriteLine(string.Format("Delete {0} Exiting", smokeTask.ObjectType.type.ToString()));


            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
            return deleteReturnCode;
        }

        public static int RunFileRenameTask(InputParams paramSet, SmokeTask smokeTask, ref ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int responseCode = -1;
            try
            {
                Rename task = smokeTask as Rename;
                if (task == null)
                    return (int)FileManagerResponseCodes.InvalidTaskType;

                Console.WriteLine(string.Format("Entering Rename {0}", smokeTask.ObjectType.type.ToString()));
                responseCode = manager.Rename(paramSet, task, task.RelativeDirectoryPath, task.OldName, task.NewName);
                Console.WriteLine(string.Format("Rename {0} Exiting", smokeTask.ObjectType.type.ToString()));
            }
            catch (Exception exception)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                }
            }
            return responseCode;
        }
    }
}
