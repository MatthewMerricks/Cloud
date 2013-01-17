using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CloudApiPublic.Static;
using CloudApiPublic.Support;

namespace CloudSetupSdkSyncSampleSupport
{
    public class CloudSetupSdkSyncSampleSupport
    {
        private static CLTrace _trace = CLTrace.Instance;

        static int Main(string[] args)
        {

            int rcToReturn = 0;
            // Initialize trace
            string userTempDirectory = System.IO.Path.GetTempPath();
            CLTrace.Initialize(TraceLocation: userTempDirectory, TraceCategory: "CloudSetupSdkSyncSampleSupport", 
                    FileExtensionWithoutPeriod: "log", TraceLevel: 9, LogErrors: true);

            // Check for arguments
            _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Main: Entry.");
            if (args.Length == 0)
            {
                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: Main: No arguments.  Must specify '/i' or '/u'.");
                return -1;
            }

            string firstArg = args[0];
            if (firstArg.Length < 2)
            {
                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: Main: Invalid first argument.  Must be '/i' or '/u'.");
                return -2;
            }
            if (firstArg.Substring(1).ToUpper() == "I")
            {
                rcToReturn = Install();
            }
            else if (firstArg.Substring(1).ToUpper() == "U")
            {
                rcToReturn = Uninstall();
            }
            else
            {
                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: Main: Invalid first argument {0}.  Must be '/i' or '/u'.", firstArg);
                return -3;
            }

            _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Main: Return {0}.", rcToReturn);
            return rcToReturn;
        }

        /// <summary>
        /// Unzip the documentation into the Docs folder
        /// </summary>
        /// <returns></returns>
        private static int Install()
        {
            _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Entry.");
            ZipFile zf = null;
            int rcToReturn = 0;
            try
            {
                // Get the path containing this executabe
                string pathExecutingProgram = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string pathInstall = Path.GetDirectoryName(pathExecutingProgram);
                string archiveFile = pathInstall + "\\Docs\\CloudSdkSyncSampleDocs.zip";
                string outFolder = pathInstall + "\\Docs";
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: pathExecutingProgram: {0}.", pathExecutingProgram);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: pathInstall: {0}.", pathInstall);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: archiveFile: {0}.", archiveFile);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: outFolder: {0}.", outFolder);

                // Make a /Support directory in the installation directory.  We will copy files needed by uninstall to that directory.
                Directory.CreateDirectory(pathInstall + "\\Support");

                // Copy this executing program file to a second file.  This is required because the limited edition
                // of InstallShield only allows custom actions to run during uninstall after all of the installed
                // files have been deleted.
                string pathSupport = pathInstall + "\\Support\\CloudSetupSdkSyncSampleSupport.exe";
                if (File.Exists(pathSupport))
                {
                    _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Delete the support program file.");
                    File.Delete(pathSupport);
                }
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Copy the support program file.");
                File.Copy(pathExecutingProgram, pathSupport);

                // Copy the .config file too.
                pathSupport = pathInstall + "\\Support\\CloudSetupSdkSyncSampleSupport.exe.config";
                if (File.Exists(pathSupport))
                {
                    _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Delete the support program config file.");
                    File.Delete(pathSupport);
                }
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Copy the support program config file.");
                File.Copy(pathExecutingProgram + ".config", pathSupport);

                // Also copy the CloudApiPublic DLL to the \Support directory.
                pathSupport = pathInstall + "\\Support\\CloudApiPublic.dll";
                if (File.Exists(pathSupport))
                {
                    _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Delete the support CloudApiPublic.dll file.");
                    File.Delete(pathSupport);
                }
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Copy the support CloudApiPublic.dll file.");
                File.Copy(pathInstall + "\\CloudApiPublic.dll", pathSupport);

                // Open the documentation zip file and decompress all of its files and folders
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Unzip the docs file.");
                FileStream fs = File.OpenRead(archiveFile);
                zf = new ZipFile(fs);
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;           // Ignore directories
                    }
                    String entryFileName = zipEntry.Name;
                    byte[] buffer = new byte[4096];     // 4K is optimum
                    Stream zipStream = zf.GetInputStream(zipEntry);

                    String fullZipToPath = Path.Combine(outFolder, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }

                // Close the .zip file
                zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                zf.Close(); // Ensure we release resources
                zf = null;
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Done unzipping the docs file.");

                // Delete the .zip file
                File.Delete(archiveFile);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: Install: ERROR: Exception. Msg: {0}.", ex.Message);
                rcToReturn = -100;
                return rcToReturn;
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                    zf = null;
                }
            }

            // Unzip the CloudSdkSyncSample source files and solution
            try
            {
                string pathExecutingProgram = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string pathInstall = Path.GetDirectoryName(pathExecutingProgram);
                string archiveFile = pathInstall + "\\SampleApp\\CloudSdkSyncSampleSource.zip";
                string outFolder = pathInstall + "\\SampleApp";
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: pathExecutingProgram(2): {0}.", pathExecutingProgram);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: pathInstall(2): {0}.", pathInstall);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: archiveFile(2): {0}.", archiveFile);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: outFolder(2): {0}.", outFolder);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Unzip the CloudSdkSyncSample source file.");

                FileStream fs = File.OpenRead(archiveFile);
                zf = new ZipFile(fs);
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;           // Ignore directories
                    }
                    String entryFileName = zipEntry.Name;
                    byte[] buffer = new byte[4096];     // 4K is optimum
                    Stream zipStream = zf.GetInputStream(zipEntry);

                    String fullZipToPath = Path.Combine(outFolder, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }

                // Close the .zip file
                zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                zf.Close(); // Ensure we release resources
                zf = null;
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Done unzipping the CloudSdkSyncSample source file.");

                // Delete the .zip file
                File.Delete(archiveFile);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: Install: ERROR: Exception(2). Msg: {0}.", ex.Message);
                rcToReturn = -101;
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                    zf = null;
                }
            }

            _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Return {0}.", rcToReturn);
            return rcToReturn;
        }

        /// <summary>
        /// Delete the Docs directory
        /// </summary>
        /// <returns></returns>
        private static int Uninstall()
        {
            int rcToReturn = 0;
            try
            {
                // Get the path containing this executabe
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Uninstall: Entry.");
                string pathInstall = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string docsFolder = pathInstall + "\\Docs";
                string sourceFolder = pathInstall + "\\SampleApp";
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Uninstall: pathInstall: {0}.", pathInstall);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Uninstall: docsFolder: {0}.", docsFolder);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Uninstall: sourceFolder: {0}.", sourceFolder);

                // Delete the Docs directory
                if (Directory.Exists(docsFolder))
                {
                    _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Uninstall: Delete the docs folder.");
                    Directory.Delete(docsFolder, recursive: true);
                }

                // Delete the source directory (SampleApp)
                if (Directory.Exists(sourceFolder))
                {
                    _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Uninstall: Delete the source folder.");
                    Directory.Delete(sourceFolder, recursive: true);
                }

                // Schedule cleanup of this executing .exe file and containing directories as possible.
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Uninstall: Call ScheduleCleanup.");
                rcToReturn = ScheduleCleanup();
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Uninstall: Return from ScheduleCleanup. rc: {0}.", rcToReturn);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: Uninstall: ERROR: Exception. Msg: {0}.", ex.Message);
                rcToReturn = -200;
            }

            _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Uninstall: Return {0}.", rcToReturn);
            return rcToReturn;
        }

        /// <summary>
        /// This function schedules a self-destructing VBScript process to run.  The VBScript
        /// process will delete this executing program and clean up the directories if it can.
        /// The .vbs file will delete itself after executing.
        /// </summary>
        private static int ScheduleCleanup()
        {
            // Write the self-destructing script to the user's temp directory and launch it.
            int rcToReturn = 0;
            try
            {
                // Stream the CloudSetupSdkSyncSampleCleanup.vbs file out to the user's temp directory
                // Locate the user's temp directory.
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: ScheduleCleanup: Entry.");
                string userTempDirectory = System.IO.Path.GetTempPath();
                string vbsPath = userTempDirectory + "CloudSetupSdkSyncSampleCleanup.vbs";
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: ScheduleCleanup: userTempDirectory: {0}.", userTempDirectory);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: ScheduleCleanup: vbsPath: {0}.", vbsPath);

                // Get the assembly containing the .vbs resource.
                System.Reflection.Assembly storeAssembly = System.Reflection.Assembly.GetAssembly(typeof(global::CloudSetupSdkSyncSampleSupport.CloudSetupSdkSyncSampleSupport));
                if (storeAssembly == null)
                {
                    _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: ScheduleCleanup: ERROR: Locating resource file.");
                    return -300;
                }

                // Stream the CloudSetupSdkSyncSampleCleanup.vbs file out to the temp directory
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: ScheduleCleanup: Call WriteResourceFileToFilesystemFile.");
                int rc = Helpers.WriteResourceFileToFilesystemFile(storeAssembly, "CloudSetupSdkSyncSampleCleanup", vbsPath);
                if (rc != 0)
                {
                    _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: ScheduleCleanup: Error {0} from WriteResourceFileToFilesystemFile.", rc);
                    return -300 - rc;
                }

                // Now we will create a new process to run the VBScript file.
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: ScheduleCleanup: Launch the .VBS process.");
                string systemFolderPath = Helpers.Get32BitSystemFolderPath();
                string cscriptPath = systemFolderPath + "\\cscript.exe";

                string argumentsString = @" //B //T:30 //Nologo """ + vbsPath + @"""";

                // Launch the process, then exit the application.
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = cscriptPath;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = argumentsString;
                Process.Start(startInfo);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: ScheduleCleanup: .VBS process started.");
            }
            catch (Exception ex)
            {
                rcToReturn = -350;
                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: ScheduleCleanup: ERROR: Exception. Msg: {0}.", ex.Message);
            }

            _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: ScheduleCleanup: Exit.");
            return rcToReturn;
        }
    }
}
