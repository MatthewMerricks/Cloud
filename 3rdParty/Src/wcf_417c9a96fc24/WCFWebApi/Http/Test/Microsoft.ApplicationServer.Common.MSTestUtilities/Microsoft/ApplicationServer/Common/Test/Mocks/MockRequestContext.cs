// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Mocks
{
    using System;
    using System.ServiceModel.Channels;

    public class MockRequestContext : RequestContext
    {
        private Message requestMessage;

        public override Message RequestMessage
        {
            get
            {
                return this.requestMessage;
            }
        }

        public Action<Message> OnReplyReceived { get; set; }

        public Message ReplyMessage { get; private set; }

        public bool AbortCalled { get; set; }

        public bool BeginReplyCalled { get; set; }

        public bool CloseCalled { get; set; }

        public bool EndReplyCalled { get; set; }

        public bool ReplyCalled { get; set; }

        public TimeSpan Timeout { get; private set; }

        public override void Abort()
        {
            this.AbortCalled = true;
        }

        public override IAsyncResult BeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.BeginReplyCalled = true;
            this.ReplyMessage = message;
            if (this.OnReplyReceived != null)
            {
                this.OnReplyReceived(message);
            }
            this.Timeout = timeout;
            return null;
        }

        public override IAsyncResult BeginReply(Message message, AsyncCallback callback, object state)
        {
            this.BeginReplyCalled = true;
            this.ReplyMessage = message;
            if (this.OnReplyReceived != null)
            {
                this.OnReplyReceived(message);
            }
            return null;
        }

        public override void Close(TimeSpan timeout)
        {
            this.CloseCalled = true;
            this.Timeout = timeout;
        }

        public override void Close()
        {
            this.CloseCalled = true;
        }

        public override void EndReply(IAsyncResult result)
        {
            this.EndReplyCalled = true;
        }

        public override void Reply(Message message, TimeSpan timeout)
        {
            this.ReplyCalled = true;
            this.ReplyMessage = message;
            if (this.OnReplyReceived != null)
            {
                this.OnReplyReceived(message);
            }
            this.Timeout = timeout;
        }

        public override void Reply(Message message)
        {
            this.ReplyCalled = true;
            this.ReplyMessage = message;
            if (this.OnReplyReceived != null)
            {
                this.OnReplyReceived(message);
            }
        }

        public void SetRequestMessage(Message message)
        {
            this.requestMessage = message;
        }
    }
}
