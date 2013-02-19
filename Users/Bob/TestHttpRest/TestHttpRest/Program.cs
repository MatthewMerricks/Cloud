using CloudApiPublic.Model;
using TestHttpRest.Helpers;
using TestHttpRest.Settings;
using TestHttpRest.TestClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace TestHttpRest
{
    public sealed class Program
    {
        #region Constants
        public const string SettingsPath = "C:\\Source\\Projects\\win-client\\Users\\Bob\\TestHttpRest\\TestHttpRest\\Settings\\Settings.xml";
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

            lock (ProcessingErrorHolder)
            {
                if (ProcessingErrorHolder.Value != null)
                {
                    Console.WriteLine("Error processing HTTP test: " + ProcessingErrorHolder.Value.errorDescription);
                    WriteExceptionsToScreen(ProcessingErrorHolder);
                    return (int)MainResult.UnknownError;
                }
            }

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

                    foreach (ParallelTaskSet parallelSet in smokeTestClass.Scenario.Items)
                    {
                        System.Threading.Tasks.Parallel.ForEach(
                            parallelSet.Items,
                            (currentTask =>
                                {
                                    SmokeTestTaskHelper.RouteToTaskMethod(smokeTestClass.InputParams, currentTask, ProcessingErrorHolder);
                                }));

                        lock (ProcessingErrorHolder)
                        {
                            if (ProcessingErrorHolder.Value != null)
                            {
                                break;
                            }
                        }
                    }

                    //System.Threading.Tasks.Parallel.ForEach(smokeTestClass.Scenario.Items, (scenarioItem) =>{
                    //    foreach (SmokeTask smokeTask in scenarioItem.Items)
                    //    {
                    //        SmokeTestTaskHelper.RouteToTaskMethod(smokeTestClass.InputParams, smokeTask, ProcessingErrorHolder);
                    //    }
                    //});
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
