// <copyright file="BasicSettingsPageViewModel.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ServiceModel.Interop.ConfigurationWizard.Services;

    /// <summary>
    /// View model for the certificate selection view page
    /// </summary>
    public class CertificatePageViewModel : PageViewModelBase
    {
        private string storeLocation;
        private string store;
        private string certificate;
        private ICertificateStoreService certificateStore;
        private IEnumerable<string> certificates;

        /// <summary>
        /// Initializes a new instance of the CertificatePageViewModel class
        /// </summary>
        /// <param name="certificateStore">A valid instance of ICertificateStoreService for selecting an existing certificate</param>
        public CertificatePageViewModel(ICertificateStoreService certificateStore)
        {
            if (certificateStore == null)
            {
                throw new ArgumentNullException("certificateStore");
            }

            this.certificateStore = certificateStore;

            this.store = this.certificateStore.GetStores().First();
            this.storeLocation = this.certificateStore.GetStoreLocations().First();
            this.certificates = this.certificateStore.GetCertificates(this.storeLocation, this.store);
        }

        /// <summary>
        /// Gets the view display name
        /// </summary>
        public override string DisplayName
        {
            get { return "Certificate Selection"; }
        }

        /// <summary>
        /// Gets the list of certificate store locations
        /// </summary>
        public IEnumerable<string> StoreLocations
        {
            get { return this.certificateStore.GetStoreLocations(); }
        }

        /// <summary>
        /// Gets or sets the selected certificate store location
        /// </summary>
        public string StoreLocation
        {
            get
            {
                return this.storeLocation;
            }

            set
            {
                if (this.storeLocation != value)
                {
                    this.storeLocation = value;
                    this.OnStoreLocationChanged();
                }
            }
        }

        /// <summary>
        /// Gets the list of certificate stores
        /// </summary>
        public IEnumerable<string> Stores
        {
            get { return this.certificateStore.GetStores(); }
        }

        /// <summary>
        /// Gets or sets the selected certificate store
        /// </summary>
        public string Store
        {
            get
            {
                return this.store;
            }

            set
            {
                if (this.store != value)
                {
                    this.store = value;
                    this.OnStoreChanged();
                }
            }
        }

        /// <summary>
        /// Gets the list of certificates
        /// </summary>
        public IEnumerable<string> Certificates
        {
            get { return this.certificates; }
        }

        /// <summary>
        /// Gets or sets the selected certificate
        /// </summary>
        public string Certificate
        {
            get { return this.certificate; }
            set { SetValue(ref this.certificate, value, () => this.Certificate); }
        }

        internal override bool IsValid()
        {
            return !string.IsNullOrEmpty(this.Certificate);
        }

        private void OnStoreChanged()
        {
            this.certificates = this.certificateStore.GetCertificates(this.StoreLocation, this.Store);

            this.OnPropertyChanged(() => this.Store);
            this.OnPropertyChanged(() => this.Certificates);
        }

        private void OnStoreLocationChanged()
        {
            this.certificates = this.certificateStore.GetCertificates(this.StoreLocation, this.Store);

            this.OnPropertyChanged(() => this.StoreLocation);
            this.OnPropertyChanged(() => this.Certificates);
        }
    }
}
