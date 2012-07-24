// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Configuration
{
    using System;
    using System.ComponentModel;
    using System.Configuration;
    using System.Reflection;
    using Microsoft.ApplicationServer.Common;

    internal class ServiceModelEnumValidator : ConfigurationValidatorBase
    {
        private Type enumHelperType;
        private MethodInfo isDefined;

        public ServiceModelEnumValidator(Type enumHelperType)
        {
            if (enumHelperType == null)
            {
                throw Fx.Exception.ArgumentNull("enumHelperType");
            }

            this.enumHelperType = enumHelperType;
            this.isDefined = this.enumHelperType.GetMethod("IsDefined", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }

        public override bool CanValidate(Type type)
        {
            return this.isDefined != null;
        }

        public override void Validate(object value)
        {
            bool retVal = (bool)this.isDefined.Invoke(null, new object[] { value });

            if (!retVal)
            {
                ParameterInfo[] isDefinedParameters = this.isDefined.GetParameters();
                throw Fx.Exception.AsError(new InvalidEnumArgumentException("value", (int)value, isDefinedParameters[0].ParameterType));
            }
        }
    }
}
