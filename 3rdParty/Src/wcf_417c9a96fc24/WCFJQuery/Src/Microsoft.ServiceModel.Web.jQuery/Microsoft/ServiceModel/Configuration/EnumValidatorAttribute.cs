// <copyright file="EnumValidatorAttribute.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Configuration
{
    using System;
    using System.Configuration;

    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class EnumValidatorAttribute : ConfigurationValidatorAttribute
    {
        private Type enumType;

        public EnumValidatorAttribute(Type enumType)
        {
            this.EnumType = enumType;
        }

        public Type EnumType
        {
            get { return this.enumType; }
            set { this.enumType = value; }
        }

        public override ConfigurationValidatorBase ValidatorInstance
        {
            get { return new EnumValidator(this.enumType); }
        }
    }
}
