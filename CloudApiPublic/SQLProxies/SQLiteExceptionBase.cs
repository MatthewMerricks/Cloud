using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.SQLProxies
{
    /// <summary>
    /// SQLite exception class.
    /// </summary>
    public abstract class SQLiteExceptionBase : ExternalException, ISQLiteException
    {
        internal protected SQLiteExceptionBase(string message, Exception innerException)
            : base(message, innerException) { }

        #region ISQLiteException members

        /// <summary>
        /// Gets the associated SQLite return code for this exception as an System.Int32.
        /// For desktop versions of the .NET Framework, this property overrides the property
        /// of the same name within the System.Runtime.InteropServices.ExternalException
        /// class. This property returns the same underlying value as the System.Data.SQLite.SQLiteException.ReturnCode
        /// property.
        /// </summary>
        public int ErrorCode
        {
            get
            {
                return _errorCode;
            }
        }

        protected abstract int _errorCode { get; }

        /// <summary>
        /// Gets the associated SQLite return code for this exception as a System.Data.SQLite.SQLiteErrorCode.
        /// This property returns the same underlying value as the System.Data.SQLite.SQLiteException.ErrorCode
        /// property.
        /// </summary>
        public WrappedSQLiteErrorCode ReturnCode
        {
            get
            {
                return _returnCode;
            }
        }

        /// <summary>
        /// Abstract property for ISQLiteException.ReturnCode
        /// </summary>
        protected abstract WrappedSQLiteErrorCode _returnCode { get; }
        #endregion

        #region Exception overrides
        /// <summary>
        /// Returns a string that contains the HRESULT of the error.
        /// </summary>
        /// <returns>A string that represents the HRESULT.</returns>
        public override string ToString()
        {
            throw new NotImplementedException("SQLiteExceptionBase must be implemented in a superclass");
        }

        /// <summary>
        /// Gets a collection of key/value pairs that provide additional user-defined
        /// information about the exception.
        /// </summary>
        /// <returns>
        /// An object that implements the System.Collections.IDictionary interface and
        /// contains a collection of user-defined key/value pairs. The default is an
        /// empty collection.
        /// </returns>
        public override IDictionary Data
        {
            get
            {
                throw new NotImplementedException("SQLiteExceptionBase must be implemented in a superclass");
            }
        }

        /// <summary>
        /// Gets or sets a link to the help file associated with this exception.
        /// </summary>
        /// <returns>The Uniform Resource Name (URN) or Uniform Resource Locator (URL).</returns>
        public override string HelpLink
        {
            get
            {
                throw new NotImplementedException("SQLiteExceptionBase must be implemented in a superclass");
            }
            set
            {
                throw new NotImplementedException("SQLiteExceptionBase must be implemented in a superclass");
            }
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <returns>
        /// The error message that explains the reason for the exception, or an empty
        /// string("").
        /// </returns>
        public override string Message
        {
            get
            {
                throw new NotImplementedException("SQLiteExceptionBase must be implemented in a superclass");
            }
        }

        /// <summary>
        /// Gets or sets the name of the application or the object that causes the error.
        /// </summary>
        /// <returns>The name of the application or the object that causes the error.</returns>
        /// <exception cref="System.ArgumentException">The object must be a runtime System.Reflection object</exception>
        public override string Source
        {
            get
            {
                throw new NotImplementedException("SQLiteExceptionBase must be implemented in a superclass");
            }
            set
            {
                throw new NotImplementedException("SQLiteExceptionBase must be implemented in a superclass");
            }
        }

        /// <summary>
        /// Gets a string representation of the immediate frames on the call stack.
        /// </summary>
        /// <returns>A string that describes the immediate frames of the call stack.</returns>
        public override string StackTrace
        {
            get
            {
                throw new NotImplementedException("SQLiteExceptionBase must be implemented in a superclass");
            }
        }

        /// <summary>
        /// When overridden in a derived class, sets the System.Runtime.Serialization.SerializationInfo
        /// with information about the exception.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual
        /// information about the source or destination.
        /// </param>
        /// <exception cref="System.ArgumentNullException">The info parameter is a null reference (Nothing in Visual Basic).</exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException("SQLiteExceptionBase must be implemented in a superclass");
        }

        /// <summary>
        /// When overridden in a derived class, returns the System.Exception that is
        /// the root cause of one or more subsequent exceptions.
        /// </summary>
        /// <returns>
        /// The first exception thrown in a chain of exceptions. If the System.Exception.InnerException
        /// property of the current exception is a null reference (Nothing in Visual
        /// Basic), this property returns the current exception.
        /// </returns>
        public override Exception GetBaseException()
        {
            throw new NotImplementedException("SQLiteExceptionBase must be implemented in a superclass");
        }
        #endregion
    }
}