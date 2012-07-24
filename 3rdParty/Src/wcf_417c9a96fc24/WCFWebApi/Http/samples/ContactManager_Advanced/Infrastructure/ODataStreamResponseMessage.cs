using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContactManager_Advanced
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Data.OData;
    using System.Threading.Tasks;

    public class ODataStreamResponseMessage : IODataResponseMessageAsync
    {
        private readonly Stream stream;
        private IDictionary<string, string> headers;
        private int statusCode;
        private bool lockedHeaders = false;

        public ODataStreamResponseMessage(Stream stream)
        {
            this.stream = stream;
            this.headers = new Dictionary<string, string>();
        }

        public string GetHeader(string headerName)
        {
            string value;
            headers.TryGetValue(headerName, out value);
            return value;
        }

        public Task<Stream> GetStreamAsync()
        {
            StreamWriter sw = new StreamWriter(stream);
            TaskCompletionSource<Stream> completionSource = new TaskCompletionSource<Stream>();
            completionSource.SetResult(this.stream);
            return completionSource.Task;
        }

        public void SetHeader(string headerName, string headerValue)
        {
            if (lockedHeaders)
                throw new ODataException("Cannot set headers they have already been written to the stream");

            this.headers[headerName] = headerValue;
        }

        public IEnumerable<KeyValuePair<string, string>> Headers
        {
            get
            {
                return this.headers;
            }
        }

        public int StatusCode
        {
            get
            {
                return statusCode;
            }
            set
            {
                statusCode = value;
            }
        }


        public Stream GetStream()
        {
            return this.stream;
        }
    }

}