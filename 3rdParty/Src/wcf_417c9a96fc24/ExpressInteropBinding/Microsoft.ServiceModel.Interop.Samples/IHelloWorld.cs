// <copyright file="InteropSecurityMode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Samples
{
    using System.ServiceModel;

    /// <summary>
    /// Service Contract
    /// </summary>
    [ServiceContract]
    public interface IHelloWorld
    {
        /// <summary>
        /// Operation contract
        /// </summary>
        /// <param name="name">Name to be used in the implementation</param>
        /// <returns>A hello world string</returns>
        [OperationContract]
        string Hello(string name);
    }
}

