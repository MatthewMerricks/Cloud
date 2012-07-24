// <copyright file="DiagnosticUtility.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace System.Json
{
    internal static class DiagnosticUtility
    {
        internal static bool IsFatal(Exception exception)
        {
            while (exception != null)
            {
                if (exception is OutOfMemoryException ||
                    exception is AccessViolationException)
                {
                    return true;
                }

                // These exceptions aren't themselves fatal, but since the CLR uses them to wrap other exceptions,
                // we want to check to see whether they've been used to wrap a fatal exception.  If so, then they
                // count as fatal.
                if (exception is TypeInitializationException)
                {
                    exception = exception.InnerException;
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        internal static class ExceptionUtility
        {
            public static Exception ThrowHelperError(Exception e)
            {
                return e;
            }

            public static ArgumentException ThrowHelperArgument(string message)
            {
                return new ArgumentException(message);
            }

            public static ArgumentException ThrowHelperArgument(string paramName, string message)
            {
                return new ArgumentException(message, paramName);
            }

            public static ArgumentNullException ThrowHelperArgumentNull(string paramName)
            {
                return new ArgumentNullException(paramName);
            }
        }
    }
}
