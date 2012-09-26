//
//  RegisterCom.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using CloudApiPrivate.Common;
using CloudApiPrivate.Model.Settings;
using CloudApiPrivate.Model;
using CloudApiPublic.Model;
using CloudApiPublic.Support;
using Microsoft.Win32;

namespace RegisterCom
{
    public class Registrar : IDisposable
    {
        private IntPtr hLib;
        private CLTrace _trace = CLTrace.Instance;
        
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        internal delegate int PointerToMethodInvoker();

        public Registrar(string filePath)
        {
            _trace.writeToLog(1, String.Format("Registrar: LoadLibrary at path <{0}>.", filePath));
            hLib = LoadLibrary(filePath);
            _trace.writeToLog(1, "Registrar: After LoadLibrary.");
            if (IntPtr.Zero == hLib)
            {
                _trace.writeToLog(1, "Registrar: Error from LoadLibrary.");
                int errno = Marshal.GetLastWin32Error();
                throw new Exception(String.Format("Registrar: Error from LoadLibrary: {0}.", errno));
            }
            _trace.writeToLog(1, "Registrar: LoadLibrary successful.");
        }

        public void RegisterComDLL()
        {
            CallPointerMethod("DllRegisterServer");
        }

        public void UnRegisterComDLL()
        {
            CallPointerMethod("DllUnregisterServer");
        }

        private void CallPointerMethod(string methodName)
        {
            _trace.writeToLog(1, String.Format("Registrar: Call GetProcAddress for method: <{0}>.", methodName));
            IntPtr dllEntryPoint = GetProcAddress(hLib, methodName);
            _trace.writeToLog(1, "Registrar: Back from GetProcAddress.");
            if (IntPtr.Zero == dllEntryPoint)
            {
                _trace.writeToLog(1, "Registrar: Error from GetProcAddress.");
                throw new Exception(String.Format("Registrar: Error from GetProcAddress for DLL. Error: {0}.", Marshal.GetLastWin32Error()));
            }
            _trace.writeToLog(1, "Registrar: Get the DLL function pointer.");
            PointerToMethodInvoker drs =
                   (PointerToMethodInvoker)Marshal.GetDelegateForFunctionPointer(dllEntryPoint,
                               typeof(PointerToMethodInvoker));
            _trace.writeToLog(1, "Registrar: Call the DLL method.");
            drs();
            _trace.writeToLog(1, "Registrar: Back from the DLL method.");
        }

        public void Dispose()
        {
            _trace.writeToLog(1, "Registrar: Dispose Entry.");
            if (IntPtr.Zero != hLib)
            {
                //UnRegisterComDLL();    // leave it registered
                _trace.writeToLog(1, "Registrar: Free the DLL.");
                FreeLibrary(hLib);
                hLib = IntPtr.Zero;
            }
        }
    }

    /// <summary>
    /// Executable to handle COM DLL registration and unregistration during InstallShield installation and uninstallation.
    /// InstallShield calls this as a custom action after installation, and with different parameters after uninstallation.
    /// Note that InstallShield deletes all of the files before calling this executable after the uninstall is complete.
    /// Parameters:
    ///   o After installation:
    ///     RegisterCom <path to the Cloud installation directory in Program Files>
    ///   o After uninstall:
    ///     RegisterCom /u
    /// </summary>

    public static class RegisterCom
    {
        private static CLTrace _trace = CLTrace.Instance;

        static int Main(string[] args)
        {
            bool wasExplorerStopped = false;
            string explorerLocation = null;

            try
            {
                _trace.writeToLog(1, "RegisterCom: Main program starting.");
                _trace.writeToLog(1, String.Format("RegisterCom: Arg count: {0}.", args.Length));

                //TODO: Always pin the systray icon to the taskbar.  This is debug code.
                //bool rcDebug = AlwaysShowNotifyIcon(WhenToShow: 16);

                if (args.Length == 0)
                {
                    _trace.writeToLog(1, String.Format("RegisterCom: ERROR. No args.  Exit."));
                    return 1;
                }

                string firstArg = args[0];

                int firstDoubleQuote = firstArg.IndexOf('\"');
                if (firstDoubleQuote > 0)
                {
                    int secondDoubleQuote = firstArg.LastIndexOf('\"');
                    if (secondDoubleQuote != firstDoubleQuote)
                    {
                        firstArg = firstArg.Substring(firstDoubleQuote, secondDoubleQuote - firstDoubleQuote - 1);
                    }
                    else if (firstDoubleQuote != firstArg.Length - 1)
                    {
                        firstArg = firstArg.Substring(firstDoubleQuote + 1);
                    }
                    else
                    {
                        firstArg = firstArg.Substring(0, firstArg.Length - 1);
                    }
                }

                _trace.writeToLog(1, String.Format("RegisterCom: First Arg: <{0}>.", firstArg));

                // Check for the uninstall option
                if (args.Length > 0 && firstArg != null && firstArg.Equals("/u", StringComparison.InvariantCultureIgnoreCase))
                {
                    // This is uninstall
                    _trace.writeToLog(1, "RegisterCom: Call UninstallCOM.");
                    int rc = UninstallCOM();
                    return rc;
                }

                // Installation.  The first parm should point to the Cloud program files directory.
                if (args.Length == 0 || firstArg == null)
                {
                    // No arguments.
                    _trace.writeToLog(1, "RegisterCom: Main: ERROR.  No arguments.");
                    return 2;
                }

                // Determine whether 32-bit or 64-bit architecture
                string bitness;
                if (IntPtr.Size == 4)
                {
                    // 32-bit 
                    bitness = "x86";
                }
                else
                {
                    // 64-bit 
                    bitness = "amd64";
                }

                // See if BadgeCOM exists at that path.
                string pathBadgeCOM = Path.Combine(firstArg, bitness + "\\BadgeCOM.dll");
                _trace.writeToLog(1, String.Format("RegisterCom: Main: Source path of BadgeCOM.dll: <{0}>.", pathBadgeCOM));
                if (!File.Exists(pathBadgeCOM))
                {
                    _trace.writeToLog(1, string.Format("RegisterCom: Main: ERROR.  Could not find BadgeCOM.dll at path {0}.", pathBadgeCOM));
                    return 3;
                }

                // Stop Explorer
                _trace.writeToLog(1, "RegisterCom: Main: Stop Explorer");
                explorerLocation = StopExplorer();
                wasExplorerStopped = true;


                // Copy some files that will not be automatically uninstalled.  These files are needed for uninstall.  The uninstall
                // process will delete them.
                int rcLocal = CopyFilesNeededForUninstall();
                if (rcLocal != 0)
                {
                    _trace.writeToLog(1, String.Format("RegisterCom: Main: ERROR: From CopyFilesNeededForUninstall: rc: {0}.", rcLocal));
                    return rcLocal;
                }

                // Copy the VC100 files only for 64-bit systems
                if (IntPtr.Size == 8)
                {
                    // Copy msvc100.dll
                    string pathSystem32Msvcp100 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msvcp100.dll");
                    string pathMsvcp100 = Path.Combine(firstArg, "msvcp100Copy.dll");
                    try
                    {
                        if (!File.Exists(pathSystem32Msvcp100))
                        {
                            File.Copy(pathMsvcp100, pathSystem32Msvcp100);
                        }
                    }
                    catch (Exception ex)
                    {
                        _trace.writeToLog(1, String.Format("RegisterCom: Main: ERROR: Exception(3).  Msg: {0}.", ex.Message));

                        // Start Explorer
                        _trace.writeToLog(1, "RegisterCom: Main: Start Explorer");
                        Process.Start(explorerLocation);
                        return 6;
                    }

                    // Copy msvcr100.dll
                    string pathSystem32Msvcr100 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msvcr100.dll");
                    string pathMsvcr100 = Path.Combine(firstArg, "msvcr100Copy.dll");
                    try
                    {
                        if (!File.Exists(pathSystem32Msvcr100))
                        {
                            File.Copy(pathMsvcr100, pathSystem32Msvcr100);
                        }
                    }
                    catch (Exception ex)
                    {
                        _trace.writeToLog(1, String.Format("RegisterCom: Main: ERROR: Exception(4).  Msg: {0}.", ex.Message));

                        // Start Explorer
                        _trace.writeToLog(1, "RegisterCom: Main: Start Explorer");
                        Process.Start(explorerLocation);
                        return 7;
                    }

                    // Copy atl100.dll
                    string pathSystem32Atl100 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "atl100.dll");
                    string pathAtl100 = Path.Combine(firstArg, "atl100Copy.dll");
                    try
                    {
                        if (!File.Exists(pathSystem32Atl100))
                        {
                            File.Copy(pathAtl100, pathSystem32Atl100);
                        }
                    }
                    catch (Exception ex)
                    {
                        _trace.writeToLog(1, String.Format("RegisterCom: Main: ERROR: Exception(5).  Msg: {0}.", ex.Message));

                        // Start Explorer
                        _trace.writeToLog(1, "RegisterCom: Main: Start Explorer");
                        Process.Start(explorerLocation);
                        return 8;
                    }
                }

                // Register BadgeCOM.dll in the ProgramFiles CommonFiles folder.
                string pathUninstallFiles = CLShortcuts.Get64BitCommonProgramFilesFolderPath() + CLPrivateDefinitions.CloudFolderInProgramFilesCommon + "\\BadgeCOM.dll";

                _trace.writeToLog(1, String.Format("RegisterCom: Call RegisterAssembly. Path: <{0}>.", pathUninstallFiles));
                rcLocal = RegisterAssembly(pathUninstallFiles);
                if (rcLocal != 0)
                {
                    _trace.writeToLog(1, String.Format("RegisterCom: ERROR: From RegisterAssembly. rc: {0}.", rcLocal));
                }

                _trace.writeToLog(1, String.Format("RegisterCom: Main: Installation exit.  rc: {0}.", rcLocal));
                return rcLocal;
            }
            catch (Exception ex)
            {
                StringBuilder exBuilder = new StringBuilder("RegisterCom: Main: ERROR: Outer exception: ");
                int tabCount = 0;

                Exception currentEx = ex;

                while (currentEx != null)
                {
                    if (tabCount != 0)
                    {
                        exBuilder.Append(Environment.NewLine + GenerateTab(tabCount));
                    }
                    exBuilder.Append(currentEx.Message);

                    tabCount++;

                    currentEx = currentEx.InnerException;
                }

                _trace.writeToLog(1, exBuilder.ToString());

                throw;
            }
            finally
            {
                // Start Explorer
                if (wasExplorerStopped)
                {
                    _trace.writeToLog(1, "RegisterCom: Main: Start Explorer");
                    Process.Start(explorerLocation);
                }
            }
        }

        /// <summary>
        /// This function will copy the files needed for uninstall.  The copies will be as follows:
        ///                                     From                                            To
        /// 32-bit systems:
        ///   - BadgeCom.dll                    Program Files\Cloud.Com\Cloud\x86               Program Files\Common Files\Cloud.Com\Cloud
        ///   - RegisterCom.exe                 Program Files\Cloud.Com\Cloud                   Program Files\Common Files\Cloud.Com\Cloud
        ///   - CloudApiPrivate.dll             Program Files\Cloud.Com\Cloud                   Program Files\Common Files\Cloud.Com\Cloud
        ///   - CloudApiPublic.dll              Program Files\Cloud.Com\Cloud                   Program Files\Common Files\Cloud.Com\Cloud
        ///   - Microsoft.Net.Http.dll          Program Files\Cloud.Com\Cloud                   Program Files\Common Files\Cloud.Com\Cloud
        /// 64-bit systems:
        ///   - BadgeCom.dll                    Program Files (x86)\Cloud.Com\Cloud\amd64       Program Files\Common Files\Cloud.Com\Cloud
        ///   - BadgeCom.dll                    Program Files (x86)\Cloud.Com\Cloud\x86         Program Files (x86)\Common Files\Cloud.Com\Cloud
        ///   - RegisterCom.exe                 Program Files (x86)\Cloud.Com\Cloud             Program Files (x86)\Common Files\Cloud.Com\Cloud
        ///   - CloudApiPrivate.dll             Program Files (x86)\Cloud.Com\Cloud             Program Files (x86)\Common Files\Cloud.Com\Cloud
        ///   - CloudApiPublic.dll              Program Files (x86)\Cloud.Com\Cloud             Program Files (x86)\Common Files\Cloud.Com\Cloud
        ///   - Microsoft.Net.Http.dll          Program Files (x86)\Cloud.Com\Cloud             Program Files (x86)\Common Files\Cloud.Com\Cloud
        ///   
        /// The ProgramFiles 32-bit functions used below will identify the following directories:
        /// 32-bit systems:
        ///   - Program Files\
        /// 64-bit systems:
        ///   - Program Files (x86)\
        /// The ProgramFiles 64-bit functions used below will identify the following directories:
        /// 32-bit systems:
        ///   - Program Files\
        /// 64-bit systems:
        ///   - Program Files\
        /// 
        /// </summary>
        /// <returns></returns>
        private static int CopyFilesNeededForUninstall()
        {
            // Determine the directories to use.
            string fromDirectory = CLShortcuts.Get32BitProgramFilesFolderPath() + CLPrivateDefinitions.CloudFolderInProgramFiles;
            string to32BitDirectory = CLShortcuts.Get32BitCommonProgramFilesFolderPath() + CLPrivateDefinitions.CloudFolderInProgramFilesCommon;
            string to64BitDirectory = CLShortcuts.Get64BitCommonProgramFilesFolderPath() + CLPrivateDefinitions.CloudFolderInProgramFilesCommon;

            try 
        	{
                // Make the directories if they don't already exist.
                _trace.writeToLog(1, String.Format("RegisterCom: CopyFilesNeededForUninstall: Entry. fromDirectory: <{0}>. to32BitDirectory: <{1}>. to64BitDirectory: <{2}>.",
                            fromDirectory, to32BitDirectory, to64BitDirectory));
                Directory.CreateDirectory(to32BitDirectory);
                Directory.CreateDirectory(to64BitDirectory);

                // Copy the files
                if (IntPtr.Size == 4)
                {
                    // 32-bit 
                    _trace.writeToLog(1, "RegisterCom: CopyFilesNeededForUninstall: Copy 32-bit BadgeCom.dll.");
                    CopyFileWithDeleteFirst(fromDirectory + "\\x86", to32BitDirectory, "BadgeCOM.dll");
                }
                else
                {
                    // 64-bit 
                    _trace.writeToLog(1, "RegisterCom: CopyFilesNeededForUninstall: Copy 64-bit and 32-bit BadgeCom.dll.");
                    CopyFileWithDeleteFirst(fromDirectory + "\\x86", to32BitDirectory, "BadgeCOM.dll");
                    CopyFileWithDeleteFirst(fromDirectory + "\\amd64", to64BitDirectory, "BadgeCOM.dll");
                }

                // Copy the AnyCpu files
                CopyFileWithDeleteFirst(fromDirectory, to32BitDirectory, "RegisterCom.exe");
                CopyFileWithDeleteFirst(fromDirectory, to32BitDirectory, "CloudApiPrivate.dll");
                CopyFileWithDeleteFirst(fromDirectory, to32BitDirectory, "CloudApiPublic.dll");
                CopyFileWithDeleteFirst(fromDirectory, to32BitDirectory, "Microsoft.Net.Http.dll");
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("RegisterCom: Main: ERROR: Exception.  Msg: {0}.", ex.Message));
                return 200;
            }

            return 0;
        }

        /// <summary>
        /// Copy a file from one directory to another.  Delete the target first if it exists.
        /// </summary>
        /// <param name="fromDirectory"></param>
        /// <param name="toDirectory"></param>
        /// <param name="filenameExt"></param>
        private static void CopyFileWithDeleteFirst(string fromDirectory, string toDirectory, string filenameExt)
        {
            try
            {
                // Build the paths
                string fromPath = fromDirectory + "\\" + filenameExt;
                string toPath = toDirectory + "\\" + filenameExt;

                // Found BadgeCOM.dll.  We will copy it to System32 as CloudBadgeCom.dll.  Delete the target first
                if (File.Exists(toPath))
                {
                    _trace.writeToLog(1, String.Format("RegisterCom: CopyFileWithDeleteFirst: Delete existing file <{0}>.", toPath));
                    File.Delete(toPath);
                }

                _trace.writeToLog(1, String.Format("RegisterCom: CopyFileWithDeleteFirst: Copy file {0} to {1}.", fromPath, toPath));
                File.Copy(fromPath, toPath);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("RegisterCom: CopyFileWithDeleteFirst: ERROR: Exception.  Msg: {0}.", ex.Message));
                throw;
            }
        }

        private static string GenerateTab(int tabCount)
        {
            return new string(Enumerable.Range(0, tabCount).Select(parent => ' ').ToArray());
        }

        /// <summary>
        /// Stop Explorer
        /// </summary>
        /// <returns>string: The path to the Explorer.exe file.</returns>
        private static string StopExplorer()
        {
            string explorerLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            try
            {
                // Kill Explorer
                _trace.writeToLog(1, String.Format("RegisterCom: StopExplorer: Entry. Explorer location: <{0}>.", explorerLocation));
                ProcessStartInfo taskKillInfo = new ProcessStartInfo();
                taskKillInfo.CreateNoWindow = true;
                taskKillInfo.UseShellExecute = false;
                taskKillInfo.FileName = "cmd.exe";
                taskKillInfo.WindowStyle = ProcessWindowStyle.Hidden;
                taskKillInfo.Arguments = "/C taskkill /F /IM explorer.exe";
                _trace.writeToLog(1, "RegisterCom: StopExplorer: Start the command.");
                Process.Start(taskKillInfo);

                // Wait for all Explorer processes to stop.
                const int maxProcessWaits = 40; // corresponds to trying for 20 seconds (if each iteration waits 500 milliseconds)
                for (int waitCounter = 0; waitCounter < maxProcessWaits; waitCounter++)
                {
                    // For some reason this won't work unless we wait here for a bit.
                    Thread.Sleep(500);
                    if (!IsExplorerRunning(explorerLocation))
                    {
                        _trace.writeToLog(1, "RegisterCom: StopExplorer: Explorer is not running.  Break.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("RegisterCom: StopExplorer: ERROR: Exception: Msg: <{0}.", ex.Message));
            }
            _trace.writeToLog(1, String.Format("RegisterCom: StopExplorer: Return. explorerLocation: <{0}>.", explorerLocation));
            return explorerLocation;
        }

        /// <summary>
        /// Uninstall function.
        /// </summary>
        /// <returns></returns>
        private static int UninstallCOM()
        {
            string explorerLocation = null;
            _trace.writeToLog(1, "RegisterCom: UninstallCOM: Entry.");
            try
            {
                // Stop Explorer
                _trace.writeToLog(1, "RegisterCom: UninstallCOM: Stop Explorer");
                explorerLocation = StopExplorer();

                // The BadgeCOM.dll was registered in the ProgramFiles CommonFiles directory.  Find it there and unregister it.
                string pathToCopiedBadgeCOM = CLShortcuts.Get64BitCommonProgramFilesFolderPath() + CLPrivateDefinitions.CloudFolderInProgramFilesCommon + "\\BadgeCOM.dll";
                if (File.Exists(pathToCopiedBadgeCOM))
                {
                    // Unregister BadgeCOM
                    _trace.writeToLog(1, String.Format("RegisterCom: UninstallCOM: BadgeCOM exists at path <{0}>.  Unregister it.", pathToCopiedBadgeCOM));
                    UnregisterAssembly(pathToCopiedBadgeCOM);

                }
                else
                {
                    _trace.writeToLog(1, String.Format("RegisterCom: UninstallCOM: ERROR.  BadgeCOM.dll not found at path {0}.", pathToCopiedBadgeCOM));
                }

                // Remove all of the Cloud folder shortcuts
                _trace.writeToLog(1, "RegisterCom: UninstallCOM: Remove Cloud folder shortcuts.");
                CLShortcuts.RemoveCloudFolderShortcuts(Settings.Instance.CloudFolderPath);

                // Remotely unlink this computer from the account.
                if (!String.IsNullOrEmpty(Settings.Instance.Akey))
                {
                    CLError error = null;
                    _trace.writeToLog(1, "RegisterCom: UninstallCOM: Remotely unlink this device.");
                    CLRegistration registration = new CLRegistration();
                    registration.UnlinkDeviceWithAccessKey(Settings.Instance.Akey, out error);
                    if (error != null)
                    {
                        _trace.writeToLog(1, String.Format("RegisterCom: UninstallCOM: ERROR: Remotely unlinking. Msg: <{0}>. Code: {1}>.", error.errorDescription, error.errorCode));
                    }
                }

                // Clear the settings.
                _trace.writeToLog(1, "RegisterCom: UninstallCOM: Clear settings.");
                Settings.Instance.resetSettings();

                // Delete the database file to force a re-index at the next start.
                string indexDBLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + CLPrivateDefinitions.CloudIndexDatabaseLocation;
                _trace.writeToLog(1, String.Format("RegisterCom: UninstallCOM: Index DB location: <{0}>.", indexDBLocation));
                if (File.Exists(indexDBLocation))
                {
                    _trace.writeToLog(1, "RegisterCom: UninstallCOM: Delete the index DB file.");
                    File.Delete(indexDBLocation);
                }

                // Finalize the uninstall.  We are running in this assembly, and this assembly has various DLLs loaded and locked, so we can't
                // just delete them.  We would like to delete all of the files recursively up to c:\program files (x86)\Cloud.com (including Cloud.com)),
                // assuming that the user hasn't added any files that we or the installer don't know about.  We will save a VBScript file in the user's
                // temp directory.  We will start a new process naming cscript and the VBScript file.  The VBScript file will unregister BadgeCom, clean
                // up the program files directory, and then delete itself.  We will just exit here so the files will be unlocked so they can
                // be cleaned up.  Under normal circumstances, the entire ProgramFiles Cloud.com directory should be removed.  The VBScript program will
                // restart Explorer.
                _trace.writeToLog(1, "RegisterCom: UninstallCOM: Call FinalizeUninstall.");
                int rc = FinalizeUninstall();
                if (rc != 0)
                {
                    // Restart Explorer
                    _trace.writeToLog(1, "RegisterCom: UninstallCOM: Start Explorer.");
                    Process.Start(explorerLocation);
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("RegisterCom: UninstallCOM: ERROR.  Exception.  Msg: {0}.", ex.Message));

                // Restart Explorer
                _trace.writeToLog(1, "RegisterCom: UninstallCOM: Start Explorer.");
                Process.Start(explorerLocation);

                return 105;
            }

            _trace.writeToLog(1, "RegisterCom: UninstallCOM: Exit successfully.");
            return 0;
        }

        /// <summary>
        /// Finalize the uninstall
        /// </summary>
        private static int FinalizeUninstall()
        {
            try
            {
                // Stream the CloudClean.vbs file out to the user's temp directory
                // Locate the user's temp directory.
                _trace.writeToLog(1, "RegisterCom: FinalizeUninstall: Entry.");
                string userTempDirectory = Path.GetTempPath();
                string vbsPath = userTempDirectory + "\\CloudClean.vbs";

                // Get the assembly containing the .vbs resource.
                _trace.writeToLog(1, "RegisterCom: FinalizeUninstall: Get the assembly containing the .vbs resource.");
                System.Reflection.Assembly storeAssembly = System.Reflection.Assembly.GetAssembly(typeof(global::RegisterCom.RegisterCom));
                if (storeAssembly == null)
                {
                    _trace.writeToLog(1, "RegisterCom: FinalizeUninstall: ERROR: storeAssembly null.");
                    return 1;
                }

                // Stream the CloudClean.vbs file out to the temp directory
                _trace.writeToLog(1, "RegisterCom: Call WriteResourceFileToFilesystemFile.");
                int rc = CLShortcuts.WriteResourceFileToFilesystemFile(storeAssembly, "CloudCleanVbs", vbsPath);
                if (rc != 0)
                {
                    _trace.writeToLog(1, String.Format("RegisterCom: FinalizeUninstall: ERROR: From WriteResourceFileToFilesystemFile. rc: {0}.", rc + 100));
                    return rc + 100;
                }
                
                // Now we will create a new process to run the VBScript file.
                _trace.writeToLog(1, "RegisterCom: FinalizeUninstall: Build the paths for launching the VBScript file.");
                string systemFolderPath = CLShortcuts.Get32BitSystemFolderPath();
                string cscriptPath = systemFolderPath + "\\cscript.exe";
                _trace.writeToLog(1, String.Format("RegisterCom: FinalizeUninstall: Cscript executable path: <{0}>.", cscriptPath));

                string parm1Path = CLShortcuts.Get32BitProgramFilesFolderPath();
                _trace.writeToLog(1, String.Format("RegisterCom: FinalizeUninstall: Parm 1: <{0}>.", parm1Path));

                string parm2Path = CLShortcuts.Get64BitProgramFilesFolderPath();
                _trace.writeToLog(1, String.Format("RegisterCom: FinalizeUninstall: Parm 2: <{0}>.", parm2Path));

                string parm3Path = Environment.GetEnvironmentVariable("SystemRoot");
                _trace.writeToLog(1, String.Format("RegisterCom: FinalizeUninstall: Parm 3: <{0}>.", parm3Path));

                string argumentsString = @" //B //T:30 //Nologo """ + vbsPath + @"""" + @" """ + parm1Path + @""" """ + parm2Path + @""" """ + parm3Path + @"""";
                _trace.writeToLog(1, String.Format("RegisterCom: FinalizeUninstall: Launch the VBScript file.  Launch: <{0}>.", argumentsString));
            
                // Launch the process
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = cscriptPath;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = argumentsString;
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("RegisterCom: FinalizeUninstall: ERROR: Exception. Msg: {0}.", ex.Message));
                return 4;
            }

            _trace.writeToLog(1, "RegisterCom: FinalizeUninstall: Exit successfully.");
            return 0;
        }

        private static bool IsExplorerRunning(string explorerLocation)
        {
            bool isExplorerRunning = false;         // assume not running

            try
            {
                _trace.writeToLog(1, String.Format("RegisterCom: IsExplorerRunning: Entry. explorerLocation: <{0}>.", explorerLocation));
                string wmiQueryString = "SELECT ProcessId, ExecutablePath FROM Win32_Process";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQueryString))
                {
                    if (searcher != null)
                    {
                        _trace.writeToLog(1, "RegisterCom: IsExplorerRunning: searcher not null. Get the results.");
                        using (ManagementObjectCollection results = searcher.Get())
                        {
                            _trace.writeToLog(1, "RegisterCom: IsExplorerRunning: Run the query.");
                            isExplorerRunning = Process.GetProcesses()
                                .Where(parent => parent.ProcessName.Equals("explorer", StringComparison.InvariantCultureIgnoreCase))
                                .Join(results.Cast<ManagementObject>(),
                                    parent => parent.Id,
                                    parent => (int)(uint)parent["ProcessId"],
                                    (outer, inner) => new ProcessWithPath(outer, (string)inner["ExecutablePath"]))
                                .Any(parent => parent.Path.Equals(explorerLocation, StringComparison.InvariantCultureIgnoreCase));
                        }
                    }
                    else
                    {
                        // searcher is null.
                        _trace.writeToLog(1, "RegisterCom: IsExplorerRunning: ERROR: searcher is null.");
                        return isExplorerRunning;           // assume Explorer is not running.
                    }
                }
            }
            catch (Exception ex)
            {
                _trace.writeToLog(1, String.Format("RegisterCom: IsExplorerRunning: ERROR: Exception: Msg: <{0}>.", ex.Message));
            }

            return isExplorerRunning;
        }

        private class ProcessWithPath
        {
            public Process Process { get; private set; }
            public string Path { get; private set; }

            public ProcessWithPath(Process process, string path)
            {
                this.Process = process;
                this.Path = path;
            }

            public override string ToString()
            {
                return (this.Process == null
                    ? "null"
                    : this.Process.ProcessName);
            }
        }

        private static int RegisterAssembly(string dllPath)
        {
            int rc = 0;

            if (File.Exists(dllPath))
            {
                try
                {
                    _trace.writeToLog(1, String.Format("RegisterCom: RegisterAssembly: Use Registrar to register the DLL at <{0}>.", dllPath));
                    using (Registrar registrar = new Registrar(dllPath))
                    {
                        _trace.writeToLog(1, "RegisterCom: Call Registrar.");
                        registrar.RegisterComDLL();
                    }
                    _trace.writeToLog(1, "RegisterCom: RegisterAssembly: Finished registering.");
                }
                catch (Exception ex)
                {
                    _trace.writeToLog(1, String.Format("RegisterCom: RegisterAssembly: ERROR.  Exception.  Msg: <{0}>.", ex.Message));
                    rc = 2;
                }
            }
            else
            {
                _trace.writeToLog(1, String.Format("RegisterCom: RegisterAssembly: ERROR.  Could not find file <{0}>.", dllPath));
                rc = 1;
            }

            return rc;
        }

        private static void UnregisterAssembly(string dllPath)
        {

            if (File.Exists(dllPath))
            {
                try
                {
                    _trace.writeToLog(1, String.Format("RegisterCom: UnregisterAssembly: Use Registrar to unregister the DLL at <{0}>.", dllPath));
                    using (Registrar registrar = new Registrar(dllPath))
                    {
                        _trace.writeToLog(1, "RegisterCom: UnregisterAssembly: Call Registrar.");
                        registrar.UnRegisterComDLL();
                    }
                    _trace.writeToLog(1, "RegisterCom: UnregisterAssembly: Finished unregistering.");
                }
                catch (Exception ex)
                {
                    _trace.writeToLog(1, String.Format("RegisterCom: UnregisterAssembly: ERROR.  Exception.  Msg: <{0}>.", ex.Message));
                }
            }
            else
            {
                _trace.writeToLog(1, String.Format("RegisterCom: UnregisterAssembly: ERROR.  Could not find file <{0}>.", dllPath));
            }
        }


        /// <summary>
        /// Always show the Cloud icon on the taskbar, rather than up in the pop-up icon list.
        /// Searches for a notify icon by application path in the registry and updates the key to always show, always hide, or hide when inactive
        /// WhenToShow should be 16 (Dec) for always (verified), 17 (dec) for never (I'm guessing on this), and 18 (Dec) for hide when inactive (verified).
        /// This will return success status.  Highly suggest putting a local setting variable in to only run this once per machine....
        /// </summary>
        /// <param name="WhenToShow">16: Always show. 17: Never.  18: Hide when inactive.</param>
        /// <returns>bool: true: success.</returns>
#if TRASH
        private static bool AlwaysShowNotifyIcon(byte WhenToShow)
        {
            string myHolderString = null;
            System.Text.UTF8Encoding encText = new System.Text.UTF8Encoding();
            try
            {
                // Get our registry entry
                byte[] myRegistryKeyAsByte = null;
                string myRegistryKeyAsString = "";
                RegistryKey myKey = null;

                try
                {
                    // @@@@@@@@@@@  Debug only
                    myKey = Registry.Users.OpenSubKey("S-1-5-21-169676751-141520382-2068143436-1000_Classes\\Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\TrayNotify");
                    if (myKey == null)
                    {
                        return false;
                    }
                    WriteBinaryDataForRegKey(myKey, "IconStreams", "Users");
                    WriteBinaryDataForRegKey(myKey, "PastIconsStream", "Users");

                    myKey = Registry.CurrentUser.OpenSubKey("Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\TrayNotify");
                    if (myKey == null)
                    {
                        return false;
                    }
                    WriteBinaryDataForRegKey(myKey, "IconStreams", "CurrentUser");
                    WriteBinaryDataForRegKey(myKey, "PastIconsStream", "CurrentUsers");


                    //RegistryKey myKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\TrayNotify");
                    myKey = Registry.CurrentUser.OpenSubKey("Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\TrayNotify");

                    // Read the data
                    myRegistryKeyAsByte = (byte[])myKey.GetValue("IconStreams", new Byte[0]);

                    // @@@@@@@@ Debug only
                    FileStream outFile = new FileStream("c:\\trash\\NotifyIcon\\IconStreamsByteContent.bin", FileMode.Create);
                    outFile.Write(myRegistryKeyAsByte, 0, myRegistryKeyAsByte.Length);
                    outFile.Close();

                    // @@@@@@@@ Debug only
                    // Use rot-13 decryption and decrypt every byte that can be decrypted, and dump it again.
                    byte[] myRegistryKeyAsByteCopy = new byte[myRegistryKeyAsByte.Length];
                    myRegistryKeyAsByte.CopyTo(myRegistryKeyAsByteCopy, 0);  // make a copy

                    Rot13DecodeInPlace(ref myRegistryKeyAsByteCopy);  // decode in place

                    outFile = new FileStream("c:\\trash\\NotifyIcon\\IconStreamsByteContentTranslated.bin", FileMode.Create);
                    outFile.Write(myRegistryKeyAsByteCopy, 0, myRegistryKeyAsByteCopy.Length);
                    outFile.Close();



                    // Convert the bytes to a string of hex values
                    for (int i = 0; i < myRegistryKeyAsByte.Length; i++)
                    {
                        myHolderString = myRegistryKeyAsByte[i].ToString("X2");
                        myRegistryKeyAsString += myHolderString;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                // Get our application path including just the filename.
                byte[] myTempAppPathAsByte = null;
                myTempAppPathAsByte = encText.GetBytes(CLShortcuts.GetProgramFilesFolderPath() + CLPrivateDefinitions.CloudFolderInProgramFiles + "\\" + CLPrivateDefinitions.CloudAppName);
                byte[] myAppPathAsByte = new byte[myTempAppPathAsByte.Length * 2];
                string myAppPathAsString = "";
                try
                {
                    // Add in zeros for every other byte like the registry key has
                    for (int i = 0; i < myAppPathAsByte.Length - 1; i++)
                    {
                        if (i % 2 == 0)
                        {
                            myAppPathAsByte[i] = myTempAppPathAsByte[Convert.ToInt32(i / 2)];
                        }
                        else
                        {
                            myAppPathAsByte[i] = 0;
                        }
                    }
                    // Convert the bytes to a string of hex values
                    for (int i = 0; i < myAppPathAsByte.Length; i++)
                    {
                        myHolderString = myAppPathAsByte[i].ToString("X2");
                        myAppPathAsString += myHolderString;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                // Hunt for the application path inside the registry key
                long myPosition = myRegistryKeyAsString.IndexOf(myAppPathAsString) - 1;

                if (myPosition > 0)
                {
                    // We found our startup path so make our change to the byte 20 before the start of the path
                    // I believe this is the right byte to change from manually setting a icon.  I exported out the TrayIcon 
                    // key, changed the value, rebooted, and expored it out again and this byte was the only thing to change
                    myRegistryKeyAsByte[Convert.ToInt32(myPosition / 2 - 20)] = WhenToShow;

                    // Write the modified key back to the registry
                    myKey.SetValue("IconStreams", myRegistryKeyAsByte);

                    //// Now crash explorer.  Thats right....explorer keeps this information in memory and reads it at startup and writes it at shutdown
                    //// so the only way to actaully change these values is to write to the registry then crash explorer so it can't overwrite what
                    //// we did.  It will then poll our information when it starts back up.  First look for explorer in memory:
                    //Process ExplorerProcess = null;
                    //foreach (Process p in Process.GetProcesses())
                    //{
                    //    if (p.ProcessName.ToString() == "explorer")
                    //    {
                    //        ExplorerProcess = p;
                    //        break; // TODO: might not be correct. Was : Exit For
                    //    }
                    //    else
                    //    {
                    //        ExplorerProcess = null;
                    //    }
                    //}
                    //// If we found it then we kill it, it will restart itself
                    //if ((ExplorerProcess != null))
                    //{
                    //    ExplorerProcess.Kill();
                    //    System.Threading.Thread.Sleep(2000);
                    //}

                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }

        }

        private static void WriteBinaryDataForRegKey(RegistryKey myKey, string subKey, string filePrefix)
        {
            byte[] myRegistryKeyAsByte = null;

            // Read the data
            myRegistryKeyAsByte = (byte[])myKey.GetValue(subKey, new Byte[0]);

            // Use rot-13 decryption and decrypt every byte that can be decrypted, and dump it again.
            byte[] myRegistryKeyAsByteCopy = new byte[myRegistryKeyAsByte.Length];
            myRegistryKeyAsByte.CopyTo(myRegistryKeyAsByteCopy, 0);  // make a copy

            Rot13DecodeInPlace(ref myRegistryKeyAsByteCopy);  // decode in place

            FileStream outFile = new FileStream("c:\\trash\\NotifyIcon\\" + filePrefix + "_" + subKey + ".bin", FileMode.Create);
            outFile.Write(myRegistryKeyAsByteCopy, 0, myRegistryKeyAsByteCopy.Length);
            outFile.Close();
        }

        private static void Rot13DecodeInPlace(ref byte[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                byte b = a[i];
                char c = (char)b;
                if (c >= 'A' && c <= 'M')
                    a[i] = (byte)(b + 13);
                else if (c >= 'N' && c <= 'Z')
                    a[i] = (byte)(b - 13);
                else if (c >= 'a' && c <= 'm')
                    a[i] = (byte)(b + 13);
                else if (c >= 'n' && c <= 'z')
                    a[i] = (byte)(b - 13);
                else a[i] = b;
            }
        } 
#endif // TRASH
    }
}
