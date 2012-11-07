using System;
using System.Collections.Generic;
using System.Text;
using BadgeCOMLib;
using System.Threading;
using System.Diagnostics;

namespace TestReferenceBadgeCom
{
    class Program
    {
        static void Main(string[] args)
        {

            PubSubServerClass test = new PubSubServerClass();
            test.Initialize();
            string sharedMemoryName = test.SharedMemoryName;
            EnumPubSubServerPublishReturnCodes  rcPublish = test.Publish(EnumEventType.BadgeNet_AddBadgePath,EnumCloudAppIconBadgeType.cloudAppBadgeSynced, "This is a full path");
            Guid myGuid = Guid.NewGuid();
            EnumPubSubServerSubscribeReturnCodes rcSubscribe = test.Subscribe(EnumEventType.BadgeNet_AddBadgePath, myGuid, 0);
            EnumPubSubServerCancelWaitingSubscriptionReturnCodes rcCancel = test.CancelWaitingSubscription(EnumEventType.BadgeNet_AddBadgePath, myGuid);
            test.Terminate();
           
        }
    }
}
