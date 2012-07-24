//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Notification
{
    using System;
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.Threading.Tasks;
    //using Microsoft.AppFabric.Tracing;

    public class NotificationSender
    {
        public NotificationSender()
        {
            this.NotificationServiceChannelFactory = new ChannelFactory<INotificationService>(new NetTcpBinding(SecurityMode.None));
        }

        ChannelFactory<INotificationService> NotificationServiceChannelFactory { get; set; }

        public void SendNotification(NotificationEvent notificationEvent, Uri[] receiverAddresses)
        {
            this.SendNotification(new List<NotificationEvent>() { notificationEvent }, receiverAddresses);
        }

        public void SendNotification(IList<NotificationEvent> notificationEvents, Uri[] receiverAddresses)
        {
            foreach (Uri address in receiverAddresses)
            {
                // fire and forget
                Task.Factory.StartNew((object addressObject) => this.CallNotificationServices(notificationEvents, (Uri)addressObject), address)
                    .ContinueWith(
                        (antecedents) =>
                        {
                           // ManagementEtwProvider.Provider.EventWriteThrowingException(antecedents.Exception.ToString());
                        },
                        TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        void CallNotificationServices(IList<NotificationEvent> notificationEvents, Uri listenerUri)
        {
            INotificationService notificationService = null;
            IClientChannel clientChannel = null;

            // See the following links which explain the rather sophisticated exception handling below (instead of a simple "using() {})"
            //  * http://msdn.microsoft.com/en-us/library/aa354510.aspx
            //  * http://msdn.microsoft.com/en-us/library/aa355056.aspx
            try
            {
                // For now, we open the channel each and every time. We could do channel caching if we see performance problems
                // Do not attempt to create the channel if the factory was deleted (shutdown case)
                if (this.NotificationServiceChannelFactory != null)
                {
                    notificationService = this.NotificationServiceChannelFactory.CreateChannel(new EndpointAddress(listenerUri));
                    clientChannel = notificationService as IClientChannel;
                }

                if (notificationService != null && clientChannel != null)
                {
                    clientChannel.Open();
                    notificationService.Notify(notificationEvents);
                    clientChannel.Close();
                }
            }
            catch (Exception ex)
            {
                //ManagementEtwProvider.Provider.EventWriteThrowingException(ex.ToString());

                if (clientChannel != null)
                {
                    clientChannel.Abort();
                }

                if (!(ex is CommunicationException ||
                      ex is TimeoutException ||
                      ex is InvalidOperationException))
                {
                    throw;
                }
            }
        }
    }
}
