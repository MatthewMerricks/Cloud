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

namespace RegisterCom
{
    public class Registrar : IDisposable
    {
        private IntPtr hLib;

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        internal delegate int PointerToMethodInvoker();

        public Registrar(string filePath)
        {
            Trace.WriteLine(String.Format("Registrar: LoadLibrary at path <{0}>.", filePath));
            hLib = LoadLibrary(filePath);
            Trace.WriteLine("Registrar: After LoadLibrary.");
            if (IntPtr.Zero == hLib)
            {
                Trace.WriteLine("Registrar: Error from LoadLibrary.");
                int errno = Marshal.GetLastWin32Error();
                throw new Exception(String.Format("Registrar: Error from LoadLibrary: {0}.", errno));
            }
            Trace.WriteLine("Registrar: LoadLibrary successful.");
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
            Trace.WriteLine(String.Format("Registrar: Call GetProcAddress for method: <{0}>.", methodName));
            IntPtr dllEntryPoint = GetProcAddress(hLib, methodName);
            Trace.WriteLine("Registrar: Back from GetProcAddress.");
            if (IntPtr.Zero == dllEntryPoint)
            {
                Trace.WriteLine("Registrar: Error from GetProcAddress.");
                throw new Exception(String.Format("Registrar: Error from GetProcAddress for DLL. Error: {0}.", Marshal.GetLastWin32Error()));
            }
            Trace.WriteLine("Registrar: Get the DLL function pointer.");
            PointerToMethodInvoker drs =
                   (PointerToMethodInvoker)Marshal.GetDelegateForFunctionPointer(dllEntryPoint,
                               typeof(PointerToMethodInvoker));
            Trace.WriteLine("Registrar: Call the DLL method.");
            drs();
            Trace.WriteLine("Registrar: Back from the DLL method.");
        }

        public void Dispose()
        {
            Trace.WriteLine("Registrar: Dispose Entry.");
            if (IntPtr.Zero != hLib)
            {
                //UnRegisterComDLL();    // leave it registered
                Trace.WriteLine("Registrar: Free the DLL.");
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

    public static class MainProgram
    {
        public static bool shouldTerminate = false;

        static int Main(string[] args)
        {
            bool wasExplorerStopped = false;
            string explorerLocation = null;

            try
            {
                Trace.WriteLine("RegisterCom: Main program starting.");
                Trace.WriteLine(String.Format("RegisterCom: Arg count: {0}.", args.Length));

                if (args.Length == 0)
                {
                    Trace.WriteLine(String.Format("RegisterCom: ERROR. No args.  Exit."));
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

                Trace.WriteLine(String.Format("RegisterCom: First Arg: <{0}>.", firstArg));

                // Check for the uninstall option
                if (args.Length > 0 && firstArg != null && firstArg.Equals("/u", StringComparison.InvariantCultureIgnoreCase))
                {
                    // This is uninstall
                    Trace.WriteLine("RegisterCom: Call UninstallCOM.");
                    int rc = UninstallCOM();
                    return rc;
                }

                // Installation.  The first parm should point to the Cloud program files directory.
                if (args.Length == 0 || firstArg == null)
                {
                    // No arguments.
                    Trace.WriteLine("RegisterCom: Main: ERROR.  No arguments.");
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
                Trace.WriteLine(String.Format("RegisterCom: Main: Source path of BadgeCOM.dll: <{0}>.", pathBadgeCOM));
                if (!File.Exists(pathBadgeCOM))
                {
                    Trace.WriteLine(string.Format("RegisterCom: Main: ERROR.  Could not find BadgeCOM.dll at path {0}.", pathBadgeCOM));
                    return 3;
                }

                // Stop Explorer
                Trace.WriteLine("RegisterCom: Main: Stop Explorer");
                explorerLocation = StopExplorer();
                wasExplorerStopped = true;


                // Copy some files that will not be automatically uninstalled.  These files are needed for uninstall.  The uninstall
                // process will delete them.
                int rcLocal = CopyFilesNeededForUninstall();
                if (rcLocal != 0)
                {
                    Trace.WriteLine(String.Format("RegisterCom: Main: ERROR: From CopyFilesNeededForUninstall: rc: {0}.", rcLocal));
                    return rcLocal;
                }

                // Copy the VC100 files only for 64-bit systems
                if (IntPtr.Size == 8)
                {
                    // Copy msvc100.dll
                    string pathSystem32Msvcp100 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msvcp100.dll");
                    string pathMsvcp100 = Path.Combine(firstArg, "msvcp100.dll");
                    try
                    {
                        if (!File.Exists(pathSystem32Msvcp100))
                        {
                            File.Copy(pathMsvcp100, pathSystem32Msvcp100);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(String.Format("RegisterCom: Main: ERROR: Exception(3).  Msg: {0}.", ex.Message));

                        // Start Explorer
                        Trace.WriteLine("RegisterCom: Main: Start Explorer");
                        Process.Start(explorerLocation);
                        return 6;
                    }

                    // Copy msvcr100.dll
                    string pathSystem32Msvcr100 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msvcr100.dll");
                    string pathMsvcr100 = Path.Combine(firstArg, "msvcr100.dll");
                    try
                    {
                        if (!File.Exists(pathSystem32Msvcr100))
                        {
                            File.Copy(pathMsvcr100, pathSystem32Msvcr100);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(String.Format("RegisterCom: Main: ERROR: Exception(4).  Msg: {0}.", ex.Message));

                        // Start Explorer
                        Trace.WriteLine("RegisterCom: Main: Start Explorer");
                        Process.Start(explorerLocation);
                        return 7;
                    }

                    // Copy atl100.dll
                    string pathSystem32Atl100 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "atl100.dll");
                    string pathAtl100 = Path.Combine(firstArg, "atl100.dll");
                    try
                    {
                        if (!File.Exists(pathSystem32Atl100))
                        {
                            File.Copy(pathAtl100, pathSystem32Atl100);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(String.Format("RegisterCom: Main: ERROR: Exception(5).  Msg: {0}.", ex.Message));

                        // Start Explorer
                        Trace.WriteLine("RegisterCom: Main: Start Explorer");
                        Process.Start(explorerLocation);
                        return 8;
                    }
                }

                // Register BadgeCOM.dll in the CloudSupport folder.
                string pathUninstallFiles = GetProgramFilesFolderPathForBitness() + CLPrivateDefinitions.CloudFolderInProgramFiles + CLPrivateDefinitions.CloudSupportFolderInProgramFiles + "\\BadgeCOM.dll";

                Trace.WriteLine(String.Format("RegisterCom: Call RegisterAssembly. Path: <{0}>.", pathUninstallFiles));
                rcLocal = RegisterAssembly(pathUninstallFiles);
                if (rcLocal != 0)
                {
                    Trace.WriteLine(String.Format("RegisterCom: ERROR: From RegisterAssembly. rc: {0}.", rcLocal));
                }

                Trace.WriteLine(String.Format("RegisterCom: Main: Installation exit.  rc: {0}.", rcLocal));
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

                Trace.WriteLine(exBuilder.ToString());

                throw;
            }
            finally
            {
                // Start Explorer
                if (wasExplorerStopped)
                {
                    Trace.WriteLine("RegisterCom: Main: Start Explorer");
                    Process.Start(explorerLocation);
                }
            }
        }

        private static int CopyFilesNeededForUninstall()
        {
            // Build AnyCpu "from" directory
            string fromDirectory = GetProgramFilesFolderPathForBitness() + CLPrivateDefinitions.CloudFolderInProgramFiles;

            // Build bitness "from" directory
            string fromDirectoryBitness;
            if (IntPtr.Size == 4)
            {
                // 32-bit 
                fromDirectoryBitness = fromDirectory + "\\x86";
            }
            else
            {
                // 64-bit 
                fromDirectoryBitness = fromDirectory + "\\amd64";
            }


            // Build the "to" directory
            string toDirectory = fromDirectory + CLPrivateDefinitions.CloudSupportFolderInProgramFiles;

            try 
        	{
                // Create the "to" directory
                Directory.CreateDirectory(toDirectory);

                // Copy the files
                Trace.WriteLine(String.Format("RegisterCom: CopyFilesNeededForUninstall: Entry. fromDirectory: <{0}>. fromDirectoryBitness: <{1}>. toDirectory: <{2}>.", fromDirectory, fromDirectoryBitness, toDirectory));
                CopyFileWithDeleteFirst(fromDirectoryBitness, toDirectory, "BadgeCOM.dll");
                CopyFileWithDeleteFirst(fromDirectory, toDirectory, "RegisterCom.exe");
                CopyFileWithDeleteFirst(fromDirectory, toDirectory, "CloudApiPrivate.dll");
                CopyFileWithDeleteFirst(fromDirectory, toDirectory, "CloudApiPublic.dll");
                CopyFileWithDeleteFirst(fromDirectory, toDirectory, "Microsoft.Net.Http.dll");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("RegisterCom: Main: ERROR: Exception.  Msg: {0}.", ex.Message));
                return 200;
            }

            return 0;
        }

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
                    Trace.WriteLine(String.Format("RegisterCom: CopyFileWithDeleteFirst: Delete existing file <{0}>.", toPath));
                    File.Delete(toPath);
                }

                Trace.WriteLine(String.Format("RegisterCom: CopyFileWithDeleteFirst: Copy file {0} to {1}.", fromPath, toPath));
                File.Copy(fromPath, toPath);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("RegisterCom: CopyFileWithDeleteFirst: ERROR: Exception.  Msg: {0}.", ex.Message));
                throw;
            }
        }

        private static string GetProgramFilesFolderPathForBitness()
        {
            // Determine whether 32-bit or 64-bit architecture
            if (IntPtr.Size == 4)
            {
                // 32-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }
            else
            {
                // 64-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            }
        }

        private static string GetSystemFolderPathForBitness()
        {
            // Determine whether 32-bit or 64-bit architecture
            if (IntPtr.Size == 4)
            {
                // 32-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.System);
            }
            else
            {
                // 64-bit 
                return Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
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
                Trace.WriteLine(String.Format("RegisterCom: StopExplorer: Entry. Explorer location: <{0}>.", explorerLocation));
                ProcessStartInfo taskKillInfo = new ProcessStartInfo();
                taskKillInfo.CreateNoWindow = true;
                taskKillInfo.UseShellExecute = false;
                taskKillInfo.FileName = "cmd.exe";
                taskKillInfo.WindowStyle = ProcessWindowStyle.Hidden;
                taskKillInfo.Arguments = "/C taskkill /F /IM explorer.exe";
                Trace.WriteLine("RegisterCom: StopExplorer: Start the command.");
                Process.Start(taskKillInfo);

                // Wait for all Explorer processes to stop.
                const int maxProcessWaits = 40; // corresponds to trying for 20 seconds (if each iteration waits 500 milliseconds)
                for (int waitCounter = 0; waitCounter < maxProcessWaits; waitCounter++)
                {
                    // For some reason this won't work unless we wait here for a bit.
                    Thread.Sleep(500);
                    if (!IsExplorerRunning(explorerLocation))
                    {
                        Trace.WriteLine("RegisterCom: StopExplorer: Explorer is not running.  Break.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("RegisterCom: StopExplorer: ERROR: Exception: Msg: <{0}.", ex.Message));
            }
            Trace.WriteLine(String.Format("RegisterCom: StopExplorer: Return. explorerLocation: <{0}>.", explorerLocation));
            return explorerLocation;
        }

        /// <summary>
        /// Uninstall function.
        /// </summary>
        /// <returns></returns>
        private static int UninstallCOM()
        {
            string explorerLocation = null;
            Trace.WriteLine("RegisterCom: UninstallCOM: Entry.");
            try
            {
                // Stop Explorer
                Trace.WriteLine("RegisterCom: UninstallCOM: Stop Explorer");
                explorerLocation = StopExplorer();

                // The BadgeCOM.dll was registered in the Cloud program files CloudSupport directory.  Find it there and unregister it.
                string pathToCopiedBadgeCOM = GetProgramFilesFolderPathForBitness() + CLPrivateDefinitions.CloudFolderInProgramFiles + CLPrivateDefinitions.CloudSupportFolderInProgramFiles + "\\BadgeCOM.dll";
                if (File.Exists(pathToCopiedBadgeCOM))
                {
                    // Unregister BadgeCOM
                    Trace.WriteLine(String.Format("RegisterCom: UninstallCOM: BadgeCOM exists at path <{0}>.  Unregister it.", pathToCopiedBadgeCOM));
                    UnregisterAssembly(pathToCopiedBadgeCOM);

                }
                else
                {
                    Trace.WriteLine(String.Format("RegisterCom: UninstallCOM: ERROR.  BadgeCOM.dll not found at path {0}.", pathToCopiedBadgeCOM));
                }

                // Remove all of the Cloud folder shortcuts
                Trace.WriteLine("RegisterCom: UninstallCOM: Remove Cloud folder shortcuts.");
                CLShortcuts.RemoveCloudFolderShortcuts(Settings.Instance.CloudFolderPath);

                // Remotely unlink this computer from the account.
                if (!String.IsNullOrEmpty(Settings.Instance.Akey))
                {
                    CLError error = null;
                    Trace.WriteLine("RegisterCom: UninstallCOM: Remotely unlink this device.");
                    CLRegistration registration = new CLRegistration();
                    registration.UnlinkDeviceWithAccessKey(Settings.Instance.Akey, out error);
                    if (error != null)
                    {
                        Trace.WriteLine(String.Format("RegisterCom: UninstallCOM: ERROR: Remotely unlinking. Msg: <{0}>. Code: {1}>.", error.errorDescription, error.errorCode));
                    }
                }

                // Clear the settings.
                Trace.WriteLine("RegisterCom: UninstallCOM: Clear settings.");
                Settings.Instance.resetSettings();

                // Delete the database file to force a re-index at the next start.
                string indexDBLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + CLPrivateDefinitions.CloudIndexDatabaseLocation;
                Trace.WriteLine(String.Format("RegisterCom: UninstallCOM: Index DB location: <{0}>.", indexDBLocation));
                if (File.Exists(indexDBLocation))
                {
                    Trace.WriteLine("RegisterCom: UninstallCOM: Delete the index DB file.");
                    File.Delete(indexDBLocation);
                }

                // Finalize the uninstall.  We are running in this assembly, and this assembly has various DLLs loaded and locked, so we can't
                // just delete them.  We would like to delete all of the files recursively up to c:\program files (x86)\Cloud.com (including Cloud.com)),
                // assuming that the user hasn't added any files that we or the installer don't know about.  We will save a VBScript file in the user's
                // temp directory.  We will start a new process naming cscript and the VBScript file.  The VBScript file will unregister BadgeCom, clean
                // up the program files directory, and then delete itself.  We will just exit here so the files will be unlocked so they can
                // be cleaned up.  Under normal circumstances, the entire ProgramFiles Cloud.com directory should be removed.  The VBScript program will
                // restart Explorer.
                Trace.WriteLine("RegisterCom: UninstallCOM: Call FinalizeUninstall.");
                int rc = FinalizeUninstall();
                if (rc != 0)
                {
                    // Restart Explorer
                    Trace.WriteLine("RegisterCom: UninstallCOM: Start Explorer.");
                    Process.Start(explorerLocation);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("RegisterCom: UninstallCOM: ERROR.  Exception.  Msg: {0}.", ex.Message));

                // Restart Explorer
                Trace.WriteLine("RegisterCom: UninstallCOM: Start Explorer.");
                Process.Start(explorerLocation);

                return 105;
            }

            Trace.WriteLine("RegisterCom: UninstallCOM: Exit successfully.");
            return 0;
        }

        /// <summary>
        /// Finalize the uninstall
        /// </summary>
        private static int FinalizeUninstall()
        {
            try
            {
                // Locate the user's temp directory.
                Trace.WriteLine("RegisterCom: FinalizeUninstall: Entry.");
                string userTempDirectory = Path.GetTempPath();
                string vbsPath = userTempDirectory + "\\CloudClean.vbs";

                // Stream the CloudClean.vbs file out to the temp directory
                Trace.WriteLine(String.Format("RegisterCom: FinalizeUninstall: Create file <{0}>.", vbsPath));
                System.Reflection.Assembly storeAssembly = System.Reflection.Assembly.GetAssembly(typeof(RegisterCom.MainProgram));
                if (storeAssembly == null)
                {
                    Trace.WriteLine("RegisterCom: FinalizeUninstall: ERROR: storeAssembly null.");
                    return 1;
                }
                Trace.WriteLine(String.Format("RegisterCom: FinalizeUninstall: ERROR: storeAssembly.GetName(): <{0}>.", storeAssembly.GetName()));
                Trace.WriteLine(String.Format("RegisterCom: FinalizeUninstall: ERROR: storeAssembly.GetName().Name: <{0}>.", storeAssembly.GetName() != null ? storeAssembly.GetName().Name : "ERROR: Not Set!"));
                using (Stream txtStream = storeAssembly.GetManifestResourceStream(storeAssembly.GetName().Name + ".Resources.CloudCleanVbs"))
                {
                    if (txtStream == null)
                    {
                        Trace.WriteLine("RegisterCom: FinalizeUninstall: ERROR: txtStream null.");
                        return 2;
                    }
                    using (FileStream tempStream = new FileStream(vbsPath, FileMode.Create))
                    {
                        if (tempStream == null)
                        {
                            Trace.WriteLine("RegisterCom: FinalizeUninstall: ERROR: tempStream null.");
                            return 3;
                        }

                        byte[] streamBuffer = new byte[4096];
                        int readAmount;

                        while ((readAmount = txtStream.Read(streamBuffer, 0, 4096)) > 0)
                        {
                            Trace.WriteLine(String.Format("RegisterCom: FinalizeUninstall: Write {0} bytes to the .vbs file.", readAmount));
                            tempStream.Write(streamBuffer, 0, readAmount);
                        }
                        Trace.WriteLine("RegisterCom: FinalizeUninstall: Finished writing the .vbs file.");
                    }
                }

                // For some reason, Windows is dozing (WinDoze?).  The file we just wrote does not immediately appear in the
                // file system, and the process we will launch next won't find it.  Wait until we can see it in the file system.  ????
                for (int i = 0; i < 10; i++)
                {
                    if (File.Exists(vbsPath))
                    {
                        break;
                    }

                    Thread.Sleep(100);
                }

                // Now we will create a new process to run the VBScript file.
                Trace.WriteLine("RegisterCom: FinalizeUninstall: Build the paths for launching the VBScript file.");
                string systemFolderPath = GetSystemFolderPathForBitness();
                string cscriptPath = systemFolderPath + "\\cscript.exe";
                Trace.WriteLine(String.Format("RegisterCom: FinalizeUninstall: Cscript executable path: <{0}>.", cscriptPath));

                string parm1Path = GetProgramFilesFolderPathForBitness();
                Trace.WriteLine(String.Format("RegisterCom: FinalizeUninstall: Parm 1: <{0}>.", parm1Path));

                string parm2Path = Environment.GetEnvironmentVariable("SystemRoot");
                Trace.WriteLine(String.Format("RegisterCom: FinalizeUninstall: Parm 2: <{0}>.", parm2Path));

                string argumentsString = @" //B //T:30 //Nologo """ + vbsPath + @"""" + @" """ + parm1Path + @""" """ + parm2Path + @"""";
                Trace.WriteLine(String.Format("RegisterCom: FinalizeUninstall: Launch the VBScript file.  Launch: <{0}>.", argumentsString));
            
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
                Trace.WriteLine(String.Format("RegisterCom: FinalizeUninstall: ERROR: Exception. Msg: {0}.", ex.Message));
                return 4;
            }

            Trace.WriteLine("RegisterCom: FinalizeUninstall: Exit successfully.");
            return 0;
        }

        private static void DeleteFile(string supportPath, string filenameExt)
        {
            string path = supportPath + "\\" + filenameExt;
            Trace.WriteLine(String.Format("RegisterCom: DeleteFile: Entry.  Delete file at <{0}>.", path));
            File.Delete(path);
        }

        private static bool IsExplorerRunning(string explorerLocation)
        {
            bool isExplorerRunning = false;         // assume not running

            try
            {
                Trace.WriteLine(String.Format("RegisterCom: IsExplorerRunning: Entry. explorerLocation: <{0}>.", explorerLocation));
                string wmiQueryString = "SELECT ProcessId, ExecutablePath FROM Win32_Process";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQueryString))
                {
                    if (searcher != null)
                    {
                        Trace.WriteLine("RegisterCom: IsExplorerRunning: searcher not null. Get the results.");
                        using (ManagementObjectCollection results = searcher.Get())
                        {
                            Trace.WriteLine("RegisterCom: IsExplorerRunning: Run the query.");
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
                        Trace.WriteLine("RegisterCom: IsExplorerRunning: ERROR: searcher is null.");
                        return isExplorerRunning;           // assume Explorer is not running.
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("RegisterCom: IsExplorerRunning: ERROR: Exception: Msg: <{0}>.", ex.Message));
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
                    Trace.WriteLine(String.Format("RegisterCom: RegisterAssembly: Use Registrar to register the DLL at <{0}>.", dllPath));
                    using (Registrar registrar = new Registrar(dllPath))
                    {
                        Trace.WriteLine("RegisterCom: Call Registrar.");
                        registrar.RegisterComDLL();
                    }
                    Trace.WriteLine("RegisterCom: RegisterAssembly: Finished registering.");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(String.Format("RegisterCom: RegisterAssembly: ERROR.  Exception.  Msg: <{0}>.", ex.Message));
                    rc = 2;
                }
            }
            else
            {
                Trace.WriteLine(String.Format("RegisterCom: RegisterAssembly: ERROR.  Could not find file <{0}>.", dllPath));
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
                    Trace.WriteLine(String.Format("RegisterCom: UnregisterAssembly: Use Registrar to unregister the DLL at <{0}>.", dllPath));
                    using (Registrar registrar = new Registrar(dllPath))
                    {
                        Trace.WriteLine("RegisterCom: UnregisterAssembly: Call Registrar.");
                        registrar.UnRegisterComDLL();
                    }
                    Trace.WriteLine("RegisterCom: UnregisterAssembly: Finished unregistering.");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(String.Format("RegisterCom: UnregisterAssembly: ERROR.  Exception.  Msg: <{0}>.", ex.Message));
                }
            }
            else
            {
                Trace.WriteLine(String.Format("RegisterCom: UnregisterAssembly: ERROR.  Could not find file <{0}>.", dllPath));
            }
        }

    }
}
