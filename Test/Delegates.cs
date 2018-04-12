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

        public static void foo()
        {
            var x = 5;
            Contract.Assert(x == 5);
        }

        public static int plus(int x)
        {
            return x+1;
        }

        public static int plus(int x, int y)
        {
            return x + y;
        }

        public static int plus(int x, int y, int z)
        {
            return x + y + z;
        }

        public static int minus(int x)
        {
            return x - 1;
        }

        public static bool foo(int x, Object o)
        {
            return true;
        }

        public static bool bar(int x, Object o)
        {
            return false;
        }

        public static void creation1()
        {
            Func<int, int> f = plus;
        }

        public static void creation2()
        {
            DelegateIntInt d = minus;
        }

        public static void test(int a, int b)
        {

        }

        public static void invoke1(Func<int, int> f)
        {
            // deberia invocarse plus o devolver no deterministico
            var r = f(1);

            //Contract.Assert(r == 0 || r == 2);
        }

        public static void invoke2(DelegateIntInt f)
        {
            // deberia invocarse minus o cualquier cosa
            var r = f(1);
            //Contract.Assert(r == 0 || r == 2);
        }

        public static void creation_invoke1()
        {
            // en este invoke deberia ser solo plus
            Func<int, int> f = plus;
            var r = f(5);
            Contract.Assert(r == 6);
        }

        public static void creation_invoke2()
        {
            // deberia estar solo minux
            DelegateIntInt d = minus;
            var r = d(5);
            Contract.Assert(r == 4);
        }

        public static void creation_invoke3()
        {
            Func<int, int, int, int> f = plus;
            // deberia estar solo plus
            var r = f(1, 2, 3);
            Contract.Assert(r == 6);
        } 

        public static void creation_invoke4()
        {
            Func<int, Object, bool> f1 = foo;
            var r = f1(5, null);
            Contract.Assert(r == true);

            Func<int, Object, bool> f2 = bar;
            r = f2(5, null);
            Contract.Assert(r == false);
        }

        public static void creation_invoke5()
        {
            Action t = foo;
            t();
        }

        public static void invoke4(Func<int, int> f)
        {
            f(1);
        }

        public static int foo(int x)
        {
            return x + x;
        }

        public static void invoke3()
        {
            invoke4(foo);
        }

        public void p()
        {
            Action f = p;
            f();

            f = dummy;
            f();
        }

        public void dummy()
        {
        }
    }
}
