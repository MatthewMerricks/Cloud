using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// The exception that is thrown when an error occurs trying to serialize an event.
    /// </summary>
    [Serializable()]
    public class EventSerializationException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new EventSerializationException
        /// </summary>
        public EventSerializationException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new EventSerializationException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public EventSerializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new EventSerializationException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public EventSerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new EventSerializationException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object buffer about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected EventSerializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    /// <summary>
    /// The exception that is thrown when an error occurs trying to deserialize an event.
    /// </summary>
    [Serializable()]
    public class EventDeserializationException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new EventDeserializationException
        /// </summary>
        public EventDeserializationException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new EventDeserializationException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public EventDeserializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new EventDeserializationException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public EventDeserializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new EventDeserializationException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object buffer about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected EventDeserializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when an unsupported type is passed to be serialized.
    /// </summary>
    [Serializable()]
    public class EventTypeNotSupportedException : SystemException
    {
        /// <summary>
        /// Base constructor to create a new EventTypeNotSupportedException
        /// </summary>
        public EventTypeNotSupportedException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor to create a new EventTypeNotSupportedException
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public EventTypeNotSupportedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor to create a new EventTypeNotSupportedException
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public EventTypeNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor to create a new EventTypeNotSupportedException
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object buffer about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected EventTypeNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

}
