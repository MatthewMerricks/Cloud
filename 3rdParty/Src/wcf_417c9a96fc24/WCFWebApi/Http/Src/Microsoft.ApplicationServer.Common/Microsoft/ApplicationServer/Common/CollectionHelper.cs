//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common
{
    using System.Collections.Generic;
    using System.Linq;
    using System;

    static class CollectionHelper
    {
        internal static IEnumerable<T> Reorder<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
            {
                yield break;
            }

            if (list.Count == 1)
            {
                yield return list[0];
                yield break;
            }

            Random random = new Random();
            int[] mapping = new int[list.Count];
            mapping[mapping.Length - 1] = mapping.Length;
            for (int i = 0; i < mapping.Length - 1; i++)
            {
                if (mapping[i] == 0)
                {
                    mapping[i] = i + 1;
                }

                int next = random.Next(i, mapping.Length);
                if (next != i)
                {
                    if (mapping[next] == 0)
                    {
                        mapping[next] = mapping[i];
                        mapping[i] = next + 1;
                    }
                    else
                    {
                        mapping[next] ^= mapping[i];
                        mapping[i] ^= mapping[next];
                        mapping[next] ^= mapping[i];
                    }
                }

                yield return list[mapping[i] - 1];
            }

            yield return list[mapping[mapping.Length - 1] - 1];
        }
    }
}
