using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Assume
    {
        public void test(int x)
        {
            Contract.Assume(x < 6);
            Contract.Assert(x != 6);
        }
    }
}
