using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Sync
{
    internal static class IEnumerableStreamExtensions
    {
        // extension method so that whenever CLError FileStreams are dequeued,
        // they can be disposed with a simple method call
        public static CLError DisposeAllStreams(this IEnumerable<Stream> allStreams)
        {
            CLError disposalError = null;
            if (allStreams != null)
            {
                foreach (Stream currentStream in allStreams)
                {
                    if (currentStream != null)
                    {
                        try
                        {
                            currentStream.Dispose();
                        }
                        catch (Exception ex)
                        {
                            disposalError += ex;
                        }
                    }
                }
            }
            return disposalError;
        }
    }
}