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

        public static void SyntaxTest5(ref int x)
        {
            x = 10;
        }

        public static void SyntaxTest6(ref int x, AddressesSimple a)
        {
            int y = x;
            int z = 0;
            
            SyntaxTest6(ref z, a);

            SyntaxTest5(ref a.i);
            SyntaxTest5(ref AddressesSimple.z);
        }

        public static void SyntaxText7(ref int x, AddressesSimple a)
        {
            // test store instructions
            x = 10;
            z = 100;
            a.i = 1;
        }


       static int z;

       public int i;
    }
}
