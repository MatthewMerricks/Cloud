// <copyright file="FileSelectionService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.Services
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Certificate selection service
    /// </summary>
    public interface ICertificateStoreService
    {
        /// <summary>
        /// Gets a list of available certificate store locations
        /// </summary>
        /// <returns>A list of available certificate store locations</returns>
        IEnumerable<string> GetStoreLocations();

        /// <summary>
        /// Gets a list of available certificate stores
        /// </summary>
        /// <returns>A list of available certificate stores</returns>
        IEnumerable<string> GetStores();

        /// <summary>
        /// Gets a list of available certificates in an certificate store
        /// </summary>
        /// <param name="location">A valid certificate store location</param>
        /// <param name="store">A valid certificate store</param>
        /// <returns>A list of available certificates in an store</returns>
        IEnumerable<string> GetCertificates(string location, string store);
    }

    /// <summary>
    /// Certificate selection service implementation
    /// </summary>
    public class CertificateStoreService : ICertificateStoreService
    {
        /// <summary>
        /// Gets a list of available certificate store locations
        /// </summary>
        /// <returns>A list of available certificate store locations</returns>
        public IEnumerable<string> GetStoreLocations()
        {
            return Enum.GetNames(typeof(StoreLocation));
        }

        /// <summary>
        /// Gets a list of available certificate stores
        /// </summary>
        /// <returns>A list of available certificate stores</returns>
        public IEnumerable<string> GetStores()
        {
            return Enum.GetNames(typeof(StoreName));
        }

        /// <summary>
        /// Gets a list of available certificates in an certificate store
        /// </summary>
        /// <param name="location">A valid certificate store location</param>
        /// <param name="store">A valid certificate store</param>
        /// <returns>A list of available certificates in an store</returns>
        public IEnumerable<string> GetCertificates(string location, string store)
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new ArgumentNullException("location");
            }

            if (string.IsNullOrEmpty(store))
            {
                throw new ArgumentNullException("store");
            }

            var typedLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), location, true);
            var typedStore = (StoreName)Enum.Parse(typeof(StoreName), store, true);

            var certificateStore = new X509Store(typedStore, typedLocation);
            try
            {
                certificateStore.Open(OpenFlags.ReadOnly);

                var certificates = new List<string>();
                foreach (var certificate in certificateStore.Certificates)
                {
                    certificates.Add(certificate.SubjectName.Name);
                }

                return certificates;
            }
            finally
            {
                certificateStore.Close();
            }
        }
    }
}

