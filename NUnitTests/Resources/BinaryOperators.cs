using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class BinaryOperators
    {
        public static void ModTest1()
        {
            int i = 10;
            int z = 5;

            int res = i % z;

            Contract.Assert(res == 0);
        }

        public static void ModTest2()
        {
            int i = 10;
            int z = 3;

            int res = i % z;

            Contract.Assert(res != 1);
        }

        int i;
        public void Test()
        {
            Contract.Assume(i != 0);
        }

        // not supported yet
        public static void ModTest3()
        {
            //float i = 10;
            //float z = 3;

            //float res = i % z;

            //Contract.Assert(res != 1);
        }

    }
}
