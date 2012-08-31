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
    ///     RegisterCom <path to the Cloud directory in Program Files>
    ///   o After uninstall:
    ///     RegisterCom /u
    /// This processes as follows:
    ///   if first parm is not "/u"
    ///     check to see that the target BadgeCom.dll exists.
    ///     if BadgeCom.ddl does not exist
    ///       trace error
    ///       exit
    ///     else BadgeCom.dll exists
    ///       delete the target file for the following copy command (if it exists)
    ///       copy the 32-bit or 64-bit BadgeCom.dll to System32 as CloudBadgeCOM.dll
    ///       if error copying
    ///         trace error
    ///         exit
    ///       endif error copying
    ///       delete the target file for the following copy command (if it exists)
    ///       copy the RegisterCom.exe file to System32 as CloudRegisterCom.exe
    ///       if this is 64-bit
    ///         copy ProgramFiles\msvcp100.dll to System32, unless it already exists
    ///         copy ProgramFiles\msvcr100.dll to System32, unless it already exists
    ///         copy ProgramFiles\atl100.dll to System32, unless it already exists
    ///       endif this is 64-bit
    ///     endelse BadgeCom.ddl exists
    ///     ;
    ///     stop Explorer
    ///     wait for Explorer processes to stop
    ///     ;
    ///     perform the equivalalent of RegSvr32 BadgeCOM.dll
    ///     ;
    ///     start Explorer
    ///   else first parm is "/u"
    ///     stop Explorer
    ///     wait for all Explorer processes to stop
    ///     ;
    ///     perform the equivalent of RegSvr32 /u c:\windows\system32\CloudBadgeCOM.dll
    ///     delete c:\windows\system32\BadgeCOM.dll
    ///     ;
    ///     start Explorer
    ///   endif first parm is "/u"
    /// </summary>

    public static class MainProgram
    {
        public static bool shouldTerminate = false;

        static int Main(string[] args)
        {
            try
            {
                Trace.WriteLine("RegisterCom: Main program starting.");
                Trace.WriteLine(String.Format("RegisterCom: Arg count: {0}.", args.Length));

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

                // Installation.  The first parm should point to BadgeCOM.dll in the program files directory.
                if (args.Length == 0 || firstArg == null)
                {
                    // No arguments.
                    Trace.WriteLine("RegisterCom: Main: ERROR.  No arguments.");
                    return 1;
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
                if (!File.Exists(pathBadgeCOM))
                {
                    Trace.WriteLine(string.Format("RegisterCom: Main: ERROR.  Could not find BadgeCOM.dll at path {0}.", pathBadgeCOM));
                    return 2;
                }

                string pathSystem32BadgeCOM = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "CloudBadgeCOM.dll");

                // Copy BadgeCom.dll to System32\CloudBadgeCom.dll for uninstall.
                try
                {
                    // Found BadgeCOM.dll.  We will copy it to System32 as CloudBadgeCom.dll.  Delete the target first
                    if (File.Exists(pathSystem32BadgeCOM))
                    {
                        Trace.WriteLine("RegisterCom: Main: Delete existing CloudBadgeCOM.dll in System32.");
                        File.Delete(pathSystem32BadgeCOM);
                    }

                    Trace.WriteLine(String.Format("RegisterCom: Main: Copy file {0} to {1}.", pathBadgeCOM, pathSystem32BadgeCOM));
                    File.Copy(pathBadgeCOM, pathSystem32BadgeCOM);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(String.Format("RegisterCom: Main: ERROR: Exception.  Msg: {0}.", ex.Message));
                    return 3;
                }

                // Also copy this program (RegisterCom.exe) to system32 as CloudRegisterCom.exe.
                string pathSystem32RegisterCom = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "CloudRegisterCom.exe");
                string pathRegisterCom = Path.Combine(firstArg, "RegisterCom.exe");
                try
                {
                    // Delete the target first
                    if (File.Exists(pathSystem32RegisterCom))
                    {
                        Trace.WriteLine("RegisterCom: Main: Delete existing CloudRegisterCom.exe in System32.");
                        File.Delete(pathSystem32RegisterCom);
                    }

                    Trace.WriteLine(String.Format("RegisterCom: Main: Copy file {0} to {1}.", pathRegisterCom, pathSystem32RegisterCom));
                    File.Copy(pathRegisterCom, pathSystem32RegisterCom);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(String.Format("RegisterCom: Main: ERROR: Exception(2).  Msg: {0}.", ex.Message));
                    return 3;
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
                        return 4;
                    }

                    // Copy msvcr100.dll
                    string pathSystem32Msvcr100 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msvcr100.dll");
                    string pathMsvcr100 = firstArg + "msvcr100.dll";
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
                        return 4;
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
                        return 4;
                    }
                }

                // Stop Explorer
                Trace.WriteLine("RegisterCom: Main: Stop Explorer");
                string explorerLocation = StopExplorer();

                // Register BadgeCOM.dll in the System32 directory.
                Trace.WriteLine("RegisterCom: Call RegisterAssembly.");
                RegisterAssembly(pathSystem32BadgeCOM);
                Trace.WriteLine("RegisterCom: Back from RegisterAssembly.");

                // Start Explorer
                Trace.WriteLine("RegisterCom: Main: Start Explorer");
                Process.Start(explorerLocation);

                Trace.WriteLine("RegisterCom: Main: Installation successful.");
                return 0;
            }
            catch (Exception ex)
            {
                StringBuilder exBuilder = new StringBuilder("RegisterCom: Main: Exception: ");
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
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C taskkill /F /IM explorer.exe";
            process.StartInfo = startInfo;
            process.Start();

            // Wait for all Explorer processes to stop.
            string explorerLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");

            const int maxProcessWaits = 40; // corresponds to trying for 20 seconds (if each iteration waits 500 milliseconds)
            for (int waitCounter = 0; waitCounter < maxProcessWaits; waitCounter++)
            {
                // For some reason this won't work unless we wait here for a bit.
                Thread.Sleep(500);
                if (!IsExplorerRunning(explorerLocation))
                {
                    break;
                }
            }
            return explorerLocation;
        }

        /// <summary>
        /// Uninstall function.
        /// </summary>
        /// <returns></returns>
        private static int UninstallCOM()
        {
            // The BadgeCOM.dll file should be in the system directory
            Trace.WriteLine("RegisterCom: UninstallCOM: Entry.");
            string pathSystem32BadgeCOM = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "CloudBadgeCOM.dll");
            if (!File.Exists(pathSystem32BadgeCOM))
            {
                Trace.WriteLine(String.Format("RegisterCom: UninstallCOM: ERROR.  BadgeCOM.dll not found at path {0}.", pathSystem32BadgeCOM));
                return 4;
            }

            try
            {
                // Stop Explorer
                Trace.WriteLine("RegisterCom: UninstallCOM: Stop Explorer");
                string explorerLocation = StopExplorer();

                // Unregister BadgeCOM in System32.
                UnregisterAssembly(pathSystem32BadgeCOM);

                // Delete BadgeCOM.dll in System32.
                File.Delete(pathSystem32BadgeCOM);

                // Start Explorer
                Trace.WriteLine("RegisterCom: UninstallCOM: Start Explorer");
                Process.Start(explorerLocation);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("RegisterCom: UninstallCOM: ERROR.  Exception.  Msg: {0}.", ex.Message));
                return 5;
            }

            Trace.WriteLine("RegisterCom: UninstallCOM: Uninstallation successful.");
            return 0;
        }

        private static bool IsExplorerRunning(string explorerLocation)
        {
            string wmiQueryString = "SELECT ProcessId, ExecutablePath FROM Win32_Process";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQueryString))
            using (ManagementObjectCollection results = searcher.Get())
            {
                return Process.GetProcesses()
                    .Where(parent => parent.ProcessName.Equals("explorer", StringComparison.InvariantCultureIgnoreCase))
                    .Join(results.Cast<ManagementObject>(),
                        parent => parent.Id,
                        parent => (int)(uint)parent["ProcessId"],
                        (outer, inner) => new ProcessWithPath(outer, (string)inner["ExecutablePath"]))
                    .Any(parent => parent.Path.Equals(explorerLocation, StringComparison.InvariantCultureIgnoreCase));
            }
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

        private static void RegisterAssembly(string dllPath)
        {

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
                }
            }
            else
            {
                Trace.WriteLine(String.Format("RegisterCom: RegisterAssembly: ERROR.  Could not find file <{0}>.", dllPath));
            }
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
