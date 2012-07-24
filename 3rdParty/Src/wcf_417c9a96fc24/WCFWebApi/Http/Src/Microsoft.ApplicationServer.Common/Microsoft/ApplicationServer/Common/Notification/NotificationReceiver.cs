//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common.Notification
{
    using System;
    using System.Collections.Generic;
    using System.ServiceModel;
    //using Microsoft.AppFabric.Tracing;

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = true)]
    public abstract class NotificationReceiver : INotificationService, IDisposable
    {
        static readonly TimeSpan ServiceHostOpenningTimeout = TimeSpan.FromSeconds(30);

        ServiceHost serviceHost;
        Uri baseAddress;
        bool disposed;

        protected NotificationReceiver(Uri baseAddress)
        {
            this.baseAddress = baseAddress;
        }

        public void Start()
        {
            if (this.baseAddress != null)
            {
                this.serviceHost = new ServiceHost(this, this.baseAddress);

                try
                {
                    this.serviceHost.AddServiceEndpoint(typeof(INotificationService), new NetTcpBinding(SecurityMode.None), string.Empty);
                    this.serviceHost.Open(ServiceHostOpenningTimeout);
                }
                catch (Exception ex)
                {
                    //ManagementEtwProvider.Provider.EventWriteThrowingException(ex.ToString());

                    // Eat expected exceptions since Notification is optional
                    if (!(ex is CommunicationException ||
                          ex is TimeoutException ||
                          ex is InvalidOperationException))
                    {
                        throw;
                    }
                }
            }
        }

        public void Stop()
        {
            if (this.serviceHost != null && this.baseAddress != null)
            {
                try
                {
                    this.serviceHost.Close();
                }
                catch (Exception ex)
                {
                    //ManagementEtwProvider.Provider.EventWriteThrowingException(ex.ToString());

                    this.serviceHost.Abort();

                    if (!(ex is CommunicationException ||
                          ex is TimeoutException ||
                          ex is InvalidOperationException))
                    {
                        throw;
                    }
                }

                this.serviceHost = null;
            }
        }

        public void Notify(IList<NotificationEvent> events)
        {
            // Assume each notification handling is fast so this can block
            foreach (NotificationEvent evt in events)
            {
                try
                {
                    this.OnNotificationReceived(evt);
                }
                catch (Exception ex)
                {
                    //ManagementEtwProvider.Provider.EventWriteHandledException(ex.ToString());

                    if (Fx.IsFatal(ex))
                    {
                        throw;
                    }
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.Stop();
                }
            }

            this.disposed = true;
        }

        protected abstract void OnNotificationReceived(NotificationEvent notificationEvent);
    }
}
