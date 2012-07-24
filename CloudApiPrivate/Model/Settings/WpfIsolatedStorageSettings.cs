using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CloudApiPrivate.Model.Settings
{

    /// <summary>
    /// This class is implemented to store user settings in an Isolated storage file.
    /// </summary>
    public class IsolatedStorageSettings : IDictionary<string, Object>
    {
        #region Constants/Variables
        static Dictionary<string, object> appDictionary = new Dictionary<string, object>();
        static IsolatedStorageSettings _isolatedStorageSettings = new IsolatedStorageSettings();
        const string filename = "CustomIsolatedStorage.bin";
        #endregion

        #region Singleton Implementation

        /// <summary>
        /// Its a private constructor.
        /// </summary>
        private IsolatedStorageSettings()
        {

        }

        /// <summary>
        /// Its a static singleton instance.
        /// </summary>
        public static IsolatedStorageSettings Instance
        {
            get
            {
                return _isolatedStorageSettings;
            }
        }

        /// <summary>
        /// Its static constructor.
        /// </summary>
        static IsolatedStorageSettings()
        {
            LoadData();
        }

        private static void LoadData()
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

            if(isoStore.GetFileNames(filename).Length == 0)
            {
                // File not exists. Let us NOT try to DeSerialize it.        
                return;
            }

            // Read the stream from Isolated Storage.    
            Stream stream = new IsolatedStorageFileStream(filename, FileMode.OpenOrCreate, isoStore);
            if(stream != null)
            {
                try
                {
                    // DeSerialize the Dictionary from stream.            
                    IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    Dictionary<string, Object> appData = (Dictionary<string, Object>)formatter.Deserialize(stream);

                    // Enumerate through the collection and load our Dictionary.            
                    IDictionaryEnumerator enumerator = appData.GetEnumerator();
                    while(enumerator.MoveNext())
                    {
                        appDictionary[enumerator.Key.ToString()] = enumerator.Value;
                    }
                }
                finally
                {
                    stream.Close();
                }

            }
        }

        #endregion

        #region Methods
        /// <summary>
        /// It serializes dictionary in binary format and stores it in a binary file.
        /// </summary>
        public void Save()
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

            Stream stream = new IsolatedStorageFileStream(filename, FileMode.Create, isoStore);
            if(stream != null)
            {
                try
                {
                    // Serialize dictionary into the IsolatedStorage.            
                    IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    formatter.Serialize(stream, appDictionary);
                }
                finally
                {
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// It Checks if Dictionary object has item corresponding to passed key,
        /// if True then it returns that object else it returns default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultvalue"></param>
        /// <returns></returns>
        public object this[string key, Object defaultvalue]
        {
            get
            {
                if(appDictionary.ContainsKey(key))
                {
                    return appDictionary[key];
                }
                else
                {
                    return defaultvalue;
                }
            }
            set
            {
                appDictionary[key] = value;
                Save();
            }
        }

        #endregion

        #region IDictionary<string, object> Members

        public void Add(string key, object value)
        {
            appDictionary.Add(key, value);
            Save();
        }

        public bool ContainsKey(string key)
        {
            return appDictionary.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return appDictionary.Keys; }
        }

        public bool Remove(string key)
        {
            try
            {
                Save();
                appDictionary.Remove(key);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryGetValue(string key, out object value)
        {
            return appDictionary.TryGetValue(key, out value);
        }

        public bool TryGetValue<TT>(string key, out TT value)
        {
            object tempValue;
            value = default(TT);
            bool rc = appDictionary.TryGetValue(key, out tempValue);
            if(rc)
            {
                value = (TT)tempValue;
            }
            return rc;
        }

        public ICollection<object> Values
        {
            get { return appDictionary.Values; }
        }

        public object this[string key]
        {
            get
            {
                return appDictionary[key];
            }
            set
            {
                appDictionary[key] = value;
                Save();
            }
        }


        public void Add(KeyValuePair<string, object> item)
        {
            appDictionary.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            appDictionary.Clear();
            Save();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return appDictionary.ContainsKey(item.Key);
        }

        public bool Contains(string key)
        {
            return appDictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return appDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return appDictionary.Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return appDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return appDictionary.GetEnumerator();
        }

        #endregion
    }
}