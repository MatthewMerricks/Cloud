using Cloud.Model;
using CloudSDK_SmokeTest.Events.CLEventArgs;
using CloudSDK_SmokeTest.Helpers;
using CloudSDK_SmokeTest.Interfaces;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public sealed class ItemsListManager : ISmokeTaskManager
    {

        public readonly List<Cloud.JsonContracts.Plan> Plans;
        public readonly List<Cloud.JsonContracts.SyncBox> SyncBoxes;
        public readonly List<Cloud.JsonContracts.Session> Sessions;

        public readonly List<string> SessionsCreatedDynamically;
        public readonly List<long> SyncBoxesCreatedDynamically;
        public readonly List<long> PlansCreatedDynamically;

        private static readonly GenericHolder<ItemsListManager> _instance = new GenericHolder<ItemsListManager>(null);
        public static ItemsListManager GetInstance()
        {
            lock (_instance)
            {
                if (_instance.Value == null)
                {
                    _instance.Value = new ItemsListManager();
                }
                return _instance.Value;
            }

        }

        private ItemsListManager()
        {
            Plans = new List<Cloud.JsonContracts.Plan>();
            SyncBoxes = new List<Cloud.JsonContracts.SyncBox>();
            Sessions = new List<Cloud.JsonContracts.Session>();
            SessionsCreatedDynamically = new List<string>();
            SyncBoxesCreatedDynamically = new List<long>();
            PlansCreatedDynamically = new List<long>();

           
            // check the error
            // create the other direction
        }

        public void RemoveSession(Cloud.JsonContracts.Session session)
        {
            Cloud.JsonContracts.Session fromList = Sessions.Where(s => s.Key == session.Key && s.Token == session.Token).FirstOrDefault();
            if (fromList != null)
                Sessions.Remove(fromList);
        }

        public static int RunListItemsTask(InputParams paramSet, SmokeTask smokeTask, ref StringBuilder reportBuilder, ref GenericHolder<CLError> ProcessingErrorHolder)
        {
            int responseCode = -1;
            ListItems listTask = smokeTask as ListItems;
            ItemsListManager manager = ItemsListManager.GetInstance();
            if (listTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            ItemListHelperEventArgs eventArgs = new ItemListHelperEventArgs()
            {
                ProcessingErrorHolder = ProcessingErrorHolder,
                ParamSet = paramSet,
                ListItemsTask = listTask,
                ReportBuilder = reportBuilder,
            };
            switch (listTask.ListType)
            {
                case ListItemsListType.Plans:
                    reportBuilder.AppendLine("Entering List Plans");
                    ItemsListHelper.RunListSubscribedPlans(eventArgs);
                    reportBuilder.AppendLine("Exiting List Plans");
                    break;
                case ListItemsListType.Sessions:
                    reportBuilder.AppendLine("Entering List Sessions");
                    ItemsListHelper.RunListSessions(eventArgs, true, true);
                    reportBuilder.AppendLine("Exiting List Sessions");
                    break;
                case ListItemsListType.SyncBoxes:
                    reportBuilder.AppendLine("Entering List SyncBoxes");
                    ItemsListHelper.RunListSubscribtionSyncBoxes(eventArgs);
                    reportBuilder.AppendLine("Exiting List SyncBoxes");
                    break;
            }
            return responseCode;
        }

        public int Create(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }

        public int Rename(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }

        public int Delete(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }

        public int UnDelete(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }

        public int Download(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }

        public int ListItems(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            int responseCode = -1;
            ListItems listTask = e.CurrentTask as ListItems;
            ItemsListManager manager = ItemsListManager.GetInstance();
            if (listTask == null)
                return (int)FileManagerResponseCodes.InvalidTaskType;

            ItemListHelperEventArgs eventArgs = new ItemListHelperEventArgs()
            {
                ProcessingErrorHolder = e.ProcessingErrorHolder,
                ParamSet = e.ParamSet,
                ListItemsTask = listTask,
                ReportBuilder = e.ReportBuilder,
            };
            switch (listTask.ListType)
            {
                case ListItemsListType.Plans:
                    Console.WriteLine("Entering List Plans");
                    ItemsListHelper.RunListSubscribedPlans(eventArgs);
                    Console.WriteLine("Exiting List Plans");
                    break;
                case ListItemsListType.Sessions:
                    Console.WriteLine("Entering List Sessions");
                    ItemsListHelper.RunListSessions(eventArgs, true, true);
                    Console.WriteLine("Exiting List Sessions");
                    break;
                case ListItemsListType.SyncBoxes:
                    Console.WriteLine("Entering List SyncBoxes");
                    ItemsListHelper.RunListSubscribtionSyncBoxes(eventArgs);
                    Console.WriteLine("Exiting List SyncBoxes");
                    break;
            }
            return responseCode;
        }

        public int AlternativeAction(Events.ManagerEventArgs.SmokeTestManagerEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
