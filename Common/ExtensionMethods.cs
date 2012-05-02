using System.ComponentModel;
using System.Runtime.Serialization;
using System.IO;

namespace win_client.Common
{


    public static class ExtensionMethods
    {
        public static T DeepCopy<T>(this T oSource)
        {

            T oClone;
            DataContractSerializer dcs = new DataContractSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream())
            {
                dcs.WriteObject(ms, oSource);
                ms.Position = 0;
                oClone = (T)dcs.ReadObject(ms);
            }
            return oClone;
        }
    }

}
