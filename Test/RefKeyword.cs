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

        //static void OutFunc(out int a, out int b) { a = b = 0; }
        //public delegate void OutAction<T1, T2>(out T1 a, out T2 b);

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
    }
}
