using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Microsoft.WebSolutionsPlatform.Event.PubSubManager
{
    /// <summary>
    /// The exception that is thrown when an error occurs during publish/subscribe. See 
    /// the inner exception.
    /// </summary>
    [Serializable()]
    public class PubSubException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new PubSubException
        /// </summary>
        public PubSubException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PubSubException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public PubSubException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object buffer about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected PubSubException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when an error occurs during publish/subscribe initialization. 
    /// See the inner exception.
    /// </summary>
    [Serializable()]
    public class PubSubInitializationException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new PubSubInitializationException
        /// </summary>
        public PubSubInitializationException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubInitializationException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PubSubInitializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubInitializationException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public PubSubInitializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubInitializationException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object buffer about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected PubSubInitializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when there is insufficient memory to instantiate the object.
    /// </summary>
    [Serializable()]
    public class PubSubInsufficientMemoryException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new PubSubInsufficientMemoryException
        /// </summary>
        public PubSubInsufficientMemoryException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubInsufficientMemoryException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PubSubInsufficientMemoryException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubInsufficientMemoryException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public PubSubInsufficientMemoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubInsufficientMemoryException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object buffer about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected PubSubInsufficientMemoryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when the event system service is not running.
    /// </summary>
    [Serializable()]
    public class PubSubQueueDoesNotExistException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new PubSubQueueDoesNotExistException
        /// </summary>
        public PubSubQueueDoesNotExistException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubQueueDoesNotExistException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PubSubQueueDoesNotExistException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubQueueDoesNotExistException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public PubSubQueueDoesNotExistException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubQueueDoesNotExistException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object buffer about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected PubSubQueueDoesNotExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when the event system queue is full.
    /// </summary>
    [Serializable()]
    public class PubSubQueueFullException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new PubSubQueueFullException
        /// </summary>
        public PubSubQueueFullException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubQueueFullException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PubSubQueueFullException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubQueueFullException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public PubSubQueueFullException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new PubSubQueueFullException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object buffer about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected PubSubQueueFullException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
