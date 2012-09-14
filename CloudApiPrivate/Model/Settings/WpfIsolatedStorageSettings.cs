using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Policy;
using System.Security.Cryptography.X509Certificates;

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

        private static readonly byte[] EvidenceBytes = new byte[]
            {
#region random bytes certificate
                (byte)48, (byte)130, (byte)2, (byte)15, (byte)48, (byte)130, (byte)1, (byte)120,
                (byte)160, (byte)3, (byte)2, (byte)1, (byte)2, (byte)2, (byte)16, (byte)0,
                (byte)146, (byte)50, (byte)244, (byte)84, (byte)153, (byte)95, (byte)19, (byte)29,
                (byte)0, (byte)239, (byte)118, (byte)35, (byte)119, (byte)12, (byte)213, (byte)48,
                (byte)13, (byte)6, (byte)9, (byte)42, (byte)134, (byte)72, (byte)134, (byte)247,
                (byte)13, (byte)1, (byte)1, (byte)11, (byte)5, (byte)0, (byte)48, (byte)27,
                (byte)49, (byte)25, (byte)48, (byte)23, (byte)6, (byte)3, (byte)85, (byte)4,
                (byte)3, (byte)12, (byte)16, (byte)84, (byte)101, (byte)115, (byte)116, (byte)32,
                (byte)67, (byte)101, (byte)114, (byte)116, (byte)105, (byte)102, (byte)105, (byte)99,
                (byte)97, (byte)116, (byte)101, (byte)48, (byte)30, (byte)23, (byte)13, (byte)49,
                (byte)50, (byte)48, (byte)56, (byte)49, (byte)52, (byte)49, (byte)51, (byte)50,
                (byte)55, (byte)53, (byte)49, (byte)90, (byte)23, (byte)13, (byte)49, (byte)52,
                (byte)48, (byte)57, (byte)49, (byte)51, (byte)49, (byte)51, (byte)50, (byte)55,
                (byte)53, (byte)49, (byte)90, (byte)48, (byte)27, (byte)49, (byte)25, (byte)48,
                (byte)23, (byte)6, (byte)3, (byte)85, (byte)4, (byte)3, (byte)12, (byte)16,
                (byte)84, (byte)101, (byte)115, (byte)116, (byte)32, (byte)67, (byte)101, (byte)114,
                (byte)116, (byte)105, (byte)102, (byte)105, (byte)99, (byte)97, (byte)116, (byte)101,
                (byte)48, (byte)129, (byte)159, (byte)48, (byte)13, (byte)6, (byte)9, (byte)42,
                (byte)134, (byte)72, (byte)134, (byte)247, (byte)13, (byte)1, (byte)1, (byte)1,
                (byte)5, (byte)0, (byte)3, (byte)129, (byte)141, (byte)0, (byte)48, (byte)129,
                (byte)137, (byte)2, (byte)129, (byte)129, (byte)0, (byte)128, (byte)157, (byte)126,
                (byte)195, (byte)189, (byte)6, (byte)178, (byte)239, (byte)173, (byte)232, (byte)221,
                (byte)20, (byte)136, (byte)234, (byte)71, (byte)253, (byte)19, (byte)237, (byte)86,
                (byte)199, (byte)173, (byte)25, (byte)168, (byte)15, (byte)34, (byte)109, (byte)71,
                (byte)134, (byte)249, (byte)78, (byte)147, (byte)24, (byte)176, (byte)228, (byte)238,
                (byte)0, (byte)209, (byte)118, (byte)92, (byte)234, (byte)19, (byte)30, (byte)72,
                (byte)140, (byte)244, (byte)221, (byte)39, (byte)33, (byte)118, (byte)154, (byte)246,
                (byte)144, (byte)27, (byte)216, (byte)116, (byte)121, (byte)120, (byte)49, (byte)186,
                (byte)22, (byte)112, (byte)187, (byte)185, (byte)157, (byte)206, (byte)8, (byte)231,
                (byte)7, (byte)50, (byte)214, (byte)235, (byte)130, (byte)96, (byte)163, (byte)68,
                (byte)253, (byte)144, (byte)90, (byte)197, (byte)239, (byte)102, (byte)111, (byte)49,
                (byte)87, (byte)72, (byte)97, (byte)98, (byte)209, (byte)96, (byte)243, (byte)206,
                (byte)117, (byte)213, (byte)83, (byte)191, (byte)56, (byte)7, (byte)19, (byte)136,
                (byte)140, (byte)87, (byte)147, (byte)145, (byte)43, (byte)232, (byte)14, (byte)74,
                (byte)77, (byte)54, (byte)100, (byte)232, (byte)149, (byte)80, (byte)5, (byte)135,
                (byte)22, (byte)40, (byte)211, (byte)126, (byte)91, (byte)189, (byte)57, (byte)147,
                (byte)167, (byte)254, (byte)201, (byte)121, (byte)171, (byte)2, (byte)3, (byte)1,
                (byte)0, (byte)1, (byte)163, (byte)84, (byte)48, (byte)82, (byte)48, (byte)12,
                (byte)6, (byte)3, (byte)85, (byte)29, (byte)19, (byte)1, (byte)1, (byte)255,
                (byte)4, (byte)2, (byte)48, (byte)0, (byte)48, (byte)14, (byte)6, (byte)3,
                (byte)85, (byte)29, (byte)15, (byte)1, (byte)1, (byte)255, (byte)4, (byte)4,
                (byte)3, (byte)2, (byte)4, (byte)144, (byte)48, (byte)22, (byte)6, (byte)3,
                (byte)85, (byte)29, (byte)37, (byte)1, (byte)1, (byte)255, (byte)4, (byte)12,
                (byte)48, (byte)10, (byte)6, (byte)8, (byte)43, (byte)6, (byte)1, (byte)5,
                (byte)5, (byte)7, (byte)3, (byte)1, (byte)48, (byte)26, (byte)6, (byte)3,
                (byte)85, (byte)29, (byte)17, (byte)4, (byte)19, (byte)48, (byte)17, (byte)129,
                (byte)15, (byte)99, (byte)108, (byte)111, (byte)117, (byte)100, (byte)64, (byte)99,
                (byte)108, (byte)111, (byte)117, (byte)100, (byte)46, (byte)99, (byte)111, (byte)109,
                (byte)48, (byte)13, (byte)6, (byte)9, (byte)42, (byte)134, (byte)72, (byte)134,
                (byte)247, (byte)13, (byte)1, (byte)1, (byte)11, (byte)5, (byte)0, (byte)3,
                (byte)129, (byte)129, (byte)0, (byte)70, (byte)203, (byte)196, (byte)161, (byte)245,
                (byte)93, (byte)39, (byte)135, (byte)125, (byte)210, (byte)243, (byte)216, (byte)110,
                (byte)246, (byte)94, (byte)3, (byte)52, (byte)96, (byte)152, (byte)243, (byte)243,
                (byte)192, (byte)226, (byte)208, (byte)25, (byte)194, (byte)42, (byte)53, (byte)155,
                (byte)67, (byte)65, (byte)45, (byte)114, (byte)94, (byte)250, (byte)233, (byte)40,
                (byte)187, (byte)62, (byte)52, (byte)37, (byte)48, (byte)36, (byte)89, (byte)93,
                (byte)244, (byte)52, (byte)77, (byte)193, (byte)197, (byte)188, (byte)190, (byte)67,
                (byte)233, (byte)160, (byte)250, (byte)203, (byte)92, (byte)234, (byte)62, (byte)98,
                (byte)163, (byte)64, (byte)2, (byte)135, (byte)39, (byte)203, (byte)34, (byte)22,
                (byte)27, (byte)104, (byte)58, (byte)13, (byte)33, (byte)128, (byte)102, (byte)103,
                (byte)167, (byte)34, (byte)42, (byte)214, (byte)46, (byte)27, (byte)32, (byte)185,
                (byte)69, (byte)6, (byte)199, (byte)47, (byte)56, (byte)119, (byte)123, (byte)46,
                (byte)74, (byte)62, (byte)131, (byte)88, (byte)144, (byte)214, (byte)55, (byte)93,
                (byte)48, (byte)143, (byte)9, (byte)116, (byte)165, (byte)12, (byte)102, (byte)161,
                (byte)96, (byte)62, (byte)255, (byte)209, (byte)205, (byte)50, (byte)239, (byte)205,
                (byte)227, (byte)158, (byte)195, (byte)61, (byte)203, (byte)252, (byte)218, (byte)135,
                (byte)217, (byte)133, (byte)19
#endregion
            };

        private static Publisher ConstantPublisher = new Publisher(new X509Certificate(EvidenceBytes));

        private static Evidence ConstantEvidence = new Evidence(new EvidenceBase[] { ConstantPublisher }, new EvidenceBase[] { ConstantPublisher });

        private static Evidence CopiedAssemblyEvidence = System.Reflection.Assembly.GetAssembly(typeof(IsolatedStorageSettings)).Evidence;

        private static CloudApiPublic.Model.GenericHolder<bool> CopiedAssemblyEvidenceModified = new CloudApiPublic.Model.GenericHolder<bool>(false);

        private static Evidence GetRebuiltAssemblyEvidence
        {
            get
            {
                lock (CopiedAssemblyEvidenceModified)
                {
                    if (!CopiedAssemblyEvidenceModified.Value)
                    {
                        CopiedAssemblyEvidence.RemoveType(typeof(System.Security.Policy.Url));
                        CopiedAssemblyEvidence.Merge(ConstantEvidence);
                        CopiedAssemblyEvidenceModified.Value = true;
                    }
                    return CopiedAssemblyEvidence;
                }
            }
        }

        private static void LoadData()
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly,
                GetRebuiltAssemblyEvidence,
                typeof(Publisher),
                GetRebuiltAssemblyEvidence,
                typeof(Publisher));

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
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly,
                GetRebuiltAssemblyEvidence,
                typeof(Publisher),
                GetRebuiltAssemblyEvidence,
                typeof(Publisher));

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