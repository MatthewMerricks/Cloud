// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel
{
    using System.ComponentModel;
    using System.ServiceModel;
    using Microsoft.ApplicationServer.Common;

    internal static class HostNameComparisonModeHelper
    {
        public static bool IsDefined(HostNameComparisonMode value)
        {
            return
                value == HostNameComparisonMode.StrongWildcard
                || value == HostNameComparisonMode.Exact
                || value == HostNameComparisonMode.WeakWildcard;
        }

        public static void Validate(HostNameComparisonMode value)
        {
            if (!IsDefined(value))
            {
                throw Fx.Exception.AsError(new InvalidEnumArgumentException("value", (int)value, typeof(HostNameComparisonMode)));
            }
        }
    }
}
