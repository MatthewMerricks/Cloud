// <copyright file="InteropSecurityMode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Samples
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Description;

    /// <summary>
    /// Sample service host
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main method
        /// </summary>
        public static void Main()
        {
            ServiceHost host = new ServiceHost(
                typeof(HelloWorldService),
                new Uri("http://localhost/HelloWorld"));

            host.Open();

            foreach (ServiceEndpoint endpoint in host.Description.Endpoints)
            {
                Console.WriteLine(Messages.Listening, endpoint.Address.Uri.AbsoluteUri);
            }

            Console.WriteLine(Messages.Exit);
            Console.ReadLine();

            host.Close();
        }
    }
}

