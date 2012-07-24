namespace Microsoft.ServiceModel.Web.Test.Common
{
    using System;

    internal static class Log
    {
        public static void Info(string text, params object[] args)
        {
            Console.WriteLine(text, args);
        }
    }
}
