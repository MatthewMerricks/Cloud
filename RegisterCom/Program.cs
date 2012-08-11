using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


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
            Trace.WriteLine("Registrar: LoadLibrary.");
            hLib = LoadLibrary(filePath);
            Trace.WriteLine("Registrar: After LoadLibrary.");
            if (IntPtr.Zero == hLib)
            {
                Trace.WriteLine("Registrar: Error from LoadLibrary.");
                int errno = Marshal.GetLastWin32Error();
                throw new Exception(String.Format("Registrar: Failed to load DLL at path <{0}>.", filePath));
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
            Trace.WriteLine("Registrar: Call GetProcAddress for method: <{0}>.", methodName);
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

    public static class MainProgram
    {
        public static bool shouldTerminate = false;

        static int Main(string[] args)
        {
            Trace.WriteLine("RegisterCom: Main program starting.");
            Trace.WriteLine(String.Format("RegisterCom: Arg count: {0}.", args.Length));
            Trace.WriteLine(String.Format("RegisterCom: First Arg: <{0}>.", args[0]));

            if (args.Length > 0 && args[0] != null)
            {
                Trace.WriteLine("RegisterCom: Call RegisterAssembly.");
                RegisterAssembly(args[0]);
                Trace.WriteLine("RegisterCom: Back from RegisterAssembly.");
            }

            Trace.WriteLine("RegisterCom: Main program terminating.");
            return 0;
        }

        private static void RegisterAssembly(string dllPath)
        {

            if (File.Exists(dllPath))
            {
                try
                {
                    Trace.WriteLine("RegisterCom: Use Registrar to register the DLL at <{0}>.", dllPath);
                    using (Registrar registrar = new Registrar(dllPath))
                    {
                        Trace.WriteLine("RegisterCom: Call Registrar.");
                        registrar.RegisterComDLL();
                    }
                    Trace.WriteLine("RegisterCom: Finished registering.");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("RegisterCom: ERROR.  Exception.  Msg: <{0}>.", ex.Message);
                }
            }
            else
            {
                Trace.WriteLine("RegisterCom: ERROR.  Could not find file <{0}>.", dllPath);
            }
        }
    }
}
