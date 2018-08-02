using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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

        public static void Test1()
        {
            int var1 = 1;
            bool var2 = true;
            float var3 = 5.0f;
            string var4 = "hello boogie";

            Contract.Assert(var1 == 1);
            Contract.Assert(var2 == true);
            Contract.Assert(var3 == 5.0f);
            Contract.Assert(var4 == "hello boogie");
        }

        public static void Test2(AddressesSimple a)
        {
            if (a != null && a.i > 5)
            {
                Contract.Assert(a.i > 5);
            }
        }

        public static void Test3(AddressesSimple a)
        {
            Contract.Assume(a != null && a.i <= 5);

            if (a != null && a.i > 5)
            {
                Contract.Assert(a.i > 5);
            }
            else
            {
                Contract.Assert(false);
            }
        }

        public static void Test4(AddressesSimple a)
        {
            if (a != null)
            {
                Contract.Assert(a != null);
            }
        }
        public static void Test5(AddressesSimple a)
        {
            Contract.Assume(a != null);
            Contract.Assert(a != null);
        }
        public static void Test6(AddressesSimple a)
        {
            Contract.Assume(a != null);
            Contract.Assume(a.i > 5);
            Contract.Assert(a.i > 5);
        }

        public static void Test7(AddressesSimple a)
        {
            var c = new AddressesSimple();
            c.i = 5;

            var b = new AddressesSimple();
            b.i = 5;
            PassByRef(ref b.i);

            int h = 5;

            PassByRef(ref h);

            PassByRef(ref a.i);

            Contract.Assume(a != null);
            Contract.Assume(a.i == 5);
            Contract.Assume(a.i > 5);
            Contract.Assert(b.i > 5);
            Contract.Assert(c.i == 5);
        }

        public static void PassByRef(ref int a)
        {
            a = 10;
        }

        static int z;

        public int i;
    }
}
