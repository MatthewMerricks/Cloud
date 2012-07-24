// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel
{
    using System.ComponentModel;
    using System.ServiceModel;
    using Microsoft.ApplicationServer.Common;

    internal static class TransferModeHelper
    {
        public static bool IsDefined(TransferMode transferMode)
        {
            return transferMode == TransferMode.Buffered || 
                   transferMode == TransferMode.Streamed ||
                   transferMode == TransferMode.StreamedRequest || 
                   transferMode == TransferMode.StreamedResponse;
        }

        public static bool IsRequestStreamed(TransferMode transferMode)
        {
            return transferMode == TransferMode.StreamedRequest || transferMode == TransferMode.Streamed;
        }

        public static bool IsResponseStreamed(TransferMode transferMode)
        {
            return transferMode == TransferMode.StreamedResponse || transferMode == TransferMode.Streamed;
        }

        public static void Validate(TransferMode value)
        {
            if (!IsDefined(value))
            {
                throw Fx.Exception.AsError(new InvalidEnumArgumentException("value", (int)value, typeof(TransferMode)));
            }
        }
    }
}
