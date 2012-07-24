// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Configuration
{
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;
    using Microsoft.ApplicationServer.ServiceModel.Activation;

    /// <summary>
    /// Represents a configuration element that contains sub-elements that specify settings for using 
    /// the <see cref="Microsoft.ApplicationServer.Http.HttpBinding">HttpBinding</see> binding.
    /// </summary>
    public class HttpBindingCollectionElement : StandardBindingCollectionElement<HttpBinding, HttpBindingElement>
    {
        private const string SectionPath = "system.serviceModel/bindings";

        internal static HttpBindingCollectionElement GetBindingCollectionElement()
        {
            BindingsSection bindings = AspNetEnvironment.Current.GetConfigurationSection(SectionPath) as BindingsSection;

            if (bindings != null)
            {            
                foreach (BindingCollectionElement bindingCollection in bindings.BindingCollections)
                {
                    if (bindingCollection.BindingName == HttpBinding.CollectionElementName)
                    {
                        return bindingCollection as HttpBindingCollectionElement;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a default <see cref="Microsoft.ApplicationServer.Http.HttpBinding">HttpBinding</see> binding.
        /// </summary>
        /// <returns>
        /// A default <see cref="Microsoft.ApplicationServer.Http.HttpBinding">HttpBinding</see> binding.
        /// </returns>
        protected override Binding GetDefault()
        {
            return new HttpBinding();
        }
    }
}
