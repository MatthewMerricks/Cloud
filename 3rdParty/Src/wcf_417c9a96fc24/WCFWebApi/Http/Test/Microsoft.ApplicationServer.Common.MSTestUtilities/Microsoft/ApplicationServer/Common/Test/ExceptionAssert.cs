// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.ApplicationServer.Common.Test
{
    using System;
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// MSTest utility for testing code that throws exceptions.
    /// </summary>
    public static class ExceptionAssert
    {
        /// <summary>
        /// Asserts that the code under test given by the <paramref name="codeThatThrows"/> parameter
        /// throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="paramName">The name of the null parameter that results in the <see cref="ArgumentException"/>.</param>
        /// <param name="codeThatThrows">The code under test that is expected to throw an <see cref="ArgumentException"/>. Should not be <c>null</c>.</param>
        public static void ThrowsArgument(string paramName, Action codeThatThrows)
        {
            Assert.IsNotNull(codeThatThrows, "The 'codeThatThrows' parameter should not be null.");

            Throws<ArgumentException>(codeThatThrows,
                (exception) =>
                {
                    if (paramName != null)
                    {
                        string actualParamName = exception.ParamName;
                        Assert.AreEqual(
                            paramName,
                            actualParamName,
                            string.Format(
                                "Expected exception to have paramName='{0}' but found instead '{1}'.",
                                paramName,
                                actualParamName));
                    }
                });
        }

        /// <summary>
        /// Asserts that the code under test given by the <paramref name="codeThatThrows"/> parameter
        /// throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that results in the <see cref="ArgumentException"/>.</param>
        /// <param name="expectedMessage">The message expected in the resulting <see cref="ArgumentException"/>.</param>
        /// <param name="codeThatThrows">The code under test that is expected to throw an <see cref="ArgumentException"/>.</param>
        public static void ThrowsArgument(string paramName, string expectedMessage, Action codeThatThrows)
        {
            Throws<ArgumentException>(codeThatThrows,
                (exception) =>
                {
                    if (paramName != null)
                    {
                        string actualParamName = exception.ParamName;
                        Assert.AreEqual(
                            paramName,
                            actualParamName,
                            string.Format(
                                "Expected exception to have paramName='{0}' but found instead '{1}'.",
                                paramName,
                                actualParamName));
                    }

                    if (expectedMessage != null)
                    {
                        Assert.IsFalse(string.IsNullOrWhiteSpace(expectedMessage), "Test usage error: empty message is not expected.");

                        string actualMessage = exception.Message;

                        // Note: ArgumentException.Message adds in newline + Parameter: name, which could be localized,
                        // so we only insist it begins with the message.
                        Assert.IsTrue(
                            actualMessage.StartsWith(expectedMessage),
                            string.Format(
                                "Expected exception to have Message='{0}' but found instead '{1}'.",
                                expectedMessage,
                                actualMessage));
                    }
                });
        }

        /// <summary>
        /// Asserts that the code under test given by the <paramref name="codeThatThrows"/> parameter
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="paramName">The name of the null parameter that results in the <see cref="ArgumentNullException"/>.</param>
        /// <param name="codeThatThrows">The code under test that is expected to throw an <see cref="ArgumentNullException"/>. Should not be <c>null</c>.</param>
        public static void ThrowsArgumentNull(string paramName, Action codeThatThrows)
        {
            Assert.IsNotNull(codeThatThrows, "The 'codeThatThrows' parameter should not be null.");

            Throws<ArgumentNullException>(codeThatThrows,
                (exception) =>
                {
                    if (paramName != null)
                    {
                        string actualParamName = exception.ParamName;
                        Assert.AreEqual(
                            paramName,
                            actualParamName,
                            string.Format(
                                "Expected exception to have paramName='{0}' but found instead '{1}'.",
                                paramName,
                                actualParamName));
                    }
                });
        }

        /// <summary>
        /// Asserts that the code under test given by the <paramref name="codeThatThrows"/> parameter
        /// throws an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        /// <param name="paramName">The name of the null parameter that results in the <see cref="ArgumentOutOfRangeException"/>.</param>
        /// <param name="codeThatThrows">The code under test that is expected to throw an <see cref="ArgumentOutOfRangeException"/>. Should not be <c>null</c>.</param>
        public static void ThrowsArgumentOutOfRange(string paramName, Action codeThatThrows)
        {
            Assert.IsNotNull(codeThatThrows, "The 'codeThatThrows' parameter should not be null.");

            Throws<ArgumentOutOfRangeException>(codeThatThrows,
                (exception) =>
                {
                    if (paramName != null)
                    {
                        string actualParamName = exception.ParamName;
                        Assert.AreEqual(
                            paramName,
                            actualParamName,
                            string.Format(
                                "Expected exception to have paramName='{0}' but found instead '{1}'.",
                                paramName,
                                actualParamName));
                    }
                });
        }

        /// <summary>
        /// Asserts that the code under test given by the <paramref name="codeThatThrows"/> parameter
        /// throws an <see cref="ObjectDisposedException"/>.
        /// </summary>
        /// <param name="paramName">The name of the object that results in the <see cref="ObjectDisposedException"/>.</param>
        /// <param name="codeThatThrows">The code under test that is expected to throw an <see cref="ObjectDisposedException"/>. Should not be <c>null</c>.</param>
        public static void ThrowsObjectDisposed(string objectName, Action codeThatThrows)
        {
            Assert.IsNotNull(codeThatThrows, "The 'codeThatThrows' parameter should not be null.");

            Throws<ObjectDisposedException>(codeThatThrows,
                (exception) =>
                {
                    if (objectName != null)
                    {
                        string actualParamName = exception.ObjectName;
                        Assert.AreEqual(
                            objectName,
                            actualParamName,
                            string.Format(
                                "Expected exception to have objectName='{0}' but found instead '{1}'.",
                                objectName,
                                actualParamName));
                    }
                });
        }

        /// <summary>
        /// Asserts that the code under test given by the <paramref name="codeThatThrows"/> parameter
        /// throws an <see cref="System.ComponentModel.InvalidEnumArgumentException"/>.
        /// </summary>
        /// <param name="argumentName">The name of the argument that results in the <see cref="System.ComponentModel.InvalidEnumArgumentException"/>.</param>
        /// <param name="invalidValue">The invalid value assigned to the argument.</param>
        /// <param name="enumClass">The type of the Enum of the argument that is assigned the invalid value.</param>
        /// <param name="codeThatThrows">The code under test that is expected to throw an <see cref="System.ComponentModel.InvalidEnumArgumentException"/>. Should not be <c>null</c>.</param>
        public static void ThrowsInvalidEnumArgument(string argumentName, int invalidValue, Type enumClass, Action codeThatThrows)
        {
            Throws<System.ComponentModel.InvalidEnumArgumentException>(
                string.Format("The value of argument '{0}' ({1}) is invalid for Enum type '{2}'.{3}Parameter name: {0}", argumentName, invalidValue, enumClass.Name, Environment.NewLine),
                codeThatThrows);
        }

        /// <summary>
        /// Asserts that an exception of type <typeparamref name="TException"/> is thrown
        /// and calls the caller back to do more fine-grained checking.
        /// </summary>
        /// <typeparam name="TException">The type of exception that must be thrown.</typeparam>
        /// <param name="noExceptionMessage">The message to assert if no exception is thrown.</param>
        /// <param name="codeThatThrows">Code that is expected to trigger the exception. Should not be <c>null</c>.</param>
        /// <param name="codeThatChecks">Code called after the exception is caught to do finer-grained checking. Should not be <c>null</c>.</param>
        public static void Throws<TException>(
                    string noExceptionMessage,
                     Action codeThatThrows,
                     Action<TException> codeThatChecks) where TException : Exception
        {
            Assert.IsNotNull(codeThatThrows, "The 'codeThatThrows' parameter should not be null.");
            Assert.IsNotNull(codeThatChecks, "The 'codeThatChecks' parameter should not be null.");

            Exception actualException = null;
            AssertFailedException assertFailedException = null;
            try
            {
                codeThatThrows();
            }
            catch (Exception exception)
            {
                actualException = exception;

                // Let assert failure in the callback escape these checks
                assertFailedException = exception as AssertFailedException;
                if (assertFailedException != null)
                {
                    throw;
                }
            }
            finally
            {
                if (assertFailedException == null)
                {
                    if (actualException == null)
                    {
                        string message = string.IsNullOrWhiteSpace(noExceptionMessage) ?
                            string.Format("Expected an exception of type '{0}' but no exception was thrown.", typeof(TException).FullName) :
                            noExceptionMessage;
                        Assert.Fail(message);
                    }

                    Assert.IsInstanceOfType(
                        actualException,
                        typeof(TException),
                        string.Format(
                            "Expected an exception of type '{0}' but encountered an exception of type '{1}' with the message: {2}.",
                            typeof(TException).FullName,
                            actualException.GetType().FullName,
                            actualException.Message));

                    codeThatChecks((TException)actualException);
                }
            }
        }

        /// <summary>
        /// Asserts that an exception of type <typeparamref name="TException"/> is thrown
        /// and calls the caller back to do more fine-grained checking.
        /// </summary>
        /// <typeparam name="TException">The type of exception that must be thrown.</typeparam>
        /// <param name="codeThatThrows">Code that is expected to trigger the exception. Should not be <c>null</c>.</param>
        /// <param name="codeThatChecks">Code called after the exception is caught to do finer-grained checking. Should not be <c>null</c>.</param>
        public static void Throws<TException>(
                     Action codeThatThrows,
                     Action<TException> codeThatChecks) where TException : Exception
        {
            Throws<TException>(null, codeThatThrows, codeThatChecks);
        }

        /// <summary>
        /// Asserts that an exception of type <typeparamref name="TException"/> is thrown
        /// and with a given exception message.
        /// </summary>
        /// <typeparam name="TException">The type of exception that must be thrown.</typeparam>
        /// <param name="noExceptionMessage">The message to assert if no exception is thrown.</param>
        /// <param name="expectedExceptionMessage">The message of the exception that is expected to be thrown.</param>
        /// <param name="codeThatChecks">Code called after the exception is caught to do finer-grained checking. Should not be <c>null</c>.</param>
        public static void Throws<TException>(
                     string noExceptionMessage,
                     string expectedExceptionMessage,
                     Action codeThatThrows) where TException : Exception
        {
            Throws(
                noExceptionMessage,
                codeThatThrows, 
                (TException e) => 
                {
                    if (expectedExceptionMessage != null)
                    {
                        Assert.IsTrue(expectedExceptionMessage.Length > 0, "Incorrect test usage: expectedException method cannot be empty.");
                        Assert.AreEqual(expectedExceptionMessage, e.Message, "Incorrect exception message.");
                    }; 
                });
        }

        /// <summary>
        /// Asserts that an exception of type <typeparamref name="TException"/> is thrown
        /// and with a given exception message.
        /// </summary>
        /// <typeparam name="TException">The type of exception that must be thrown.</typeparam>
        /// <param name="expectedExceptionMessage">The message of the exception that is expected to be thrown.</param>
        /// <param name="codeThatChecks">Code called after the exception is caught to do finer-grained checking. Should not be <c>null</c>.</param>

        public static void Throws<TException>(
                     string expectedExceptionMessage,
                     Action codeThatThrows) where TException : Exception
        {
            Throws<TException>(null, expectedExceptionMessage, codeThatThrows);
        }


        /// <summary>
        /// Asserts that an exception of type <typeparamref name="TException"/> is thrown
        /// and with a given exception message wrapped inside an exception of type <typeparam name="TWrapperException"/>.
        /// </summary>
        /// <typeparam name="TWrapperException">The type of exception that will wrap the expected exception.</typeparam>
        /// <typeparam name="TException">The type of exception that must be thrown.</typeparam>
        /// <param name="expectedExceptionMessage">The message of the exception that is expected to be thrown.  A <c>null</c> means don't check the message.</param>
        /// <param name="codeThatThrows">Code called to generate the exception.  Should not be <c>null</c>.</param>
        /// <param name="codeThatChecks">Code called after the exception is caught to do finer-grained checking.  A <c>null</c> is allowed.</param>
        public static void Throws<TWrapperException,TException>(
                     string expectedExceptionMessage,
                     Action codeThatThrows,
                     Action<TException> codeThatChecks) where TWrapperException : Exception where TException : Exception
        {
            Assert.IsNotNull(codeThatThrows, "The 'codeThatThrows' parameter should not be null.");

            Throws<TWrapperException>(
                string.Format("Expected to receive an exception of type '{0}' wrapped in one of type '{1}'. ", typeof(TException), typeof(TWrapperException)),
                codeThatThrows,
                (wrapperException) =>
                {
                    TException innerException = wrapperException.InnerException as TException;
                    Assert.IsNotNull(innerException, "InnerException of " + wrapperException + " was not of the expected type " + typeof(TException));
                    if (expectedExceptionMessage != null)
                    {
                        // ArgumentException appends param name and is not checked
                        if (innerException is ArgumentException)
                        {
                            Assert.IsTrue(innerException.Message.StartsWith(expectedExceptionMessage),
                                string.Format(
                                    "Exception message was incorrect.{0}Expected: <{1}>{0}Actual:    <{2}>",
                                    Environment.NewLine,
                                    expectedExceptionMessage,
                                    innerException.Message)
                                );
                        }
                        else
                        {
                            Assert.AreEqual(expectedExceptionMessage, innerException.Message, "Incorrect exception message.");
                        }
                    }

                    if (codeThatChecks != null)
                    {
                        codeThatChecks(innerException);
                    }
                });
        }

        /// <summary>
        /// Asserts that an exception of type <typeparamref name="TException"/> is thrown
        /// and wrapped inside an exception of type <typeparam name="TWrapperException"/>.
        /// </summary>
        /// <typeparam name="TWrapperException">The type of exception that will wrap the expected exception.</typeparam>
        /// <typeparam name="TException">The type of exception that must be thrown.</typeparam>
        /// <param name="codeThatThrows">Code called to generate the exception.  Should not be <c>null</c>.</param>
        /// <param name="codeThatChecks">Code called after the exception is caught to do finer-grained checking.  Should not be <c>null</c>.</param>
        public static void Throws<TWrapperException, TException>(
                     Action codeThatThrows,
                     Action<TException> codeThatChecks)
            where TWrapperException : Exception
            where TException : Exception
        {
            Throws<TWrapperException, TException>(null, codeThatThrows, codeThatChecks);
        }


        /// <summary>
        /// Asserts that an exception of type <typeparamref name="TException"/> is thrown
        /// and with a given exception message wrapped inside an exception of type <typeparam name="TWrapperException"/>.
        /// </summary>
        /// <typeparam name="TWrapperException">The type of exception that will wrap the expected exception.</typeparam>
        /// <typeparam name="TException">The type of exception that must be thrown.</typeparam>
        /// <param name="expectedExceptionMessage">The message of the exception that is expected to be thrown.   A <c>null</c> means don't check the message.</param>
        /// <param name="codeThatThrows">Code called to generate the exception.  Should not be <c>null</c>.</param>
        public static void Throws<TWrapperException, TException>(
                     string expectedExceptionMessage,
                     Action codeThatThrows)
            where TWrapperException : Exception
            where TException : Exception
        {
            Throws<TWrapperException,TException>(expectedExceptionMessage, codeThatThrows, null);
        }

        /// <summary>
        /// Asserts that an <see cref="ArgumentNullException"/> is thrown and wrapped inside
        /// an exception of type <typeparamref name="TWrapperException"/>.
        /// </summary>
        /// <typeparam name="TWrapperException">The type of exeption actually thrown that will wrap
        /// <see cref="ArgumentNullException"/> as it inner exception.</typeparam>
        /// <param name="paramName">The name of the parameter in the <see cref="ArgumentNullException"/>.</param>
        /// <param name="codeThatThrows">Code called to generate the exception.  Should not be <c>null</c>.</param>
        public static void ThrowsArgumentNull<TWrapperException>(string paramName, Action codeThatThrows) where TWrapperException : Exception
        {
            Throws<TWrapperException, ArgumentException>(
                null,
                codeThatThrows,
                (ae) => Assert.AreEqual(paramName, ae.ParamName, "Incorrect parameter name.")
                );
        }

        /// <summary>
        /// Asserts that an <see cref="ArgumentException"/> is thrown and wrapped inside
        /// an exception of type <typeparamref name="TWrapperException"/>.
        /// </summary>
        /// <typeparam name="TWrapperException">The type of exeption actually thrown that will wrap
        /// <see cref="ArgumentException"/> as it inner exception.</typeparam>
        /// <param name="paramName">The name of the parameter in the <see cref="ArgumentException"/>.</param>
        /// <param name="expectedExceptionMessage">The expected message of the exception.  A <c>null</c> means don't check the message.</param>
        /// <param name="codeThatThrows">Code called to generate the exception.  Should not be <c>null</c>.</param>
        public static void ThrowsArgument<TWrapperException>(string paramName, string expectedExceptionMessage, Action codeThatThrows) where TWrapperException : Exception
        {
            Throws<TWrapperException, ArgumentException>(
                expectedExceptionMessage,
                codeThatThrows,
                (ae) => Assert.AreEqual(paramName, ae.ParamName, "Incorrect parameter name.")
                );
        }
    }
}
