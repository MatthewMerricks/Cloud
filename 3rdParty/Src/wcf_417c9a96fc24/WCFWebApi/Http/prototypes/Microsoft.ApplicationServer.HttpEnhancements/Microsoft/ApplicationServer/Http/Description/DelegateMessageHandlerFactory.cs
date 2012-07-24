namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Net.Http;

    using Microsoft.ApplicationServer.Http.Channels;

    internal class DelegateMessageHandlerFactory : HttpMessageHandlerFactory
    {
        private Func<HttpMessageChannel, HttpMessageChannel> create;

        public DelegateMessageHandlerFactory(Func<HttpMessageChannel, HttpMessageChannel> create)
        {
            this.create = create;
        }

        protected override HttpMessageChannel OnCreate(HttpMessageChannel innerChannel)
        {
            return this.create(innerChannel);
        }
    }
}