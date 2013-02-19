using CloudApiPublic.Model;
using TestHttpRest.Settings;
using TestHttpRest.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TestHttpRest.Helpers
{
    public sealed class SmokeTestTaskHelper
    {
        #region Static
        public static void RouteToTaskMethod(InputParams paramSet, SmokeTask smokeTask, GenericHolder<CLError> ProcessingErrorHolder)
        {
            try
            {
                switch (smokeTask.type)
                {
                    case SmokeTaskType.HttpTest:
                        TestHttpCalls.Run(paramSet, ProcessingErrorHolder);
                        break;
                }
            }
            catch (Exception ex)
            {
                lock (ProcessingErrorHolder)
                {
                    ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                }
            }
        }

        #endregion 

        

    }
}
