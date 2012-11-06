using System;
using System.Collections.Generic;
using System.Text;
using 

namespace TestReferenceBadgeCom
{
    class Program
    {
        static void Main(string[] args)
        {

            PubSubServerClass test = new PubSubServerClass();
            string sharedMemoryName = test.SharedMemoryName;
            int rc = test.Publish(EnumEventType.BadgeNet_AddBadgePath,EnumCloudAppIconBadgeType.cloudAppBadgeSynced, "This is a full path");
            rc = test.Subscribe(EnumEventType.BadgeNet_AddBadgePath, 10);
            
            
           
        }
    }
}
