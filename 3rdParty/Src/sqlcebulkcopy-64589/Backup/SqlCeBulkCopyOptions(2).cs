using System;

namespace ErikEJ.SqlCe
{
    /// <summary>
    /// Bitwise flag that specifies one or more options to use with an instance of SqlCeBulkCopy.
    /// This enumeration has a FlagsAttribute attribute that allows a bitwise combination of its member values.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ce"), Flags]
    public enum SqlCeBulkCopyOptions
    {
        /// <summary>
        /// No options enabled
        /// </summary>
        Default = 0x0,
        /// <summary>
        /// Preserve source identity values. When not specified, identity values are assigned by the destination.
        /// This is implemented by using 'SET IDENTITY_INSERT [table] ON' when enabled
        /// </summary>
        KeepIdentity = 0x1,
        /// <summary>
        /// Preserve null values in the destination table regardless of the settings for default values. When not specified, null values are replaced by default values where applicable.
        /// </summary>
        KeepNulls = 0x8,
    }
}
