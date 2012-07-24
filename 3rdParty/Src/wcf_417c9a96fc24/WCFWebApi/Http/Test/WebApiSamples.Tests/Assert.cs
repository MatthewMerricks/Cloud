using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebApiSamples.Tests
{
    using System.Net.Http;

    public static class AssertHelpers
    {
        public static void Throws<T>(this object test, Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch(Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(T));
                return;
            }
            Assert.Fail("Expected to throw exception of type: {0}", typeof(T));
        } 

        public static void HasContentWithMediaType(this HttpResponseMessage response, string mediaType)
        {
            Assert.AreEqual(mediaType, response.Content.Headers.ContentType.MediaType);
        }
    }
}
