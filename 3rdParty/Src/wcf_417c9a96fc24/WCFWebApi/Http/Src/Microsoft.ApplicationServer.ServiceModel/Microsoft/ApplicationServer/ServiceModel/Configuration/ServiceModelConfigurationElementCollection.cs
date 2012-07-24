// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Configuration
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Text;
    using Microsoft.ApplicationServer.Common;

    public abstract class ServiceModelConfigurationElementCollection<ConfigurationElementType> : ConfigurationElementCollection
        where ConfigurationElementType : ConfigurationElement, new()
    {
        private ConfigurationElementCollectionType collectionType;
        private string elementName;

        internal ServiceModelConfigurationElementCollection(ConfigurationElementCollectionType collectionType, string elementName)
        {
            this.collectionType = collectionType;
            this.elementName = elementName;

            if (!String.IsNullOrEmpty(elementName))
            {
                this.AddElementName = elementName;
            }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return this.collectionType; }
        }

        protected override string ElementName
        {
            get
            {
                string retval = this.elementName;
                if (string.IsNullOrEmpty(retval))
                {
                    retval = base.ElementName;
                }

                return retval;
            }
        }

        public virtual ConfigurationElementType this[object key]
        {
            get
            {
                if (key == null)
                {
                    throw Fx.Exception.ArgumentNull("key");
                }

                ConfigurationElementType retval = (ConfigurationElementType)this.BaseGet(key);
                if (retval == null)
                {
                    throw Fx.Exception.AsError(new System.Collections.Generic.KeyNotFoundException(
                        SR.ConfigKeyNotFoundInElementCollection(
                        key.ToString())));
                }

                return retval;
            }

            set
            {
                if (this.IsReadOnly())
                {
                    this.Add(value);
                }

                if (value == null)
                {
                    throw Fx.Exception.ArgumentNull("value");
                }

                if (key == null)
                {
                    throw Fx.Exception.ArgumentNull("key");
                }

                if (this.GetElementKey(value).ToString().Equals((string)key, StringComparison.Ordinal))
                {
                    if (this.BaseGet(key) != null)
                    {
                        this.BaseRemove(key);
                    }

                    this.Add(value);
                }
                else
                {
                    throw Fx.Exception.ArgumentNull(SR.ConfigKeysDoNotMatch(
                        this.GetElementKey(value).ToString(),
                        key.ToString()));
                }
            }
        }

        public ConfigurationElementType this[int index]
        {
            get
            {
                return (ConfigurationElementType)this.BaseGet(index);
            }

            set
            {
                if (!this.IsReadOnly() && !this.ThrowOnDuplicate)
                {
                    if (this.BaseGet(index) != null)
                    {
                        this.BaseRemoveAt(index);
                    }
                }

                this.BaseAdd(index, value);
            }
        }

        public void Add(ConfigurationElementType element)
        {
            if (!this.IsReadOnly())
            {
                if (element == null)
                {
                    throw Fx.Exception.ArgumentNull("element");
                }
            }

            this.BaseAdd(element);
        }

        public void Clear()
        {
            this.BaseClear();
        }

        public virtual bool ContainsKey(object key)
        {
            if (key == null)
            {
                List<string> elementKeys = new List<string>();

                ConfigurationElement dummyElement = this.CreateNewElement();
                foreach (PropertyInformation propertyInfo in dummyElement.ElementInformation.Properties)
                {
                    if (propertyInfo.IsKey)
                    {
                        elementKeys.Add(propertyInfo.Name);
                    }
                }

                if (0 == elementKeys.Count)
                {
                    throw Fx.Exception.ArgumentNull("key");
                }
                else if (1 == elementKeys.Count)
                {
                    throw Fx.Exception.AsError(new ConfigurationErrorsException(SR.ConfigElementKeyNull(elementKeys[0])));
                }
                else
                {
                    StringBuilder elementKeysString = new StringBuilder();

                    for (int i = 0; i < elementKeys.Count - 1; i++)
                    {
                        elementKeysString = elementKeysString.Append(elementKeys[i] + ", ");
                    }

                    elementKeysString = elementKeysString.Append(elementKeys[elementKeys.Count - 1]);

                    throw Fx.Exception.AsError(new ConfigurationErrorsException(SR.ConfigElementKeysNull(elementKeys.ToString())));
                }
            }
            else
            {
                return null != this.BaseGet(key);
            }
        }

        public void CopyTo(ConfigurationElementType[] array, int start)
        {
            if (array == null)
            {
                throw Fx.Exception.ArgumentNull("array");
            }

            if (start < 0 || start >= array.Length)
            {
                throw Fx.Exception.Argument("start", SR.ConfigInvalidStartValue(array.Length - 1, start));
            }

            ((ICollection)this).CopyTo(array, start);
        }

        public int IndexOf(ConfigurationElementType element)
        {
            if (element == null)
            {
                throw Fx.Exception.ArgumentNull("element");
            }

            return this.BaseIndexOf(element);
        }

        public void Remove(ConfigurationElementType element) 
        {
            if (!this.IsReadOnly())
            {
                if (element == null)
                {
                    throw Fx.Exception.ArgumentNull("element");
                }
            }

            this.BaseRemove(this.GetElementKey(element));
        }

        public void RemoveAt(object key) 
        {
            if (!this.IsReadOnly())
            {
                if (key == null)
                {
                    throw Fx.Exception.ArgumentNull("key");
                }
            }

            this.BaseRemove(key);
        }

        public void RemoveAt(int index)
        {
            this.BaseRemoveAt(index);
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            if (!this.IsReadOnly() && !this.ThrowOnDuplicate)
            {
                object key = this.GetElementKey(element);

                if (this.ContainsKey(key))
                {
                    this.BaseRemove(key);
                }
            }

            base.BaseAdd(element);
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ConfigurationElementType();
        }
    }
}
