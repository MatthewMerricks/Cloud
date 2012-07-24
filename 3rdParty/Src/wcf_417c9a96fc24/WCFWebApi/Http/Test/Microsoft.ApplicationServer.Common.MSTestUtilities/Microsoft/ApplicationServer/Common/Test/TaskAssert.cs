// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Test assert class to make assertions about tests using <see cref="Task"/>.
    /// </summary>
    public static class TaskAssert
    {
        private static int timeOutMs = System.Diagnostics.Debugger.IsAttached ? TimeoutConstant.DefaultTimeout : TimeoutConstant.DefaultTimeout * 10;

        /// <summary>
        /// Asserts the given task has been started.  TAP guidelines are that all
        /// <see cref="Task"/> objects returned from public API's have been started.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to test.</param>
        public static void IsStarted(Task task)
        {
            Assert.IsNotNull(task, "Task was null.");
            Assert.IsTrue(task.Status != TaskStatus.Created, "Task has not been started.");
        }

        /// <summary>
        /// Asserts the given task completes successfully.  This method will block the
        /// current thread waiting for the task, but will timeout if it does not complete.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to test.</param>
        public static void Succeeds(Task task)
        {
            IsStarted(task);
            task.Wait(timeOutMs);
            AggregateException aggregateException = task.Exception;
            Exception innerException = aggregateException == null ? null : aggregateException.InnerException;
            Assert.IsNull(innerException, string.Format("Task failed to complete: {0}", innerException));
        }

        /// <summary>
        /// Asserts the given task completes successfully and returns a result.
        /// Use this overload for a generic <see cref="Task"/> whose generic parameter is not known at compile time.
        /// This method will block the current thread waiting for the task, but will timeout if it does not complete.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to test.</param>
        /// <returns>The result from that task.</returns>
        public static object SucceedsWithResult(Task task)
        {
            Succeeds(task);
            Assert.IsTrue(task.GetType().IsGenericType, "Task does not return a result.");
            Type[] genericArguments = task.GetType().GetGenericArguments();
            Assert.AreEqual(1, genericArguments.Length, "Task must have only 1 generic argument.");
            PropertyInfo resultProperty = task.GetType().GetProperty("Result");
            Assert.IsNotNull(resultProperty, "Cannot determine the Result property info.");
            return resultProperty.GetValue(task, null);
        }

        /// <summary>
        /// Asserts the given task completes successfully and returns a <typeparamref name="T"/> result.
        /// This method will block the current thread waiting for the task, but will timeout if it does not complete.
        /// </summary>
        /// <typeparam name="T">The result of the <see cref="Task"/>.</typeparam>
        /// <param name="task">The <see cref="Task"/> to test.</param>
        /// <returns>The result from that task.</returns>
        public static T SucceedsWithResult<T>(Task<T> task)
        {
            Succeeds(task);
            return task.Result;
        }

        /// <summary>
        /// Asserts the given <see cref="Task"/> completes successfully and yields
        /// the expected result.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to test.</param>
        /// <param name="expectedObj">The expected result.</param>
        public static void ResultEquals(Task task, object expectedObj)
        {
            object actualObj = SucceedsWithResult(task);
            TestDataAssert.AreEqual(expectedObj, actualObj, "Task returned the wrong result value.");
        }

        /// <summary>
        /// Asserts the given <see cref="Task"/> completes successfully and yields
        /// the expected result.
        /// </summary>
        /// <typeparam name="T">The type the task will return.</typeparam>
        /// <param name="task">The task to test.</param>
        /// <param name="expectedObj">The expected result.</param>
        public static void ResultEquals<T>(Task<T> task, T expectedObj)
        {
            T actualObj = SucceedsWithResult<T>(task);
            TestDataAssert.AreEqual(expectedObj, actualObj, "Task returned the wrong result value.");
        }
    }
}
