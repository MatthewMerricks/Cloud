using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Management;
using System.Linq;
using wyUpdate.Common;

namespace wyUpdate
{
    static class Program
    {
        private static bool didStopExplorer = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            int returnCode = 0;
            Application.EnableVisualStyles();

            try
            {
                // Trace the entry and arguments
                Trace.WriteLine("CloudUpdater: Main: Entry.");
                for (int i = 0; i < args.Length; i++)
                {
                    Trace.WriteLine(String.Format("CloudUpdater: Main: Arg[{0}]: {1}.", i, args[i]));
                }

                // Make sure we catch all events and restart Explorer if it has been killed here.
                Application.ApplicationExit += Application_ApplicationExit;

                // Kill Explorer if we should
                Arguments commands = new Arguments(args);
                if (commands["supdf"] != null)
                {
                    // We are actually updating and running from the temp directory.
                    Trace.WriteLine("CloudUpdater: Main: Call StopExplorer.");
                    didStopExplorer = true;
                    StopExplorer();
                }

                frmMain mainForm = new frmMain(args);

                // if the mainForm has been closed, return 0 (Note: we'll eventually need good return codes)
                if (mainForm.IsDisposed)
                    return 0;

                StringBuilder mutexName = new StringBuilder("Local\\CloudUpdater-" + mainForm.update.GUID);

                if (mainForm.IsAdmin)
                    mutexName.Append('a');

                if (mainForm.SelfUpdateState == SelfUpdateState.FullUpdate)
                    mutexName.Append('s');

                if (mainForm.IsNewSelf)
                    mutexName.Append('n');

                Mutex mutex = new Mutex(true, mutexName.ToString());

                if (mutex.WaitOne(TimeSpan.Zero, true))
                {
                    Trace.WriteLine("CloudUpdater: Main: Call ApplicationRun.");
                    Application.Run(mainForm);

                    mutex.ReleaseMutex();
                }
                else
                {
                    FocusOtherProcess();
                }

                // Make sure Explorer is running
                if (didStopExplorer)
                {
                    RestartExplorer();
                }

                returnCode = mainForm.ReturnCode;
            }
            catch (Exception ex)
            {
                // Trace
                Trace.WriteLine(String.Format("CloudUpdater: Main: ERROR: Exception: Msg: <{0}>.", ex.Message));

                // Make sure Explorer is running.
                if (didStopExplorer)
                {
                    RestartExplorer();
                }

                // Rethrow the exception so the app has the same behavior.
                throw;
            }

            Trace.WriteLine(String.Format("CloudUpdater: Main: Exit with code: {0}.", returnCode));
            return returnCode;
        }


        /// <summary>
        /// The application is exiting.  Restart explorer if it is not running.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void Application_ApplicationExit(object sender, EventArgs e)
        {
                if (didStopExplorer)
                {
                    RestartExplorer();
                }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void RestartExplorer()
        {
            // Start explorer if it is not already running
            string explorerLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            if (!IsExplorerRunning(explorerLocation))
            {
                Process.Start(explorerLocation);
            }

            // Wait to allow Explorer to appear
            const int maxProcessWaits = 40; // corresponds to trying for 20 seconds (if each iteration waits 500 milliseconds)
            for (int waitCounter = 0; waitCounter < maxProcessWaits; waitCounter++)
            {
                // For some reason this won't work unless we wait here for a bit.
                Thread.Sleep(500);
                if (IsExplorerRunning(explorerLocation))
                {
                    Trace.WriteLine("CloudUpdater: RestartExplorer: Explorer is running now.  Break.");
                    break;
                }
            }

            // Wait a little longer to make sure it is fully started.
            Trace.WriteLine("CloudUpdater: RestartExplorer: Explorer is running now.  Wait a bit.");
            Thread.Sleep(2000);
        }

        /// <summary>
        /// Stop Explorer and wait for it to fully stop.
        /// </summary>
        private static void StopExplorer()
        {
            string explorerLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            try
            {
                // Kill Explorer
                Trace.WriteLine(String.Format("CloudUpdater: StopExplorer: Entry. Explorer location: <{0}>.", explorerLocation));
                ProcessStartInfo taskKillInfo = new ProcessStartInfo();
                taskKillInfo.CreateNoWindow = true;
                taskKillInfo.UseShellExecute = false;
                taskKillInfo.FileName = "cmd.exe";
                taskKillInfo.WindowStyle = ProcessWindowStyle.Hidden;
                taskKillInfo.Arguments = "/C taskkill /F /IM explorer.exe";
                Trace.WriteLine("CloudUpdater: StopExplorer: Start the command.");
                Process.Start(taskKillInfo);

                // Wait for all Explorer processes to stop.
                const int maxProcessWaits = 40; // corresponds to trying for 20 seconds (if each iteration waits 500 milliseconds)
                for (int waitCounter = 0; waitCounter < maxProcessWaits; waitCounter++)
                {
                    // For some reason this won't work unless we wait here for a bit.
                    Thread.Sleep(500);
                    if (!IsExplorerRunning(explorerLocation))
                    {
                        Trace.WriteLine("CloudUpdater: StopExplorer: Explorer is not running.  Break.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("CloudUpdater: StopExplorer: ERROR: Exception: Msg: <{0}.", ex.Message));
            }
            Trace.WriteLine("CloudUpdater: StopExplorer: Exit.");
        }

        /// <summary>
        /// Determine whether Explorer is running.
        /// </summary>
        /// <param name="explorerLocation"></param>
        /// <returns></returns>
        private static bool IsExplorerRunning(string explorerLocation)
        {
            bool isExplorerRunning = false;         // assume not running

            try
            {
                Trace.WriteLine(String.Format("CloudUpdater: IsExplorerRunning: Entry. explorerLocation: <{0}>.", explorerLocation));
                string wmiQueryString = "SELECT ProcessId, ExecutablePath FROM Win32_Process";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQueryString))
                {
                    if (searcher != null)
                    {
                        Trace.WriteLine("CloudUpdater: IsExplorerRunning: searcher not null. Get the results.");
                        using (ManagementObjectCollection results = searcher.Get())
                        {
                            Trace.WriteLine("CloudUpdater: IsExplorerRunning: Run the query.");
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
                        Trace.WriteLine("CloudUpdater: IsExplorerRunning: ERROR: searcher is null.");
                        return isExplorerRunning;           // assume Explorer is not running.
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("CloudUpdater: IsExplorerRunning: ERROR: Exception: Msg: <{0}>.", ex.Message));
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

        [DllImport("user32")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32")]
        static extern int ShowWindow(IntPtr hWnd, int swCommand);
        [DllImport("user32")]
        static extern bool IsIconic(IntPtr hWnd);

        public static void FocusOtherProcess()
        {
            Process proc = Process.GetCurrentProcess();

            // Using Process.ProcessName does not function properly when
            // the actual name exceeds 15 characters. Using the assembly 
            // name takes care of this quirk and is more accurate than 
            // other work arounds.

            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;

            foreach (Process otherProc in Process.GetProcessesByName(assemblyName))
            {
                //ignore "this" process, and ignore wyUpdate with a different filename

                if (proc.Id != otherProc.Id 
                        && otherProc.MainModule != null && proc.MainModule != null 
                        && proc.MainModule.FileName == otherProc.MainModule.FileName)
                {
                    // Found a "same named process".
                    // Assume it is the one we want brought to the foreground.
                    // Use the Win32 API to bring it to the foreground.

                    IntPtr hWnd = otherProc.MainWindowHandle;

                    if (IsIconic(hWnd))
                        ShowWindow(hWnd, 9); //SW_RESTORE

                    SetForegroundWindow(hWnd);
                    break;
                }
            }
        }
    }
}