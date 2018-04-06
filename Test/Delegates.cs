using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Delegates
    {
        /*           EXAMPLES OF SUPPORTED CODE              */

        public delegate int DelegateIntInt(int num);

        public static int plus(int x)
        {
            return x+1;
        }

        public static int minus(int x)
        {
            return x - 1;
        }

        public static void creation1()
        {
            Func<int, int> f = plus;
        }

        public static void creation2()
        {
            DelegateIntInt d = minus;
        }

        public static void invoke1(Func<int, int> f)
        {
            var r = f(1);

            //Contract.Assert(r == 0 || r == 2);
        }

        public static void invoke2(DelegateIntInt f)
        {
            var r = f(1);
            //Contract.Assert(r == 0 || r == 2);
        }

        public static void creation_invoke1()
        {
            Func<int, int> f = plus;
            var r = f(5);
            Contract.Assert(r == 6);
        }

        public static void creation_invoke2()
        {
            DelegateIntInt d = minus;
            var r = d(5);
            Contract.Assert(r == 4);
        }
    }
}
