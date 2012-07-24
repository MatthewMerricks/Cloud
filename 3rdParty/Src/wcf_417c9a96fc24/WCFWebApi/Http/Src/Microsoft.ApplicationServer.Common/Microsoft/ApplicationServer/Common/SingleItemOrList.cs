//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common
{
    using System.Collections.Generic;

    struct SingleItemOrList<T> where T : class
    {
        T item;
        List<T> list;

        public SingleItemOrList(T item)
        {
            Fx.Assert(item != null, "null item!");

            this.item = item;
            this.list = null;
        }

        public SingleItemOrList(IEnumerable<T> list)
        {
            Fx.Assert(list != null, "null list!");

            this.item = null;
            this.list = new List<T>(list);
        }

        public SingleItemOrList(int capacity)
        {
            this.item = null;
            this.list = new List<T>(capacity);
        }

        public bool IsSingleItem
        {
            get
            {
                return (this.item != null) && (this.list == null);
            }
        }

        public T Item
        {
            get
            {
                Fx.Assert((this.item != null) && (this.list == null), "This is expected to be a single item container!");

                return this.item;
            }
        }

        public IList<T> List
        {
            get
            {
                Fx.Assert((this.item == null) && (this.list != null), "This is expected to be a list container!");

                return this.list;
            }
        }
    }
}
