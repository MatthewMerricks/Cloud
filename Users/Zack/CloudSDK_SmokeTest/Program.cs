﻿using CloudApiPublic;
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

                    foreach (ParallelTaskSet parallelSet in smokeTestClass.Scenario.Items)
                    {
                        StringBuilder _report = new StringBuilder();
                        foreach (SmokeTask currentTask in parallelSet.Items)
                        {
                            //SmokeTestManagerEventArgs e = new SmokeTestManagerEventArgs(smokeTestClass.InputParams, currentTask, ProcessingErrorHolder);
                            //SmokeTestTaskHelper.RouteToTask(e);
                            int response = SmokeTestTaskHelper.RouteToTaskMethod(smokeTestClass.InputParams, currentTask, ref _report, ProcessingErrorHolder);
                            //RunInnerTasks(smokeTestClass, currentTask.InnerTask, currentTask.SyncBoxes);
                            RunInnerTasks(smokeTestClass, currentTask, ref _report);
                            Console.Write(_report.ToString());

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

        //ZW Replace
        //public static void RunInnerTasks(SmokeTest smokeTestClass, SmokeTask currentTask, Dictionary<long, CLSyncBox> syncBoxes)
        public static void RunInnerTasks(SmokeTest smokeTestClass, SmokeTask currentTask, ref StringBuilder ReportBuilder)
        {
            SmokeTask innerTask = currentTask.InnerTask;
            if (innerTask != null)
            {
                if (innerTask.SelectedSyncBoxID <= 0)
                    innerTask.SelectedSyncBoxID = currentTask.SelectedSyncBoxID;
                //ZW Replace
                //foreach (var box in syncBoxes)
                //{
                //    if (!currentTask.SyncBoxes.Keys.Contains(box.Key))
                //        currentTask.SyncBoxes.Add(box.Key, box.Value);
                //}
                SmokeTestTaskHelper.RouteToTaskMethod(smokeTestClass.InputParams, innerTask, ref ReportBuilder, ProcessingErrorHolder);
                if (innerTask.InnerTask != null)
                {
                    SmokeTask thisTask = innerTask.InnerTask;
                    SmokeTestTaskHelper.RouteToTaskMethod(smokeTestClass.InputParams, thisTask, ref ReportBuilder, ProcessingErrorHolder);
                    if(ProcessingErrorHolder.Value == null)
                        RunInnerTasks(smokeTestClass, thisTask, ref ReportBuilder);
                        //ZW Replace 
                        //RunInnerTasks(smokeTestClass, thisTask.InnerTask, thisTask.SyncBoxes);
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
