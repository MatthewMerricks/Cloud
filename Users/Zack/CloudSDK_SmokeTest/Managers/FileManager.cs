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
            int responseCode = -1;
            string fullPath = string.Empty;
            FileInfo fi = null;
            Creation creation = smokeTask as Creation;
            try
            {

                FileHelper.CreateFilePathString(creation, out fullPath);
                if (!string.IsNullOrEmpty(fullPath))
                {
                    fi = new FileInfo(fullPath);
                }
                if (fi != null)
                {
                    Console.WriteLine(string.Format("Entering Creation Task. Current Creation Type: {0}", smokeTask.ObjectType.type.ToString()));
                    responseCode = FileHelper.TryCreate(paramSet, smokeTask, fi, creation.Name, ref ProcessingErrorHolder, ref manager);
                    Console.WriteLine("Exiting Creation Task.");
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
            return responseCode;

        }

        public static int RunFileDeletionTask(InputParams paramSet, SmokeTask smokeTask, ref ManualSyncManager manager, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int deleteReturnCode = 0;
            try
            {
                if (!(smokeTask is Deletion))
                    throw new Exception("Task Passed to File Deletion was not of type FileDeletion");

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
                if (!(smokeTask is Rename))
                    throw new Exception("Task Passed to Rename File is not of type FileRename.");

                Rename task = smokeTask as Rename;
                if (task == null)
                    throw new Exception("There was an error casting the FileRename SmokeTask to type FileRename");

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
