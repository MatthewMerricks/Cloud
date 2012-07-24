// <copyright file="InteropSecurityMode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Samples
{
    using System.IdentityModel.Selectors;

    /// <summary>
    /// Custom WCF username validator implementation that authenticates all users
    /// </summary>
    public class CustomUserNameValidator : UserNamePasswordValidator
    {
        /// <summary>
        /// Validates the provided username and password
        /// </summary>
        /// <param name="userName">Username to validate</param>
        /// <param name="password">Password to validate</param>
        public override void Validate(string userName, string password)
        {
        }
    }
}

