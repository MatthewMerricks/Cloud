// <copyright file="EnumValidator.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Configuration
{
    using System;
    using System.ComponentModel;
    using System.Configuration;
    using System.Json;
    using Microsoft.ServiceModel.Web;

    internal class EnumValidator : ConfigurationValidatorBase
    {
        private Type enumType;

        public EnumValidator(Type enumType)
        {
            this.enumType = enumType;
        }

        public override bool CanValidate(Type type)
        {
            return this.enumType.IsEnum;
        }

        public override void Validate(object value)
        {
            if (!Enum.IsDefined(this.enumType, value))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidEnumArgumentException("value", (int)value, this.enumType));
            }
        }
    }
}
