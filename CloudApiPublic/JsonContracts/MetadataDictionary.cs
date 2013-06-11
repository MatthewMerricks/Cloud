//
// MetadataDictionary.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    /// <summary>
    /// Dictionary type for serialization to server in the form string key to object value where values may be directly serializable or may themselves be inner dictionaries or arrays;
    /// Exposes all the properties as if it were an IDictionary&lt;string, object&gt; but does not implement any IEnumerable interface to prevent overriding serialization,
    /// Implicitly castable as <see cref="MetadataDictionary.MetadataDictionaryEnumerable"/> which implements IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt;
    /// </summary>
    [Obfuscation(Exclude = true)]
    [Serializable]
    [KnownType(typeof(MetadataDictionary))]
    [KnownType(typeof(object[]))]
    public sealed class MetadataDictionary : ISerializable// see note below before implementing any other interfaces!!!!

        //// do not implement the following interface, because it will break serialization via DataContractJsonSerialization
        //, IDictionary<string, object>
    {
        private readonly Dictionary<string, object> InnerDict;

        public MetadataDictionary()
        {
            this.InnerDict = new Dictionary<string, object>();
        }

        public MetadataDictionary(IDictionary<string, object> initialValues)
        {
            this.InnerDict = new Dictionary<string, object>(initialValues);
        }

        public IEnumerable<KeyValuePair<string, object>> GetEnumerable()
        {
            return (MetadataDictionaryEnumerable)this;
        }

        #region implicit conversion to IEnumerable<KeyValuePair<string, object>> since we cannot implement that interface
        /// <summary>
        /// Implicitly converts a MetadataDictionary to IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt; in the form of MetadataDictionaryEnumerable
        /// </summary>
        public static implicit operator MetadataDictionaryEnumerable(MetadataDictionary metadata)
        {
            // Null check and return for nulls
            if (metadata == null)
            {
                return null;
            }

            return new MetadataDictionaryEnumerable(metadata.InnerDict.GetEnumerator());
        }

        /// <summary>
        /// Wrapper implicitly converted from <see cref="MetadataDictionary"/> which allows for easy enumeration
        /// </summary>
        public sealed class MetadataDictionaryEnumerable : IEnumerable<KeyValuePair<string, object>>
        {
            private readonly IEnumerator<KeyValuePair<string, object>> enumerator;

            internal MetadataDictionaryEnumerable(IEnumerator<KeyValuePair<string, object>> enumerator)
            {
                this.enumerator = enumerator;
            }

            #region IEnumerable<KeyValuePair<string, object>> Members
            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return this.enumerator;
            }
            #endregion

            #region IEnumerable
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.enumerator;
            }
            #endregion
        }
        #endregion

        #region ISerializable members
        // why does this work?? (called upon ReadObject on a JsonDataContractSerializer on a response stream)
        protected MetadataDictionary(SerializationInfo info, StreamingContext context)
        {
            this.InnerDict = new Dictionary<string, object>();
            foreach (SerializationEntry toAdd in info)
            {
                this.InnerDict.Add(toAdd.Name, toAdd.Value);
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            foreach (string key in this.InnerDict.Keys)
            {
                object stringDictObject = this.InnerDict[key];

                stringDictObject = RecursiveDictionaryReplace(stringDictObject);

                info.AddValue(key, stringDictObject);
            }
        }

        private static object RecursiveDictionaryReplace(object stringDictObject)
        {
            if (stringDictObject != null
                && !(stringDictObject is MetadataDictionary))
            {
                Type stringDictObjectType = stringDictObject.GetType();

                if (stringDictObjectType.IsArray)
                {
                    if (stringDictObjectType.GetArrayRank() == 1
                        && stringDictObjectType.GetElementType() != typeof(object))
                    {
                        Array stringDictArray = (Array)stringDictObject;
                        object[] arrayAsObjects = new object[stringDictArray.LongLength];
                        stringDictArray.CopyTo(arrayAsObjects, 0);
                        stringDictObject = arrayAsObjects;
                    }
                }
                else
                {
                    foreach (Type implementedInterfaceType in stringDictObjectType.GetInterfaces())
                    {
                        if (implementedInterfaceType.IsGenericType
                            && implementedInterfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                        {
                            Type[] dictionaryGenericTypes = implementedInterfaceType.GetGenericArguments();
                            if (dictionaryGenericTypes[0] == typeof(string))
                            {
                                IDictionary<string, object> wrappedDict;
                                if (dictionaryGenericTypes[1] == typeof(object))
                                {
                                    wrappedDict = (IDictionary<string, object>)stringDictObject;
                                }
                                else
                                {
                                    wrappedDict = (IDictionary<string, object>)
                                        typeof(DictionaryWrapper<>)
                                            .MakeGenericType(dictionaryGenericTypes[1])
                                            .GetConstructor(new[]
                                                {
                                                    typeof(IDictionary<,>)
                                                        .MakeGenericType(typeof(string), dictionaryGenericTypes[1])
                                                })
                                            .Invoke(new[] { stringDictObject });
                                }

                                stringDictObject = new MetadataDictionary(wrappedDict);
                                break;
                            }
                        }
                    }
                }
            }

            object[] replaceDictionariesInInnerArray = stringDictObject as object[];
            if (replaceDictionariesInInnerArray != null)
            {
                for (long idx = 0; idx < replaceDictionariesInInnerArray.LongLength; idx++)
                {
                    replaceDictionariesInInnerArray[idx] = RecursiveDictionaryReplace(replaceDictionariesInInnerArray[idx]);
                }
            }

            return stringDictObject;
        }
        #endregion

        #region IDictionary<string, object> members
        public void Add(string key, object value)
        {
            this.InnerDict.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return this.InnerDict.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return this.InnerDict.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return this.TryGetValue(key, out value);
        }

        public object this[string key]
        {
            get
            {
                return this.InnerDict[key];
            }
            set
            {
                this.InnerDict[key] = value;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                return this.InnerDict.Keys;
            }
        }

        public ICollection<object> Values
        {
            get
            {
                return this.InnerDict.Values;
            }
        }
        #endregion

        #region ICollection<string, object> Members
        public void Add(KeyValuePair<string, object> item)
        {
            ((ICollection<KeyValuePair<string, object>>)this.InnerDict).Add(item);
        }

        public void Clear()
        {
            this.InnerDict.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)this.InnerDict).Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)this.InnerDict).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)this.InnerDict).Remove(item);
        }

        public int Count
        {
            get
            {
                return this.InnerDict.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return ((ICollection<KeyValuePair<string, object>>)this.InnerDict).IsReadOnly;
            }
        }
        #endregion

        #region IEnumerable<KeyValuePair<string, object>> Members
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return this.InnerDict.GetEnumerator();
        }
        #endregion

        //#region IEnumerable
        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return this.InnerDict.GetEnumerator();
        //}
        //#endregion

        internal sealed class DictionaryWrapper<T> : IDictionary<string, object>
        {
            private readonly IDictionary<string, T> inner;
            public DictionaryWrapper(IDictionary<string, T> wrapped)
            {
                this.inner = wrapped;
            }

            #region IDictionary<string, object> Members

            public void Add(string key, object value) { inner.Add(key, (T)value); }
            public bool ContainsKey(string key) { return inner.ContainsKey(key); }
            public ICollection<string> Keys { get { return inner.Keys; } }
            public bool Remove(string key) { return inner.Remove(key); }

            public bool TryGetValue(string key, out object value)
            {
                T temp;
                bool res = inner.TryGetValue(key, out temp);
                value = temp;
                return res;
            }

            public ICollection<object> Values { get { return inner.Values.Select(x => (object)x).ToArray(); } }

            public object this[string key]
            {
                get { return inner[key]; }
                set { inner[key] = (T)value; }
            }

            #endregion

            #region ICollection<KeyValuePair<string, object>> Members

            public void Add(KeyValuePair<string, object> item) { inner.Add(item.Key, (T)item.Value); }
            public void Clear() { inner.Clear(); }
            public bool Contains(KeyValuePair<string, object> item) { return inner.Contains(new KeyValuePair<string, T>(item.Key, (T)item.Value)); }
            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                if (array == null)
                {
                    throw new NullReferenceException("array cannot be null");
                }
                if (arrayIndex < 0)
                {
                    throw new ArgumentException("arrayIndex cannot be negative");
                }
                if (inner.Count > (array.Length - arrayIndex))
                {
                    throw new ArgumentException("not enough room in array starting at arrayIndex for the full length of the inner dictionary");
                }
                foreach (KeyValuePair<string, T> currentItem in inner)
                {
                    array[arrayIndex++] = new KeyValuePair<string, object>(currentItem.Key, currentItem.Value);
                }
            }
            public int Count { get { return inner.Count; } }
            public bool IsReadOnly { get { return false; } }
            public bool Remove(KeyValuePair<string, object> item) { return inner.Remove(item.Key); }

            #endregion

            #region IEnumerable<KeyValuePair<string, object>> Members

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                foreach (KeyValuePair<string, T> item in inner)
                {
                    yield return new KeyValuePair<string, object>(item.Key, item.Value);
                }
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                foreach (KeyValuePair<string, T> item in inner)
                {
                    yield return new KeyValuePair<string, object>(item.Key, item.Value);
                }
            }

            #endregion
        }
    }
}