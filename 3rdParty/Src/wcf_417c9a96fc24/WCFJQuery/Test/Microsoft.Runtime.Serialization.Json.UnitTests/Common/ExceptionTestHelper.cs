namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public static class ExceptionTestHelper
    {
        public static void ExpectException<T>(Action action, string expectedMessage = null) where T : Exception
        {
            try
            {
                action();
                Assert.Fail("Error, action should have thrown {0}", typeof(T).FullName);
            }
            catch (T ex)
            {
                if (expectedMessage != null)
                {
                    Assert.AreEqual<string>(expectedMessage, ex.Message);
                }
                else
                {
                    Console.WriteLine("Caught expected exception with exception message: '{0}'", ex.Message);
                }
            }
        }
    }
}
