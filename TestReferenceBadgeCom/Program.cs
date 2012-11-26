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
            EnumPubSubServerPublishReturnCodes  rcPublish = test.Publish(EnumEventType.BadgeNet_To_BadgeCom, EnumEventSubType.BadgeNet_AddBadgePath,EnumCloudAppIconBadgeType.cloudAppBadgeSynced, "This is a full path");
            Guid myGuid = Guid.NewGuid();
            EnumEventSubType outEventSubType;
            EnumCloudAppIconBadgeType outBadgeType;
            string outFullPath;
            EnumPubSubServerSubscribeReturnCodes rcSubscribe = test.Subscribe(EnumEventType.BadgeNet_To_BadgeCom, myGuid, 0,
                        out outEventSubType, out outBadgeType, out outFullPath);
            EnumPubSubServerCancelWaitingSubscriptionReturnCodes rcCancel = test.CancelWaitingSubscription(EnumEventType.BadgeNet_To_BadgeCom, myGuid);
            test.Terminate();
           
        }
    }
}
