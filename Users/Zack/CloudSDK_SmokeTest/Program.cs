using CloudApiPublic;
using CloudApiPublic.Model;
using CloudSDK_SmokeTest.Events.ManagerEventArgs;
using CloudSDK_SmokeTest.Helpers;
using CloudSDK_SmokeTest.Managers;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace CloudSDK_SmokeTest
{
    public sealed class Program
    {
        #region Constants
        //TODO: Pull this from the XSD
        public const string SettingsPath = "C:\\Cloud\\windows-client\\Users\\Zack\\CloudSDK_SmokeTest\\Settings\\Settings.xml";
        private static Dictionary<int, StringBuilder> staticBuilderList = new Dictionary<int, StringBuilder>();
        #endregion

        #region Properties
        private static readonly GenericHolder<CLError> ProcessingErrorHolder = new GenericHolder<CLError>(null);


        #endregion

        #region Implementation
        [STAThread]
        static int Main(string[] args)
        {


            object waitForCompletion = new object();

            lock (waitForCompletion)
            {
                // Start async thread to process everything
                (new Thread(new ParameterizedThreadStart(SmokeTestProcessor)))
                    .Start((waitForCompletion));

                Monitor.Wait(waitForCompletion);
            }
            bool isError = false;
            lock (ProcessingErrorHolder)
            {
                if (ProcessingErrorHolder.Value != null)
                {
                    Console.WriteLine("Error processing smoke test: " + ProcessingErrorHolder.Value.errorDescription);
                    isError = true;                    
                }
            }

            WriteExceptionsToScreen(ProcessingErrorHolder);
            if(isError)
                return (int)MainResult.UnknownError;
            else
                return (int)MainResult.Success;
        }
        #endregion

        #region Private
        private static void SmokeTestProcessor(object state)
        {
            StringBuilder ReportBuilder = new StringBuilder();
            object waitForCompletion = null;
            Console.WriteLine("Initialization Begining ... ");
            Console.WriteLine();
            try
            {
                waitForCompletion = state;
                XmlSerializer serializer = new XmlSerializer(typeof(SmokeTest));

                using (XmlReader reader = XmlReader.Create(SettingsPath))
                {
                    SmokeTest smokeTestClass = (SmokeTest)serializer.Deserialize(reader);
                    //for each Scenario in the Smoke Test Class Run the Scenario. 
                    Console.WriteLine("Xml Data Initialized From Schema...");
                    Console.WriteLine();
                    InputParams.PrintDefaultValues(smokeTestClass.InputParams);
                    int opperationId =0;
                    foreach (ParallelTaskSet parallelSet in smokeTestClass.Scenario.Items)
                    {
                        StringBuilder _report = new StringBuilder();
                        foreach (SmokeTask currentTask in parallelSet.Items)
                        {
                            SmokeTestManagerEventArgs e = new SmokeTestManagerEventArgs(smokeTestClass.InputParams, currentTask, ProcessingErrorHolder);
                            e.ReportBuilder = new StringBuilder();
                            SmokeTestTaskHelper.RouteToTask(e);
                            RunInnerTasks(smokeTestClass, currentTask, e);
                            for (int x = 0; x < e.StringBuilderList.Count; x++)
                            {
                                Console.WriteLine(e.StringBuilderList[x].ToString());
                            }

                        }
                        //System.Threading.Tasks.Parallel.ForEach(
                        //    parallelSet.Items,
                        //    (currentTask =>
                        //    {
                        //        //SmokeTestManagerEventArgs e = new SmokeTestManagerEventArgs(smokeTestClass.InputParams, currentTask, ProcessingErrorHolder);
                        //        //SmokeTestTaskHelper.RouteToTask(e);
                        //        int response = SmokeTestTaskHelper.RouteToTaskMethod(smokeTestClass.InputParams, currentTask, ref _report, ProcessingErrorHolder);
                        //        //RunInnerTasks(smokeTestClass, currentTask.InnerTask, currentTask.SyncBoxes);
                        //        RunInnerTasks(smokeTestClass, currentTask, ref _report);
                        //        Console.Write(_report.ToString());

                        //    }));

                        lock (ProcessingErrorHolder)
                        {
                            if (ProcessingErrorHolder.Value != null)
                            {
                                break;
                            }
                        }
                    }
                }
                lock (waitForCompletion)
                {
                    Monitor.Pulse(waitForCompletion);
                }
            }
            catch (Exception ex)
            {
                if (waitForCompletion == null)
                {
                    throw ex;
                }
                else
                {
                    lock (ProcessingErrorHolder)
                    {
                        ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + ex;
                    }
                    lock (waitForCompletion)
                    {
                        Monitor.Pulse(waitForCompletion);
                    }
                }
            }
        }

        public static void RunInnerTasks(SmokeTest smokeTestClass, SmokeTask currentTask, SmokeTestManagerEventArgs e)
        {
            List<StringBuilder> builderList = new List<StringBuilder>();
            StringBuilder reportBuilder = new StringBuilder();
            e.ReportBuilder = reportBuilder;
            SmokeTask innerTask = currentTask.InnerTask;
            if (innerTask != null)
            {
                StringBuilder _report = e.ReportBuilder;
                if (innerTask.SelectedSyncBoxID <= 0)
                    innerTask.SelectedSyncBoxID = currentTask.SelectedSyncBoxID;
                
                if (innerTask != null)
                {
                    SmokeTask thisTask = innerTask;
                    SmokeTestManagerEventArgs ea = new SmokeTestManagerEventArgs(e) { StringBuilderList = e.StringBuilderList};
                    ea.CurrentTask = thisTask;
                    SmokeTestTaskHelper.RouteToTask(ea);
                    if (ProcessingErrorHolder.Value == null)
                    {
                        RunInnerTasks(smokeTestClass, thisTask, e);
                        staticBuilderList.Add(staticBuilderList.Count(), reportBuilder);
                    }
                }
            }
        }

        private static void WriteExceptionsToScreen(GenericHolder<CLError> ProcessingErrorHolder)
        {
            if (ProcessingErrorHolder.Value == null)
            {
                Console.WriteLine("There are no exceptions to be listed.");
            }
            else
            {
                IEnumerable<Exception> exceptions = ProcessingErrorHolder.Value.GrabExceptions();
                int counter = 0;
                foreach (Exception exception in exceptions)
                {
                    counter++;
                    Console.WriteLine(string.Format("Exception {0}:", counter));
                    Console.WriteLine(string.Format("   Source: {0}", exception.Source));
                    Console.WriteLine(string.Format("   Message: {0}", exception.Message));
                    Console.WriteLine(string.Format("   StackTrace: {0}", exception.StackTrace));
                }
            }
        }
        #endregion
    }
}
