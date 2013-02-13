using CloudApiPublic.Model;
using CloudSDK_SmokeTest.Helpers;
using CloudSDK_SmokeTest.Managers;
using CloudSDK_SmokeTest.Settings;
using CloudSDK_SmokeTest.TestClasses;
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

                    SmokeTask task = GetCreateSyncBoxTask(smokeTestClass);
                    if (task != null)
                    {
                        SmokeTestTaskHelper.RouteToTaskMethod(smokeTestClass.InputParams, task, ProcessingErrorHolder);
                    }
                    else
                    {
                        Exception exception = new Exception("A Create SyncBox Task is Required in the Settings File, even if its CreateBox Boolean is Set to False");
                        lock (ProcessingErrorHolder)
                        {
                            ProcessingErrorHolder.Value = ProcessingErrorHolder.Value + exception;
                        }
                    }

                    #region Parallel Op Code
                    // use this to run the task sets in parallel
                    System.Threading.Tasks.Parallel.ForEach(smokeTestClass.Scenario.Items, (scenarioItem) =>
                    {
                        foreach (SmokeTask smokeTask in scenarioItem.Items)
                        {
                            if (smokeTask.type != SmokeTaskType.CreateSyncBox)
                                SmokeTestTaskHelper.RouteToTaskMethod(smokeTestClass.InputParams, smokeTask, ProcessingErrorHolder);
                        }
                    });
                    #endregion 

                    #region Syncronous Op Code
                    //// use this to serialize the parallel tasks
                    //
                    //foreach (ParallelTaskSet scenarioItem in smokeTestClass.Scenario.Items)
                    //{
                    //    foreach(SmokeTask smokeTask in scenarioItem.Items)
                    //         SmokeTestTaskHelper.RouteToTaskMethod(smokeTestClass.InputParams, smokeTask, ProcessingErrorHolder);
                    //}
                    #endregion                 

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

        private static SmokeTask GetCreateSyncBoxTask(SmokeTest smokeTestClass)
        {
            SmokeTask task = null;
            foreach (ParallelTaskSet taskSet in smokeTestClass.Scenario.Items)
            {
                bool canContinue = false;
                foreach (SmokeTask smokeTask in taskSet.Items)
                {
                    if (smokeTask.type == SmokeTaskType.CreateSyncBox)
                    {
                        task = smokeTask;
                        canContinue = false;
                        break;
                    }
                }
                if (!canContinue)
                    break;
            }
            return task;
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
