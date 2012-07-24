// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test
{
    using System;

    /// <summary>
    /// Custom attribute used to indicate the type tested by
    /// a unit test class.
    /// </summary>
    [Serializable, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class UnitTestTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnitTestTypeAttribute"/> class.
        /// </summary>
        /// <param name="unitTestLevel"></param>
        public UnitTestTypeAttribute(Type type)
        {
            this.Type = type;
        }

        /// <summary>
        /// Gets the level of verification to apply to the unit test class
        /// marked with this <see cref="UnitTestLevelAttribute"/>.
        /// </summary>
        public Type Type { get; private set; }
    }
}
