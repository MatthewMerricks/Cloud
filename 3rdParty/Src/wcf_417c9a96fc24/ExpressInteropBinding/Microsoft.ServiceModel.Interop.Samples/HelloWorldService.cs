// <copyright file="InteropSecurityMode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Samples
{
    /// <summary>
    /// Sample service implementation
    /// </summary>
    public class HelloWorldService : IHelloWorld
    {
        /// <summary>
        /// Operation implementation
        /// </summary>
        /// <param name="name">Name to be used in the implementation</param>
        /// <returns>A hello world string</returns>
        public string Hello(string name)
        {
            return "Hello " + name;
        }
    }
}

