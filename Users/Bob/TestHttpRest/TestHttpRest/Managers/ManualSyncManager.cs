using CloudApiPublic;
using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudSDK_SmokeTest.Helpers;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public sealed class ManualSyncManager
    {

        #region Properties 
        public InputParams InputParams { get; set; }

        #endregion 


        #region Init
        public ManualSyncManager(InputParams paramSet)
        {
            this.InputParams = paramSet;
        }
        #endregion 

        #region All

        private void InitalizeCredentials(out CLCredential creds, out CLCredentialCreationStatus credsCreateStatus)
        {
            Console.WriteLine("Initializing Credentials for Active Sync Create Method... ");
            Console.WriteLine();
            CLCredential.CreateAndInitialize(InputParams.API_Key, InputParams.API_Secret, out creds, out credsCreateStatus);
            Console.WriteLine(string.Format("Credential Initialization {0}", credsCreateStatus.ToString()));
        }

        private void ThrowDuplicateException(ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            Exception outerException = new Exception("Attepmting to CreateFile duplicate file.");
            lock (ProcessingErrorHolder)
            {
                ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + outerException;
            }
        }

        #endregion 
    }
}
