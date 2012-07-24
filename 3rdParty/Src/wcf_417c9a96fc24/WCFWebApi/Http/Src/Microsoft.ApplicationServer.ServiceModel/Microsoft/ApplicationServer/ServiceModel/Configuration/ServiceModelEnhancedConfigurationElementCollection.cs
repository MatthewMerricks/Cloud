// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Configuration
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.ServiceModel.Configuration;
    using System.ServiceModel.Diagnostics;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.ServiceModel.Configuration;

    public abstract class ServiceModelEnhancedConfigurationElementCollection<TConfigurationElement> : ServiceModelConfigurationElementCollection<TConfigurationElement>
        where TConfigurationElement : ConfigurationElement, new()
    {
        internal ServiceModelEnhancedConfigurationElementCollection(string elementName)
            : base(ConfigurationElementCollectionType.AddRemoveClearMap, elementName)
        {
            this.AddElementName = elementName;
        }

        protected override bool ThrowOnDuplicate
        {
            get { return false; }
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            if (null == element)
            {
                throw Fx.Exception.ArgumentNull("element");
            }

            // Is this a duplicate key?
            object newElementKey = this.GetElementKey(element);
            if (this.ContainsKey(newElementKey))
            {
                ConfigurationElement oldElement = this.BaseGet(newElementKey);
                if (null != oldElement)
                {
                    // Is oldElement present in the current level of config
                    // being manipulated (i.e. duplicate in same config file)
                    if (oldElement.ElementInformation.IsPresent)
                    {
                        throw Fx.Exception.AsError(new ConfigurationErrorsException(
                            SR.ConfigDuplicateKeyAtSameScope(this.ElementName, newElementKey)));
                    }

                    //// ALTERED_FOR_PORT
                    //// TODO: CSDMAIN 205175 -- reactivate when tracing and logging are available:
                    ////else if (DiagnosticUtility.ShouldTraceWarning)
                    ////{
                    ////    Dictionary<string, string> values = new Dictionary<string, string>(6);
                    ////    values.Add("ElementName", this.ElementName);
                    ////    values.Add("Name", newElementKey.ToString());
                    ////    values.Add("OldElementLocation", oldElement.ElementInformation.Source);
                    ////    values.Add("OldElementLineNumber", oldElement.ElementInformation.LineNumber.ToString(NumberFormatInfo.CurrentInfo));
                    ////    values.Add("NewElementLocation", element.ElementInformation.Source);
                    ////    values.Add("NewElementLineNumber", element.ElementInformation.LineNumber.ToString(NumberFormatInfo.CurrentInfo));

                    ////    DictionaryTraceRecord traceRecord = new DictionaryTraceRecord(values);
                    ////    TraceUtility.TraceEvent(TraceEventType.Warning,
                    ////        TraceCode.OverridingDuplicateConfigurationKey,
                    ////        SR.GetString(SR.TraceCodeOverridingDuplicateConfigurationKey),
                    ////        traceRecord,
                    ////        this,
                    ////        null);
                    ////}
                }
            }

            base.BaseAdd(element);
        }
    }
}
