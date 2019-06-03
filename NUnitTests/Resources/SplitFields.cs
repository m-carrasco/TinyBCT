using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class SplitFields
    {
        public class Foo
        {
            public int a;
            public bool b;
        }

        class Holds<T>
        {
            public class InnerHolds
            {
                public T otro_v;

                public T foo()
                {
                    return otro_v;
                }
            }

            public T test2(T value)
            {
                var i = new InnerHolds();
                i.otro_v = value;

                return i.foo();
            }

            public T v;
            public T test()
            {
                return v;
            }
        }

        public void test1()
        {
            var f = new Foo();
            f.a = 1;
            f.b = true;

            Contract.Assert(f.a == 1);
            Contract.Assert(f.b == true);
        }

        public void test2(Foo f)
        {
            Contract.Assume(f.a != 10);
            Contract.Assume(f.b == true);


            Contract.Assert(f.a != 10);
            Contract.Assert(f.b == true);
        }

        public void test3()
        {
            var h = new Holds<int>();
            h.v = 500;
            Contract.Assert(h.v == 500);
        }

        public void test4()
        {
            var h = new Holds<int>();
            h.v = 2;
            Contract.Assert(h.v == 2);
            Contract.Assert(h.test() == 2);
        }

        public void test5()
        {
            var h = new Holds<int>();
            h.v = 2;

            Contract.Assert(h.test2(h.v) == 2);
            Contract.Assert(h.test() == 2);
        }

        public void test6()
        {
            var h = new Holds<Holds<int>>();
            h.v = new Holds<int>();
            h.v.v = 16;

            Contract.Assert(h.test().test() == 16);
        }
    }
}
