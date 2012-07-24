// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.ApplicationServer.Common;

    internal class ActionOfStreamContent : HttpContent
    {
        private Action<object> actionOfObject;
        private Action<Stream> actionOfStream;

        public ActionOfStreamContent(Action<Stream> actionOfStream)
        {
            Fx.Assert(actionOfStream != null, "The 'actionOfStream' parameter should not be null.");
            this.actionOfStream = actionOfStream;
            this.actionOfObject = this.OnStreamAsObject;
        }

        protected override void SerializeToStream(Stream stream, TransportContext context)
        {
            Fx.Assert(stream != null, "The 'stream' parameter should not be null.");
            this.actionOfStream(stream);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            Fx.Assert(stream != null, "The 'stream' parameter should not be null.");
            return Task.Factory.StartNew(this.actionOfObject, stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        private void OnStreamAsObject(object streamAsObject)
        {
            Fx.Assert(streamAsObject != null, "The 'streamAsObject' parameter should not be null.");

            Stream stream = streamAsObject as Stream;
            Fx.Assert(stream != null, "The 'streamAsObject' parameter should have been a stream.");

            this.actionOfStream(stream);
        }
    }
}