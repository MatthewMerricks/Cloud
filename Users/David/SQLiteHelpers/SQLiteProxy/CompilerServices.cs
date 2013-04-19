using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Allows you to obtain the method or property name of the caller to the method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    internal sealed class CallerMemberNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the System.Runtime.CompilerServices.CallerMemberNameAttribute
        /// class.
        /// </summary>
        public CallerMemberNameAttribute() { }
    }

    /// <summary>
    /// Allows you to obtain the full path of the source file that contains the caller.
    /// This is the file path at the time of compile.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerFilePathAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the System.Runtime.CompilerServices.CallerFilePathAttribute
        /// class.
        /// </summary>
        public CallerFilePathAttribute() { }
    }
}