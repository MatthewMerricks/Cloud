using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ApplicationServer.Http.Dispatcher;

namespace Microsoft.ApplicationServer.HttpEnhancements.Tests
{
    public class TestHttpErrorHandler : HttpErrorHandler
    {
        protected override bool OnHandleError(Exception error)
        {
            throw new NotImplementedException();
        }

        protected override System.Net.Http.HttpResponseMessage OnProvideResponse(Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
