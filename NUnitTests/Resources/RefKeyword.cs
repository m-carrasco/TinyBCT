using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class RefKeyword
    {
        public int field;

        public static void Ref(ref int a, ref int b)
        {
            a = 10;
            b = 11;
        }

        public static void RefKeyword4(ref int a, ref int b)
        {
            a = b;
            b = 3;
        }

        public void test(int j, bool i, Object o)
        {
            var a = i ? o : null;
            Console.Write(a);
        
        }

        static void Main()
        {
            //OutAction<int, int> a = OutFunc;

            int i = 16;
            int j = 18;
    
            Ref(ref i, ref j);

            Contract.Assert(i == 10 && j == 11);
        }

        static void TestField(RefKeyword r)
        {
            // r1 = r.field;
            Ref(ref r.field, ref r.field);
            Contract.Assert(r.field == 11);
        }
    }
}
