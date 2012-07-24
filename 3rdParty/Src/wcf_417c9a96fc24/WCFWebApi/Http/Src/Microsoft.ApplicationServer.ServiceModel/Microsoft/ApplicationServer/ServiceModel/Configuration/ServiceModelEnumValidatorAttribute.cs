// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Configuration
{
    using System;
    using System.Configuration;

    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class ServiceModelEnumValidatorAttribute : ConfigurationValidatorAttribute
    {
        public ServiceModelEnumValidatorAttribute(Type enumHelperType)
        {
            this.EnumHelperType = enumHelperType;
        }

        public Type EnumHelperType { get; private set; }

        public override ConfigurationValidatorBase ValidatorInstance
        {
            get 
            { 
                return new ServiceModelEnumValidator(this.EnumHelperType); 
            }
        }
    }
}
