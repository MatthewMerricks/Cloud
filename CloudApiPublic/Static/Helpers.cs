using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiPublic.Static
{
    public static class Helpers
    {
        public static object DefaultForType(Type toDefault)
        {
            Type findStruct = toDefault;
            while (true)
            {
                if (findStruct == typeof(ValueType))
                {
                    return Activator.CreateInstance(toDefault);
                }
                else if (findStruct == typeof(object))
                {
                    return null;
                }
                findStruct = findStruct.BaseType;
            }
        }
    }
}