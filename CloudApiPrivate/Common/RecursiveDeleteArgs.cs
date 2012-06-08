using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiPrivate.Common
{
    public class RecursiveDeleteArgs<T> where T : class
    {
        public T Value { get; private set; }
        public RecursiveDeleteArgs(T value)
        {
            this.Value = value;
        }
    }
}
