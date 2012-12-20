using CloudApiPublic.EventMessageReceiver;
using CloudApiPublic.Model;
using CloudApiPublic.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSdkSyncSample.ViewModels
{
    public class ViewModelLocator : IMessageSenderProvider
    {
        public IMessageSender IMessageSender
        {
            get
            {
                return MessageSender.Instance;
            }
        }
    }
}
