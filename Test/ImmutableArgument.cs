using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class ImmutableArgument
    {
        public void test1(int i)
        {
            int j;
            j = 900;
            i = 10;
            Contract.Assert(i == 10);
        }

        public void test2(int i)
        {
            int j;
            j = 900;
            i = 10;
            Contract.Assert(i == 11);
        }
    }
}
