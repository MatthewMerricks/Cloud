// <copyright file="Guids.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Extension
{
    using System;

    /// <summary>
    /// Package identifiers
    /// </summary>
    internal static class GuidList
    {
        /// <summary>
        /// Package Ids
        /// </summary>
        public const string GuidMicrosoftServiceModelInteropExtensionPkgString = "bd93b1d0-867b-48c4-b1b7-ec8c3b9337d4";

        /// <summary>
        /// Package Ids
        /// </summary>
        public const string GuidMicrosoftServiceModelInteropExtensionCmdSetString = "7d3d3fe8-f555-4227-9e6c-40de2951a40e";

        /// <summary>
        /// Package Ids
        /// </summary>
        public static readonly Guid GuidMicrosoftServiceModelInteropExtensionCmdSet = new Guid(GuidMicrosoftServiceModelInteropExtensionCmdSetString);
    }
}
