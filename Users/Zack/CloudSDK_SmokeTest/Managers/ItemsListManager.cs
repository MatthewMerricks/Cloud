using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public sealed class ItemsListManager
    {

        public readonly List<CloudApiPublic.JsonContracts.Plan> Plans;
        public readonly List<CloudApiPublic.JsonContracts.SyncBox> SyncBoxes;

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
            //
            Plans = new List<CloudApiPublic.JsonContracts.Plan>();
            SyncBoxes = new List<CloudApiPublic.JsonContracts.SyncBox>();
            // check the error
            // create the other direction
        }
    }
}
