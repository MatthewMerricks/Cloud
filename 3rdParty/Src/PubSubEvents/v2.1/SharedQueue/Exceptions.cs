using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Microsoft.WebSolutionsPlatform.Common
{
    /// <summary>
    /// The exception that is thrown when there is an error returned by the underlying queue.
    /// </summary>
    [Serializable()]
    public class SharedQueueException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new SharedQueueException
        /// </summary>
        public SharedQueueException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SharedQueueException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public SharedQueueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected SharedQueueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when an error occured attempting to create the queue.
    /// </summary>
    [Serializable()]
    public class SharedQueueInitializationException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new SharedQueueInitializationException
        /// </summary>
        public SharedQueueInitializationException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueInitializationException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SharedQueueInitializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueInitializationException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public SharedQueueInitializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueInitializationException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected SharedQueueInitializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when there is not enough memory allocate the underlying queue.
    /// </summary>
    [Serializable()]
    public class SharedQueueInsufficientMemoryException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new SharedQueueInsufficientMemoryException
        /// </summary>
        public SharedQueueInsufficientMemoryException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueInsufficientMemoryException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SharedQueueInsufficientMemoryException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueInsufficientMemoryException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public SharedQueueInsufficientMemoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueInsufficientMemoryException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected SharedQueueInsufficientMemoryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when the underlying queue cannot be opened. Check 
    /// to make sure the WspEventRouter service is running.
    /// </summary>
    [Serializable()]
    public class SharedQueueDoesNotExistException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new SharedQueueDoesNotExistException
        /// </summary>
        public SharedQueueDoesNotExistException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueDoesNotExistException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SharedQueueDoesNotExistException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueDoesNotExistException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public SharedQueueDoesNotExistException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueDoesNotExistException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected SharedQueueDoesNotExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when the queue is full.
    /// </summary>
    [Serializable()]
    public class SharedQueueFullException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new SharedQueueFullException
        /// </summary>
        public SharedQueueFullException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueFullException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SharedQueueFullException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueFullException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public SharedQueueFullException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new SharedQueueFullException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected SharedQueueFullException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
