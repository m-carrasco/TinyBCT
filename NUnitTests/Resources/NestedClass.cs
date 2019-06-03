using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class ClassA
    {
        public ClassA()
        {
            i = 10;
        }
        class NestedClassB
        {
            public int j;

            public NestedClassB()
            {
                j = 15;
            }
            class NestedClassC
            {
                public NestedClassC()
                {
                    k = 9;
                }
                public int k;
            }
        }

        public int i;
    }
}
