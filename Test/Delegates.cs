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

        public abstract class Dog
        {
            int drinkLevel = 0;
            int eatLevel = 0;

            public int Drink()
            {
                drinkLevel = drinkLevel + 2;
                return drinkLevel;
            }

            public abstract int Eat();

            public abstract void Run();

            public static int Execute(Func<int> f)
            {
                return f();
            }
        }

        class FoxTerrier : Dog
        {
            int drinkLevel = 0;
            int eatLevel = 0;

            public override int Eat()
            {
                eatLevel = eatLevel + 1;
                return eatLevel;
            }

            public override void Run()
            {
                
            }
        }

        class AiredaleTerrier : Dog
        {
            int drinkLevel = 0;
            int eatLevel = 0;

            public override int Eat()
            {
                eatLevel = eatLevel + 10;
                return eatLevel;
            }

            public override void Run()
            {
                Contract.Assert(false);
            }
        }

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
             Contract.Assert(f(5) == 5);
        }

        public void generics4()
        {
            var g = new Generic<int>();
            //Contract.Assert(g.test(1) != 1);

            Func<int, int> f = g.test;
            Contract.Assert(f(5) != 5);
        }


        public delegate int DelegateIntInt(int num);

        // the idea is not to have any delegated created of this type
        public delegate int DelegateEmpty(int num);

        public static void EmtpyGroup(DelegateEmpty e)
        {
            // no deterministic
            Contract.Assert(e(1) == 10);
        }

        public static int invoke1(Func<int, int> f)
        {
            // it should invoke plus or return a no deterministic value.
            var r = f(1);
            return r;
        }

        public static void Delegates1_NoBugs()
        {
            var r = invoke1(plus);
            Contract.Assert(r == 2);
        }
        public static void Delegates1_Bugged()
        {
            var r = invoke1(plus);
            Contract.Assert(r != 2);
        }

        public static void Delegates2_NoBugs()
        {
            // it should invoke minus
            DelegateIntInt d = minus;
            var r = d(5);
            Contract.Assert(r == 4);
        }

        public static void Delegates2_Bugged()
        {
            // it should invoke minus
            DelegateIntInt d = minus;
            var r = d(5);
            Contract.Assert(r != 4);
        }

        public static void Delegates3_NoBugs()
        {
            Func<int, int, int, int> f = plus;
            // it should invoke plus
            var r = f(1, 2, 3);
            Contract.Assert(r == 6);
        }

        public static void Delegates3_Bugged()
        {
            Func<int, int, int, int> f = plus;
            // it should invoke plus
            var r = f(1, 2, 3);
            Contract.Assert(r != 6);
        }

        public static void Delegates4_NoBugs()
        {
            Func<int, Object, bool> f1 = foo;
            var r = f1(5, null);
            Contract.Assert(r == true);

            Func<int, Object, bool> f2 = bar;
            r = f2(5, null);
            Contract.Assert(r == false);
        }

        public static void Delegates4_Bugged()
        {
            Func<int, Object, bool> f1 = foo;
            var r = f1(5, null);
            Contract.Assert(r == true);

            Func<int, Object, bool> f2 = bar;
            r = f2(5, null);
            Contract.Assert(r == false);

            Contract.Assert(false);
        }

        public static void Delegates5_Bugged()
        {
            Action a = assert;
            a();
        }

        public static void Delegates5_NoBugs()
        {
            Action a = noAssert;
            a();
        }


        public static void Delegates6_NoBugs()
        {
            // it should invoke plus
            Func<int, int> f = plus;
            var p = f(5);
            Contract.Assert(p == 6);
        }

        public static void Delegates6_Bugged()
        {
            // it should invoke plus
            Func<int, int> f = plus;
            var p = f(5);
            Contract.Assert(p != 6);
        }

        public static void DelegatesDynamicDispatch1_NoBugs()
        {
            FoxTerrier d = new FoxTerrier();
            d.Eat();
            Contract.Assert(Dog.Execute(d.Eat) == 2) ;
        }

        public static void DelegatesDynamicDispatch1_Bugged()
        {
            FoxTerrier d = new FoxTerrier();
            d.Eat();
            Contract.Assert(Dog.Execute(d.Eat) != 2);
        }

        public static void DelegatesDynamicDispatch2_NoBugs()
        {
            FoxTerrier d = new FoxTerrier();
            d.Eat();
            Contract.Assert(Dog.Execute(d.Eat) == 2);
        }

        public static void DelegatesDynamicDispatch2_Bugged()
        {
            AiredaleTerrier d = new AiredaleTerrier();
            d.Eat();
            Contract.Assert(Dog.Execute(d.Eat) != 20);
        }

        public static void DelegatesDynamicDispatch3_NoBugs()
        {
            FoxTerrier d = new FoxTerrier();
            Contract.Assert(Dog.Execute(d.Drink) == 2);
        }

        public static void DelegatesDynamicDispatch3_Bugged()
        {
            AiredaleTerrier d = new AiredaleTerrier();
            Contract.Assert(Dog.Execute(d.Drink) != 2);
        }

        public static void DelegatesDynamicDispatch4_NoBugs()
        {
            AiredaleTerrier d1 = new AiredaleTerrier();
            FoxTerrier d2 = new FoxTerrier();

            DelegateDogAction a1 = d1.Run;
            DelegateDogAction a2 = d2.Run;

            a2();
        }

        public delegate void DelegateDogAction();

        public static void DelegatesDynamicDispatch4_Bugged()
        {
            AiredaleTerrier d1 = new AiredaleTerrier();
            FoxTerrier d2 = new FoxTerrier();

            DelegateDogAction a1 = d1.Run;
            DelegateDogAction a2 = d2.Run;

            a1();
        }

        public static void DelegatesDynamicDispatch5_Bugged(Dog g)
        {
            // this will raise an exception because a1 = d1.Run  and a2 = d2.Run will be detected from previous test
            // if those lines were not existed this would not crash
            DelegateDogAction a = g.Run;
            a();
        }

        public static void assert()
        {
            Contract.Assert(false);
        }

        public static void noAssert()
        {
            Contract.Assert(true);
        }
        
        public static bool foo(int x, Object o)
        {
            return true;
        }

        public static bool bar(int x, Object o)
        {
            return false;
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


        //************************************* 
        public static void foo()
        {
            var x = 5;
            Contract.Assert(x == 5);
        }

        public static int plus(int x)
        {
            return x+1;
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

        public static void invoke2(DelegateIntInt f)
        {
            // it should invoke minus or return a no deterministic value.
            var r = f(1);
            //Contract.Assert(r == 0 || r == 2);
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
