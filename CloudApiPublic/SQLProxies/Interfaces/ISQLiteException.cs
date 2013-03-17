//
// ISQLiteException.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

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
    public interface ISQLiteException : ISerializable, _Exception
    {
        /// <summary>
        /// Gets the associated SQLite return code for this exception as an System.Int32.
        /// For desktop versions of the .NET Framework, this property overrides the property
        /// of the same name within the System.Runtime.InteropServices.ExternalException
        /// class. This property returns the same underlying value as the System.Data.SQLite.SQLiteException.ReturnCode
        /// property.
        /// </summary>
        int ErrorCode { get; }

        /// <summary>
        /// Gets the associated SQLite return code for this exception as a System.Data.SQLite.SQLiteErrorCode.
        /// This property returns the same underlying value as the System.Data.SQLite.SQLiteException.ErrorCode
        /// property.
        /// </summary>
        WrappedSQLiteErrorCode ReturnCode { get; }

        /// <summary>
        /// Returns a string that contains the HRESULT of the error.
        /// </summary>
        /// <returns>A string that represents the HRESULT.</returns>
        new string ToString();

        /// <summary>
        /// Gets a collection of key/value pairs that provide additional user-defined
        /// information about the exception.
        /// </summary>
        /// <returns>
        /// An object that implements the System.Collections.IDictionary interface and
        /// contains a collection of user-defined key/value pairs. The default is an
        /// empty collection.
        /// </returns>
        IDictionary Data { get; }

        /// <summary>
        /// Gets or sets a link to the help file associated with this exception.
        /// </summary>
        /// <returns>The Uniform Resource Name (URN) or Uniform Resource Locator (URL).</returns>
        new string HelpLink { get; set; }

        /// <summary>
        /// Gets the System.Exception instance that caused the current exception.
        /// </summary>
        /// <returns>
        /// An instance of Exception that describes the error that caused the current
        /// exception. The InnerException property returns the same value as was passed
        /// into the constructor, or a null reference (Nothing in Visual Basic) if the
        /// inner exception value was not supplied to the constructor. This property
        /// is read-only.
        /// </summary>
        new Exception InnerException { get; }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <returns>
        /// The error message that explains the reason for the exception, or an empty
        /// string("").
        /// </returns>
        new string Message { get; }

        /// <summary>
        /// Gets or sets the name of the application or the object that causes the error.
        /// </summary>
        /// <returns>The name of the application or the object that causes the error.</returns>
        /// <exception cref="System.ArgumentException">The object must be a runtime System.Reflection object</exception>
        new string Source { get; set; }

        /// <summary>
        /// Gets a string representation of the immediate frames on the call stack.
        /// </summary>
        /// <returns>A string that describes the immediate frames of the call stack.</returns>
        new string StackTrace { get; }

        /// <summary>
        /// Gets the method that throws the current exception.
        /// </summary>
        /// <returns>The System.Reflection.MethodBase that threw the current exception.</returns>
        new MethodBase TargetSite { get; }

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
        new void GetObjectData(SerializationInfo info, StreamingContext context);

        /// <summary>
        /// When overridden in a derived class, returns the System.Exception that is
        /// the root cause of one or more subsequent exceptions.
        /// </summary>
        /// <returns>
        /// The first exception thrown in a chain of exceptions. If the System.Exception.InnerException
        /// property of the current exception is a null reference (Nothing in Visual
        /// Basic), this property returns the current exception.
        /// </returns>
        new Exception GetBaseException();
    }
}