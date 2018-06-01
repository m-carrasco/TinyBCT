using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class SplitFields
    {
        class Foo
        {
            public int a;
            public bool b;
        }

        public void test1()
        {
            var f = new Foo();
            f.a = 1;
            f.b = true;

            Contract.Assert(f.a == 1);
            Contract.Assert(f.b == true);
        }
    }
}
