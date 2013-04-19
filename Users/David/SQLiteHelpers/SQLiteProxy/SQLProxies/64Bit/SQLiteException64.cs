using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;

namespace Cloud.SQLProxies
{
    extern alias SQL64;
using Cloud.Model;

    /// <summary>
    /// SQLite exception class.
    /// </summary>
    internal sealed class SQLiteException64 : SQLiteExceptionBase
    {
        private readonly SQL64.System.Data.SQLite.SQLiteException baseObject;

        /// <summary>
        /// Public constructor for generating a SQLite error given the base error code
        /// </summary>
        /// <param name="returnCode">The SQLite error code to report</param>
        /// <param name="message">Extra text to go along with the error message text</param>
        public static SQLiteExceptionBase Construct(WrappedSQLiteErrorCode returnCode, string message)
        {
            try
            {
                SQL64.System.Data.SQLite.SQLiteErrorCode convertCode = (SQL64.System.Data.SQLite.SQLiteErrorCode)((int)returnCode);

                ConstructorInfo baseExceptionConstructor;
                lock (baseConstructorInfo)
                {
                    if (baseConstructorInfo.Value == null)
                    {
                        baseConstructorInfo.Value = baseExceptionConstructor = typeof(SQL64.System.Data.SQLite.SQLiteException)
                            .GetConstructor(new[] { typeof(SQL64.System.Data.SQLite.SQLiteErrorCode), typeof(string) });
                    }
                    else
                    {
                        baseExceptionConstructor = baseConstructorInfo.Value;
                    }
                }

                SQL64.System.Data.SQLite.SQLiteException baseException = (SQL64.System.Data.SQLite.SQLiteException)baseExceptionConstructor
                        .Invoke(new object[] { convertCode, message });

                return new SQLiteException64(baseException);
            }
            catch (SQL64.System.Data.SQLite.SQLiteException ex)
            {
                throw new SQLiteException64(ex);
            }
        }
        private static readonly GenericHolder<ConstructorInfo> baseConstructorInfo = new GenericHolder<ConstructorInfo>(null);

        internal SQLiteException64(SQL64.System.Data.SQLite.SQLiteException baseObject)
            : base(baseObject.Message, baseObject.InnerException)
        {
            this.baseObject = baseObject;
        }

        #region ISQLiteException members
        /// <summary>
        /// Property override for ISQLiteException.ErrorCode
        /// </summary>
        protected override int _errorCode
        {
            get
            {
                return (int)baseObject.ErrorCode;
            }
        }

        /// <summary>
        /// Property override for ISQLiteException.ReturnCode
        /// </summary>
        protected override WrappedSQLiteErrorCode _returnCode
        {
            get
            {
                PropertyInfo returnInfo;
                lock (baseReturnInfo)
                {
                    if (baseReturnInfo.Value == null)
                    {
                        Type errorEnumType = typeof(SQL64.System.Data.SQLite.SQLiteErrorCode);
                        baseReturnInfo.Value = returnInfo =
                            typeof(SQL64.System.Data.SQLite.SQLiteException)
                                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .First(currentProp => currentProp.PropertyType == errorEnumType
                                    && (currentProp.Name == "ReturnCode" || currentProp.Name == "ErrorCode"));

                            // apparently ReturnCode is actually ErrorCode with an ambiguous duplicate name??
                            //typeof(SQL64.System.Data.SQLite.SQLiteException)
                            //    .GetProperty("ReturnCode", BindingFlags.Instance | BindingFlags.Public);

                        //// the following doesn't work either??
                        //(PropertyInfo)((MemberExpression)((UnaryExpression)((Expression<Func<SQL64.System.Data.SQLite.SQLiteException, object>>)(member => member.ReturnCode)).Body).Operand).Member;
                    }
                    else
                    {
                        returnInfo = baseReturnInfo.Value;
                    }
                }

                SQL64.System.Data.SQLite.SQLiteErrorCode baseCode = (SQL64.System.Data.SQLite.SQLiteErrorCode)returnInfo.GetValue(baseObject, null);
                return (WrappedSQLiteErrorCode)((int)baseCode);
            }
        }
        private static readonly GenericHolder<PropertyInfo> baseReturnInfo = new GenericHolder<PropertyInfo>(null);
        #endregion

        #region Exception overrides
        /// <summary>
        /// Returns a string that contains the HRESULT of the error.
        /// </summary>
        /// <returns>A string that represents the HRESULT.</returns>
        public override string ToString()
        {
            return baseObject.ToString();
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
                return baseObject.Data;
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
                return baseObject.HelpLink;
            }
            set
            {
                baseObject.HelpLink = value;
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
                return baseObject.Message;
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
                return baseObject.Source;
            }
            set
            {
                baseObject.Source = value;
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
                return baseObject.StackTrace;
            }
        }

        /// <summary>
        /// Gets the method that throws the current exception.
        /// </summary>
        /// <returns>The System.Reflection.MethodBase that threw the current exception.</returns>
        public new MethodBase TargetSite
        {
            get
            {
                return baseObject.TargetSite;
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
            baseObject.GetObjectData(info, context);
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
            return baseObject.GetBaseException();
        }
        #endregion
    }
}