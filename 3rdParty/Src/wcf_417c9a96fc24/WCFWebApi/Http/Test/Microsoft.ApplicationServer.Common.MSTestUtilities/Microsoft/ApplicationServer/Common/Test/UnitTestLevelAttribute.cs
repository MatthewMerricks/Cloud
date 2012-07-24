// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test
{
    using System;

    /// <summary>
    /// Custom attribute used to indicate the level of verification to
    /// apply to a unit test class.
    /// </summary>
    [Serializable, AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class UnitTestLevelAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnitTestLevelAttribute"/> class.
        /// </summary>
        /// <param name="unitTestLevel"></param>
        public UnitTestLevelAttribute(UnitTestLevel unitTestLevel)
        {
            this.UnitTestLevel = unitTestLevel;
        }

        /// <summary>
        /// Gets the level of verification to apply to the unit test class
        /// marked with this <see cref="UnitTestLevelAttribute"/>.
        /// </summary>
        public UnitTestLevel UnitTestLevel { get; private set; }
    }
}
