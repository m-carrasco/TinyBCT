using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Arrays
    {
        static void arrayStoreLoad1()
        {
            var a = new int[10];
            a[5] = 10;
            Contract.Assert(a[5] == 10);
        }

        static void arrayStoreLoad2()
        {
            var a = new int[10];
            a[5] = 10;
            Contract.Assert(a[5] == 9);
        }

        static void arrayStoreLoad3()
        {
            var a = new Foo[10];
            a[5] = new Foo(9);
            Contract.Assert(a[5].i == 9);
        }

        static void arrayStoreLoad4()
        {
            var a = new Foo[10];
            a[5] = new Foo(11);
            Contract.Assert(a[5].i == 9);
        }

        // corral should detect an exception here
        // a could be null or index out of range
        // not yet implemented
        static void arrayStoreLoad3(int[] a)
        {
            a[5] = 9;
        }

        static void arrayOfArrays1()
        {
            var a = new int[10][][];
            a[0] = new int[1][];
            a[0][0] = new int[100];

            a[0][0][99] = 10;

            Contract.Assert(a[0][0][99] == 10);
        }

        static void arrayOfArrays2()
        {
            var a = new int[10][][];
            a[0] = new int[1][];
            a[0][0] = new int[100];

            a[0][0][99] = 10;

            Contract.Assert(a[0][0][99] != 10);
        }

        static void arrayOfArrays3()
        {
            var a = new Foo[10][][];
            a[0] = new Foo[1][];
            a[0][0] = new Foo[100];

            a[0][0][99] = new Foo(10);

            Contract.Assert(a[0][0][99].i
                 == 10);
        }

        static void arrayOfArrays4()
        {
            var a = new Foo[10][][];
            a[0] = new Foo[1][];
            a[0][0] = new Foo[100];

            a[0][0][99] = new Foo(10);

            Contract.Assert(a[0][0][99].i
                 != 10);
        }

        static void arrayCreate1()
        {
            var a = new int[10];
            Contract.Assert(a == null);
        }

        static void arrayCreate2()
        {
            var a = new int[10];
            Contract.Assert(a != null);
        }

        static void arrayLoad1()
        {
            var a = new int[10][];
            a[0] = new int[10];

            a[0][9] = 10;

            Contract.Assert(a[0][9] == 10);
        }

        static void arrayLength(int[] a)
        {
            var x = a[5];


            var l = a.Length;
            l = l + 1;
            Contract.Assert(a.Length + 1 == l);
        }

        static void arrayFor()
        {
            var a = new int[3];
            Contract.Assert(a.Length == 3);
            for (int i = 0; i<a.Length; i++)
            {
                a[i] = 2;
            }
            var c = 0;
            for (int i = 0; i < a.Length; i++)
            {
                c = c + a[i];
            }

            Contract.Assert(c == 6);
        }

        public static void ArgsLength(string[] args)
        {
            if (args.Length > 0)
            {
                Contract.Assert(args[0] != null);
            }
        }
    }

    class Foo
    {
        public Foo(int x)
        {
            i = x;
        }
        public int i;
        public string p;
    }
}
