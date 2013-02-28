using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Settings
{
    public partial class InputParams
    {
        public static void PrintDefaultValues(InputParams input)
        {
            Console.WriteLine("Initialized Input Parameters:");
            Console.WriteLine();
            Console.WriteLine(string.Format("API KEY: {0}", input.API_Key));
            Console.WriteLine(string.Format("API Secret: {0}", input.API_Secret));
            Console.WriteLine(string.Format("API TOKEN: {0}", input.Token));
            Console.WriteLine(string.Format("Active Sync Folder: {0}", input.ActiveSync_Folder));
            Console.WriteLine(string.Format("Active Sync Trace Folder: {0}", input.ActiveSync_TraceFolder));
            Console.WriteLine(string.Format("Active SyncBoxID: {0}", input.ActiveSyncBoxID));
            Console.WriteLine(string.Format("Manual Sync Folder: {0}", input.ManualSync_Folder));
            Console.WriteLine(string.Format("Manual Sync Trace Folder: {0}", input.ManualSync_TraceFolder));
            Console.WriteLine(string.Format("Manual SyncBoxID: {0}", input.ManualSyncBoxID));
            Console.WriteLine(string.Format("Trace Level: {0}", input.TraceLevel));
            Console.WriteLine(string.Format("Trace Type: {0}", input.TraceType));
            Console.WriteLine();
        }
    }


    public partial class SmokeTask
    {
        public bool IsComparison()
        {
            if (this.GetType() == typeof(Comparison))
            {
                return true;
            }
            return false;
        }
    }
}
