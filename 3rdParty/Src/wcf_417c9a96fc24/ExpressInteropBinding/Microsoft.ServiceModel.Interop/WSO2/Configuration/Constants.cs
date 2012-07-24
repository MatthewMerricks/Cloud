// <copyright file="Constants.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Wso2.Configuration
{
    /// <summary>
    /// Configuration constants
    /// </summary>
    internal static class Constants
    {
        internal const string Security = "security";
        internal const string Pattern = "pattern";
        internal const string Binding = "binding";
        internal const string SecureConversationBootstrap = "secureConversationBootstrap";
        internal const string BindingSection = "system.serviceModel/bindings";
        internal const string WSO2InteropBinding = "wso2InteropBinding";
        internal const string DefaultPattern = Wso2InteropBindingPatters.UserNameOverTransport;
        internal const string DefaultBootstrap = "";
    }
}

