using System;
using System.Collections.Generic;
using Microsoft.Cci;

namespace TinyBCT.Comparers
{
    public class IFieldReferenceComparer : IEqualityComparer<IFieldReference>
    {
        public bool Equals(IFieldReference x, IFieldReference y)
        {
            return GetHashCode(x) == GetHashCode(y);
        }

        public int GetHashCode(IFieldReference x)
        {
            return (int)x.InternedKey;
        }
    }
}
