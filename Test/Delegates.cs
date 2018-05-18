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
        class Generic<T>
        {
            T identity(T t)
            {
                return t;
            }

            public T test(T t)
            {
                Func<T, T> f = identity;
                return f(t);
            }
        }

        public void generics1()
        {
            var g = new Generic<int>();
            Contract.Assert(g.test(1) == 1);

           // Func<int, int> f = g.test;
           // Contract.Assert(g.test(5) == 5);
        }

        public void generics2()
        {
            var g = new Generic<int>();
            Contract.Assert(g.test(1) != 1);

            // Func<int, int> f = g.test;
            // Contract.Assert(g.test(5) == 5);
        }

        public void generics3()
        {
            var g = new Generic<int>();
            //Contract.Assert(g.test(1) != 1);

             Func<int, int> f = g.test;
             Contract.Assert(g.test(5) == 5);
        }

        public void generics4()
        {
            var g = new Generic<int>();
            //Contract.Assert(g.test(1) != 1);

            Func<int, int> f = g.test;
            Contract.Assert(g.test(5) != 5);
        }

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

        public static int invoke1(Func<int, int> f)
        {
            // it should invoke plus or return a no deterministic value.
            var r = f(1);
            return r;
        }

        public static void invoke_invoke1_plus()
        {
            var r = invoke1(plus);
            Contract.Assert(r == 2);
        }

        public static void invoke2(DelegateIntInt f)
        {
            // it should invoke minus or return a no deterministic value.
            var r = f(1);
            //Contract.Assert(r == 0 || r == 2);
        }

        public static void creation_invoke1()
        {
            // it should invoke plus
            Func<int, int> f = plus;
            var p = f(5);
            Contract.Assert(p == 6);
        }

        public static void creation_invoke2()
        {
            // it should invoke minus
            DelegateIntInt d = minus;
            var r = d(5);
            Contract.Assert(r == 4);
        }

        public static void creation_invoke3()
        {
            Func<int, int, int, int> f = plus;
            // it should invoke plus
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
