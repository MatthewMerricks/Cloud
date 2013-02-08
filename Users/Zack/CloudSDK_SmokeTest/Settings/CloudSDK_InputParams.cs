using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest
{
    public class CloudSDK_InputParams
    {
        #region Constants
        public const string WriteDefaultValues = "WriteDefaultValues";
        #endregion

        #region Properties
        //TODO: Add Settings File Path as Start Up param 

        public string ActiveSync_Folder { get; set; }
        public string ManualSync_Folder { get; set; }

        public string ActiveSync_TraceFolder { get; set; }
        public string ManualSync_TraceFolder { get; set; }

        public string API_KEY { get; set; }
        public string API_SECRET { get; set; }
        public string TOKEN { get; set; }

        public long ActiveSyncBoxID { get; set; }
        public long ManualSyncBoxID { get; set; }

        public TraceType TraceType { get; set; }
        public int TraceLevel { get; set; }
        public bool LogErrors { get; set; }

        public bool UseDefaults { get; set; }
        public bool IsSilent { get; set; }

        #endregion 

        #region Init
        public CloudSDK_InputParams(bool useDefaults)
        {
            if (useDefaults)
                setDefaultValues();
        }

        #endregion 
        
        #region Implementation
        
        #endregion

        protected void SetAPIKey()
        {
            Console.WriteLine("Please Insert API key");
            string _apiKey = Console.ReadLine();
            WriteToConsole(string.Format("API Key is {0}", _apiKey));
            API_KEY = _apiKey;
        }

        protected void SetAPISecret()
        {
            Console.WriteLine("Please Insert API Secret");
            string _apiSecret = Console.ReadLine();
            WriteToConsole(string.Format("API Secret is {0}", _apiSecret));
            API_SECRET = _apiSecret;
        }

        protected void SetSyncBoxIDs()
        {
            Console.WriteLine("Please Insert The Active SyncBoxID:");
            string aSyncBoxIDString = Console.ReadLine();
            long aSyncBoxID = 0;
            if (!string.IsNullOrEmpty(aSyncBoxIDString))
            {
                long.TryParse(aSyncBoxIDString, out aSyncBoxID);
                if (aSyncBoxID <= 0)
                    Console.WriteLine("The Active SyncBoxID could not be parsed.");
                else
                    ActiveSyncBoxID = aSyncBoxID;
            }
            WriteToConsole(string.Format("ActiveSyncBoxID is {0}", aSyncBoxID));


            Console.WriteLine("Please Insert The Manual SyncBoxID:");
            string mSyncBoxIDString = Console.ReadLine();
            long mSyncBoxID = 0;
            if (!string.IsNullOrEmpty(mSyncBoxIDString))
            {
                long.TryParse(mSyncBoxIDString, out mSyncBoxID);
                if (mSyncBoxID <= 0)
                    Console.WriteLine("The Manual SyncBoxID could not be parsed.");
                else
                    ManualSyncBoxID = mSyncBoxID;
            }
            WriteToConsole(string.Format("ManualSyncBoxID is {0}", mSyncBoxID));
        }

        protected void SetSyncBoxFolders()
        {
            Console.WriteLine("Please Insert The Active SyncBox Folder:");
            string aSyncBoxFolder = Console.ReadLine();
            WriteToConsole(string.Format("Active SyncBox Folder is {0}", aSyncBoxFolder));
            ActiveSync_Folder = aSyncBoxFolder;

            Console.WriteLine("Please Insert The Active SyncBox Trace Folder:");
            string aSyncBoxTraceFolder = Console.ReadLine();
            WriteToConsole(string.Format("Active SyncBox Trace Folder is {0}", aSyncBoxTraceFolder));
            ActiveSync_TraceFolder= aSyncBoxTraceFolder;

            Console.WriteLine("Please Insert The Manual SyncBox Folder:");
            string mSyncBoxFolder = Console.ReadLine();
            WriteToConsole(string.Format("Manual SyncBox Folder is {0}", mSyncBoxFolder));
            ManualSync_Folder = mSyncBoxFolder;

            Console.WriteLine("Please Insert The Manual SyncBox Trace Folder:");
            string mSyncBoxTraceFolder = Console.ReadLine();
            WriteToConsole(string.Format("Manual SyncBox Trace Folder is {0}", mSyncBoxTraceFolder));
            ManualSync_TraceFolder = mSyncBoxTraceFolder;
        }

        public void SetTraceType()
        {
            Console.WriteLine("Please Insert The TraceType:");
            string tt = Console.ReadLine();
            int typeInt = 0;
            Int32.TryParse(tt, out typeInt);
            if (typeInt > 0)
            {
                CloudApiPublic.Static.TraceType unknownFlags;
                CloudApiPublic.Static.TraceType convertedType = unknownFlags = (CloudApiPublic.Static.TraceType)typeInt;

                foreach (int currentTraceTypeInt in Enum.GetValues(typeof(CloudApiPublic.Static.TraceType)).Cast<int>())
                {
                    unknownFlags = (unknownFlags | ((CloudApiPublic.Static.TraceType)currentTraceTypeInt)) // first make sure the flag is set
                        ^ ((CloudApiPublic.Static.TraceType)currentTraceTypeInt); // now unset the flag, result will be remaining flags
                }

                // if there are no unknown flags remaining after removing all of them, then enum value successfully parsed
                if (((int)unknownFlags) == 0)
                {
                    WriteToConsole(string.Format("TraceType Parsed correctly. TraceType Int {0}", (int)convertedType));
                }
                // else if there are unknown flags remaining, then write out the error condition
                else
                {
                    Console.WriteLine(string.Format("Error Parsing TraceType. Known Flags TraceType Int {0} The remainder is {1}.", 
                        ((int)convertedType) - ((int)unknownFlags),
                        (int)unknownFlags));
                }
            }           
        }

        public int SetTraceLevel()
        {
            Console.WriteLine("Please Insert Trace Level");
            string traceString = Console.ReadLine();
            int traceLevel = 0;
            if (!string.IsNullOrEmpty(traceString))
            {
                Int32.TryParse(traceString, out traceLevel);
                if (traceLevel <= 0)
                    Console.WriteLine("The Trace Level could not be parsed.");
                else
                    TraceLevel = traceLevel;
            }
            WriteToConsole(string.Format("Trace Level is {0}", traceLevel));
            return traceLevel;
        }

        public void WriteToConsole(string toWrite)
        {
            if (toWrite.Equals(WriteDefaultValues))
                PrintDefaultValues();
            if (IsSilent != true)
                Console.WriteLine(toWrite);
        }

        protected void PrintDefaultValues()
        {
            Console.WriteLine(string.Format("API KEY: {0}", API_KEY));
            Console.WriteLine(string.Format("API Secret: {0}", API_SECRET));
            Console.WriteLine(string.Format("API TOKEN: {0}", TOKEN));
            Console.WriteLine(string.Format("Active Sync Folder: {0}", ActiveSync_Folder));
            Console.WriteLine(string.Format("Active Sync Trace Folder: {0}", ActiveSync_TraceFolder));
            Console.WriteLine(string.Format("Active SyncBoxID: {0}", ActiveSyncBoxID));
            Console.WriteLine(string.Format("Manual Sync Folder: {0}", ManualSync_Folder));
            Console.WriteLine(string.Format("Manual Sync Trace Folder: {0}", ManualSync_TraceFolder));
            Console.WriteLine(string.Format("Manual SyncBoxID: {0}", ManualSyncBoxID));
            Console.WriteLine(string.Format("Trace Level: {0}", TraceLevel));
            Console.WriteLine(string.Format("Trace Type: {0}", TraceType));
            Console.WriteLine(string.Format("Log Errors: {0}", LogErrors));
        }

        #region Private
        private void setDefaultValues()
        {
            this.API_KEY = "af5c9411437af584cb31506e0d41b34cfd4b6085bf9b51a5ee480c424b5932ff";
            this.API_SECRET = "fc50fff51344faca5ed6cc38e4398e622963d055713db0895a144c8315c5204b";
            this.ActiveSync_Folder = "C:\\TestSyncBoxes\\Auto_SyncBoxA";
            this.ManualSync_Folder = "C:\\TestSyncBoxes\\Maunal_SyncBoxA";
            this.ActiveSyncBoxID = 4;
            //this.ManualSyncBoxID = ;
            this.ActiveSync_TraceFolder = "C:\\TestSyncBoxes\\Traces\\AutoSyncTraces";
            this.ManualSync_TraceFolder = "C:\\TestSyncBoxes\\Traces\\AutoSyncTraces";
            this.TraceType = TraceType.CommunicationIncludeAuthorization;
            this.TraceLevel = 5;
            //this.TOKEN = ;
        }
        #endregion 
    }
}
