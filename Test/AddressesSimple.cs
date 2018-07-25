using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class AddressesSimple
    {
        public static void SyntaxTest1()
        {
            int x;
            x = 1;
            bool y;
            y = true;

            bool w = true;
        }

        public static void SyntaxTest2(int x, bool y, Object z)
        {
            y = true;
        }

        public static void SyntaxTest3(AddressesSimple a)
        {
            int i = a.i;
        }

        public static void SyntaxTest4()
        {
            int localVar = z;
        }

        static int z;

        int i;
    }
}
