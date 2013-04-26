//
// InternalCLExceptions.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    /// <summary>
    /// Use in place of ArgumentException
    /// </summary>
    internal class CLArgumentException : CLException
    {
        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLArgumentException(
            CLExceptionCode code,
            string message,
            params Exception[] original)
            : base(code, message, original) { }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLArgumentException(
            CLExceptionCode code,
            string message,
            IEnumerable<Exception> innerExceptions)
            : base(code, message, innerExceptions) { }
    }

    /// <summary>
    /// Use in place of ArgumentNullException
    /// </summary>
    internal class CLArgumentNullException : CLArgumentException
    {
        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLArgumentNullException(
            CLExceptionCode code,
            string message,
            params Exception[] original)
            : base(code, message, original) { }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLArgumentNullException(
            CLExceptionCode code,
            string message,
            IEnumerable<Exception> innerExceptions)
            : base(code, message, innerExceptions) { }
    }

    /// <summary>
    /// Use in place of NullReferenceException
    /// </summary>
    internal sealed class CLNullReferenceException : CLException
    {
        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLNullReferenceException(
            CLExceptionCode code,
            string message,
            params Exception[] original)
            : base(code, message, original) { }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLNullReferenceException(
            CLExceptionCode code,
            string message,
            IEnumerable<Exception> innerExceptions)
            : base(code, message, innerExceptions) { }
    }

    /// <summary>
    /// Use in place of SystemException
    /// </summary>
    internal class CLSystemException : CLException
    {
        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLSystemException(
            CLExceptionCode code,
            string message,
            params Exception[] original)
            : base(code, message, original) { }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLSystemException(
            CLExceptionCode code,
            string message,
            IEnumerable<Exception> innerExceptions)
            : base(code, message, innerExceptions) { }
    }

    /// <summary>
    /// Use in place of InvalidOperationException
    /// </summary>
    internal class CLInvalidOperationException : CLSystemException
    {
        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLInvalidOperationException(
            CLExceptionCode code,
            string message,
            params Exception[] original)
            : base(code, message, original) { }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLInvalidOperationException(
            CLExceptionCode code,
            string message,
            IEnumerable<Exception> innerExceptions)
            : base(code, message, innerExceptions) { }
    }

    /// <summary>
    /// Use in place of ObjectDisposedException
    /// </summary>
    internal sealed class CLObjectDisposedException : CLInvalidOperationException
    {
        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLObjectDisposedException(
            CLExceptionCode code,
            string message,
            params Exception[] original)
            : base(code, message, original) { }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLObjectDisposedException(
            CLExceptionCode code,
            string message,
            IEnumerable<Exception> innerExceptions)
            : base(code, message, innerExceptions) { }
    }

    /// <summary>
    /// Use in place of IOException
    /// </summary>
    internal class CLIOException : CLSystemException
    {
        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLIOException(
            CLExceptionCode code,
            string message,
            params Exception[] original)
            : base(code, message, original) { }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLIOException(
            CLExceptionCode code,
            string message,
            IEnumerable<Exception> innerExceptions)
            : base(code, message, innerExceptions) { }
    }

    /// <summary>
    /// Use in place of PathTooLongException
    /// </summary>
    internal sealed class CLPathTooLongException : CLIOException
    {
        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLPathTooLongException(
            CLExceptionCode code,
            string message,
            params Exception[] original)
            : base(code, message, original) { }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLPathTooLongException(
            CLExceptionCode code,
            string message,
            IEnumerable<Exception> innerExceptions)
            : base(code, message, innerExceptions) { }
    }

    /// <summary>
    /// Use in place of NotSupportedException
    /// </summary>
    internal sealed class CLNotSupportedException : CLSystemException
    {
        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLNotSupportedException(
            CLExceptionCode code,
            string message,
            params Exception[] original)
            : base(code, message, original) { }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLNotSupportedException(
            CLExceptionCode code,
            string message,
            IEnumerable<Exception> innerExceptions)
            : base(code, message, innerExceptions) { }
    }

    /// <summary>
    /// Use in place of KeyNotFoundException
    /// </summary>
    internal sealed class CLKeyNotFoundException : CLSystemException
    {
        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLKeyNotFoundException(
            CLExceptionCode code,
            string message,
            params Exception[] original)
            : base(code, message, original) { }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLKeyNotFoundException(
            CLExceptionCode code,
            string message,
            IEnumerable<Exception> innerExceptions)
            : base(code, message, innerExceptions) { }
    }
}