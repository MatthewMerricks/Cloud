//
// CloudSetupSdkSyncSampleSupport.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Cloud.Static;
using Cloud.Support;
using System.Windows;

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
        /// Unzip the documentation into the Documentation folder
        /// </summary>
        /// <returns></returns>
        private static int Install()
        {
            _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Entry.");
            ZipFile zf = null;
            int rcToReturn = 0;

            string pathExecutingProgram = null;
            string pathInstall = null;

            try
            {
                // Get the path containing this executabe
                pathExecutingProgram = System.Reflection.Assembly.GetExecutingAssembly().Location;
                pathInstall = Path.GetDirectoryName(pathExecutingProgram);
                string archiveFile = pathInstall + "\\Documentation\\SampleLiveSyncDocs.zip";
                string outFolder = pathInstall + "\\Documentation";
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: pathExecutingProgram: {0}.", pathExecutingProgram);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: pathInstall: {0}.", pathInstall);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: archiveFile: {0}.", archiveFile);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: outFolder: {0}.", outFolder);

                // Make a /Support directory in the installation directory.  We will copy files needed by uninstall to that directory.
                Directory.CreateDirectory(pathInstall + "\\Support");

                // Copy this executing program file to a second file.  This is required because the limited edition
                // of InstallShield only allows custom actions to run during uninstall after all of the installed
                // files have been deleted.  Also copy all of the required support files.
                // Copy CloudSetupSdkSyncSampleSupport.exe.
                string pathWork = pathInstall + "\\Support\\CloudSetupSdkSyncSampleSupport.exe";
                if (File.Exists(pathWork))
                {
                    _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Delete the support program file.");
                    File.Delete(pathWork);
                }
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Copy the support program file.");
                File.Copy(pathExecutingProgram, pathWork);

                // Copy CloudSetupSdkSyncSampleSupport.exe.config.
                pathWork = pathInstall + "\\Support\\CloudSetupSdkSyncSampleSupport.exe.config";
                if (File.Exists(pathWork))
                {
                    _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Delete the support program config file.");
                    File.Delete(pathWork);
                }
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Copy the support program config file.");
                File.Copy(pathExecutingProgram + ".config", pathWork);

                // Copy Cloud.dll.
                pathWork = pathInstall + "\\Support\\Cloud.dll";
                if (File.Exists(pathWork))
                {
                    _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Delete the support Cloud.dll file.");
                    File.Delete(pathWork);
                }
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Copy the support Cloud.dll file.");
                File.Copy(pathInstall + "\\Cloud.dll", pathWork);

                // Copy ICSharpCode.SharpZipLib.dll.
                pathWork = pathInstall + "\\Support\\ICSharpCode.SharpZipLib.dll";
                if (File.Exists(pathWork))
                {
                    _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Delete the support ICSharpCode.SharpZipLib.dll file.");
                    File.Delete(pathWork);
                }
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Copy the support ICSharpCode.SharpZipLib.dll file.");
                File.Copy(pathInstall + "\\ICSharpCode.SharpZipLib.dll", pathWork);

                // Copy Interop.Shell32.dll.
                pathWork = pathInstall + "\\Support\\Interop.Shell32.dll";
                if (File.Exists(pathWork))
                {
                    _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Delete the support Interop.Shell32.dll file.");
                    File.Delete(pathWork);
                }
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Copy the support Interop.Shell32.dll file.");
                File.Copy(pathInstall + "\\Interop.Shell32.dll", pathWork);

                // Copy System.Xaml.dll.
                pathWork = pathInstall + "\\Support\\System.Xaml.dll";
                if (File.Exists(pathWork))
                {
                    _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Delete the support System.Xaml.dll file.");
                    File.Delete(pathWork);
                }
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Copy the support System.Xaml.dll file.");
                File.Copy(pathInstall + "\\System.Xaml.dll", pathWork);


                // Determine the SQL CE installation program to run
                string fileNameExt;
                if (IntPtr.Size == 4)
                {
                    // 32-bit 
                    fileNameExt = "SSCERuntime_x86-ENU.exe";
                }
                else
                {
                    // 64-bit 
                    fileNameExt = "SSCERuntime_x64-ENU.exe";
                }

                // Copy the SQL CE installation program to the \Support directory.
                pathWork = pathInstall + "\\Support\\" + fileNameExt;
                if (File.Exists(pathWork))
                {
                    _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Delete the support file {0}.", pathWork);
                    File.Delete(pathWork);
                }
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Copy the support SSCERuntime*.exe file.");
                File.Copy(pathInstall + "\\" + fileNameExt, pathWork);

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

            // Unzip the SampleLiveSync source files and solution
            try
            {
                string archiveFile = pathInstall + "\\Sample Code\\Sync\\Live\\Project\\SampleLiveSyncSource.zip";
                string outFolder = pathInstall + "\\Sample Code\\Sync\\Live\\Project";
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: pathExecutingProgram(2): {0}.", pathExecutingProgram);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: pathInstall(2): {0}.", pathInstall);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: archiveFile(2): {0}.", archiveFile);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: outFolder(2): {0}.", outFolder);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Unzip the SampleLiveSync source file.");

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
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Done unzipping the SampleLiveSync source file.");

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

            try
            {
                // Copy RateBar.CSDK.dll from the App directory to the Project bin folders
                string source = pathInstall + "\\Sample Code\\Sync\\Live\\App\\RateBar.CSDK.dll";
                string target = pathInstall + "\\Sample Code\\Sync\\Live\\Project\\bin\\Release\\RateBar.CSDK.dll";
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Copy RateBar.CSDK.dll. Src: <{0}>. Target: <{1}>.", source, target);
                File.Copy(source, target);
                target = pathInstall + "\\Sample Code\\Sync\\Live\\Project\\bin\\Debug\\RateBar.CSDK.dll";
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Copy RateBar.CSDK.dll. Src: <{0}>. Target: <{1}>.", source, target);
                File.Copy(source, target);

                // Install all of the DLLs required for the sample in the gac.
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Call InstallDllsToGac.");
                InstallDllsToGac(pathInstall);

                // Install SQL CE V4.0.
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Call InstallSqlCe.");
                InstallSqlCe(pathInstall);
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Back from InstallSqlCe.");

                // Schedule cleanup of the files in the installation directory.
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Call ScheduleCleanup.");
                rcToReturn = ScheduleCleanup("CloudSetupSdkSyncSampleInstallCleanup");
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Innstall: Return from ScheduleCleanup. rc: {0}.", rcToReturn);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: Install: ERROR: Exception(3). Msg: {0}.", ex.Message);
                rcToReturn = -201;
            }

            _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Install: Return {0}.", rcToReturn);
            return rcToReturn;
        }

        /// <summary>
        /// Install SQL CE.
        /// </summary>
        /// <param name="pathInstall">The installation path (program files\Cloud.com).</param>
        private static void InstallSqlCe(string pathInstall)
        {
            try
            {
                // Determine whether an installation is required.
                if (IsSqlCeV40Installed())
                {
                    // Not required to install
                    _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallSqlCe: Installation not required.");
                }

                // Create a flag file for CloudSetupSdkSyncSampleInstallCleanup to actually do the installation.
                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallSqlCe: Request installation.");
                System.IO.File.WriteAllText(pathInstall + "\\Support\\InstallSqlCe.flg", "Flag file");
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallSqlCe: ERROR: Exception. Msg: {0}.", ex.Message);
            }
        }

        /// <summary>
        /// Determine whether SQL CE V4.0 is installed.
        /// From ErikEJ: http://stackoverflow.com/questions/10534158/how-to-detect-if-sql-server-ce-4-0-is-installed
        /// </summary>
        /// <returns>bool: True: It is installed.</returns>
        public static bool IsSqlCeV40Installed()
        {
            try
            {
                System.Reflection.Assembly.Load("System.Data.SqlServerCe, Version=4.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91");
            }
            catch (System.IO.FileNotFoundException)
            {
                return false;
            }
            try
            {
                var factory = System.Data.Common.DbProviderFactories.GetFactory("System.Data.SqlServerCe.4.0");
            }
            catch (System.Configuration.ConfigurationException)
            {
                return false;
            }
            catch (System.ArgumentException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Install of the DLLs required by the sample app to the gac.  These DLLs must all be signed with:
        ///   Version=x.x.x.x
        ///   Culture=neutral
        ///   PublicKeyToken=\<our SDK public key\>
        ///   processorArchitecture=MSIL
        /// </summary>
        /// <param name="pathInstall">The installation path (program files\Cloud.com).</param>
        private static void InstallDllsToGac(string pathInstall)
        {
            try 
        	{	        
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: InstallDllsToGac: Entry.");
                string pathSdk = pathInstall + "\\CloudSDK";

                System.EnterpriseServices.Internal.Publish p = new System.EnterpriseServices.Internal.Publish();

                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallDllsToGac: Copy BadgeCOMLib.dll to the gac.");
                p.GacInstall(pathSdk + "\\BadgeCOMLib.dll");

                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallDllsToGac: Copy Cloud.dll to the gac.");
                p.GacInstall(pathSdk + "\\Cloud.dll");

                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallDllsToGac: Copy ErikEJ.SqlCe40.CSDK.dll to the gac.");
                p.GacInstall(pathSdk + "\\ErikEJ.SqlCe40.CSDK.dll");

                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallDllsToGac: Copy Microsoft.CSharp.dll to the gac.");
                p.GacInstall(pathSdk + "\\Microsoft.CSharp.dll");

                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallDllsToGac: Copy Microsoft.Net.Http.CSDK.dll to the gac.");
                p.GacInstall(pathSdk + "\\Microsoft.Net.Http.CSDK.dll");

                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallDllsToGac: Copy Microsoft.Practices.ServiceLocation.CSDK.dll to the gac.");
                p.GacInstall(pathSdk + "\\Microsoft.Practices.ServiceLocation.CSDK.dll");

                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallDllsToGac: Copy Newtonsoft.Json.CSDK.dll to the gac.");
                p.GacInstall(pathSdk + "\\Newtonsoft.Json.CSDK.dll");

                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallDllsToGac: Copy Salient.Data.CSDK.dll to the gac.");
                p.GacInstall(pathSdk + "\\Salient.Data.CSDK.dll");

                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallDllsToGac: Copy SimpleJson.CSDK.dll to the gac.");
                p.GacInstall(pathSdk + "\\SimpleJson.CSDK.dll");

                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallDllsToGac: Copy System.Dynamic.dll to the gac.");
                p.GacInstall(pathSdk + "\\System.Dynamic.dll");
    	    }
	        catch (Exception ex)
	        {
                _trace.writeToLog(1, "CloudSetupSdkSyncSampleSupport: InstallDllsToGac: ERROR: Exception: Msg: {0}.", ex.Message);
	        }
        }

        /// <summary>
        /// Delete the Docs directory
        /// </summary>
        /// <returns></returns>
        private static int Uninstall()
        {
            string pathExecutingProgram = null;
            string pathInstall = null;
            int rcToReturn = 0;

            try
            {
                // Get the path containing this executable's DLLs.
                _trace.writeToLog(9, "CloudSetupSdkSyncSampleSupport: Uninstall: Entry.");
                pathExecutingProgram = System.Reflection.Assembly.GetExecutingAssembly().Location;
                pathInstall = Path.GetDirectoryName(pathExecutingProgram);
                string docsFolder = pathInstall + "\\Documentation";
                string sourceFolder = pathInstall + "\\Sample Code\\Sync\\Live\\Project";
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
                rcToReturn = ScheduleCleanup("CloudSetupSdkSyncSampleUninstallCleanup");
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
        /// <param name="vbScriptFileToRun">The name of the VB script file to run.</param>
        private static int ScheduleCleanup(string vbScriptFileToRun)
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
                int rc = Helpers.WriteResourceFileToFilesystemFile(storeAssembly, vbScriptFileToRun, vbsPath);
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
