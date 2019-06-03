using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class UnaryOperations
    {
        public static void test1()
        {
            int x = 1;
            Contract.Assert(x * -1 == -x);
        }

        public static void test2(int x)
        {
            Contract.Assume(x > 0);
            Contract.Assert(x + (-x) == 0);
        }

        public static void test3()
        {
            int x = 10;
            int y = 1;
            y = -y;

            Contract.Assert(x + y == 9);
        }

        public static void test1_float()
        {
            float x = 1;
            Contract.Assert(x * -1 == -x);
        }

        public static void test2_float(float x)
        {
            Contract.Assume(x > 0);
            Contract.Assert(x + (-x) == 0);
        }

        public static void test3_float()
        {
            float x = 10;
            float y = 1;
            y = -y;

            Contract.Assert(x + y == 9);
        }

    }
}
