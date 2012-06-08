using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiPrivate.Common
{
    public class RecursiveRenameArgs<T> where T : class
    {
        public T Value { get; private set; }
        public RecursiveRenameArgs(T value)
        {
            this.Value = value;
        }
    }
}
