using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Arrays
    {
        /*static void Main(string[] args)
        {
            Foo f = new Foo();
            f.i = 5;

            int x = f.i;
            x = x + 1;

            f.p = args[0];
        }*/

        static void arrayLoad(int[][] a)
        {
            var p = a[1][1];
        }

        static void arrayLoad1(int[,] a)
        {
            var p = a[1,1];
        }

        static void arrayStore(int[][] a)
        {
            a[1][1] = 0;
        }

        static void arrayTest1(Foo[] a)
        {
            int l0 = a.GetLength(0);
            int l1 = a.GetLength(1);
            int l2 = a.Count();
            int l3 = a.Length;

            Object v1 = a.GetValue(0); // this causes an exception in tinybct
            Foo v2 = a[0];
        }

        /*static void arrayTest2(Foo[][] a)
        {
            Foo v2 = a[0][2];
        }

        static void arrayTest3()
        {
            var a = new Foo[510, 10];
            Foo v2 = a[0, 2];
        }

        static void arrayTest4()
        {
            var a = new Foo[510];
            Foo v2 = a[0];
        }

        static void arrayTest5()
        {
            var a = new Foo[510];
            a[0] = new Foo();
        }

        static void arrayTest6()
        {
            var a = new int[510];
            a[0] = 30;
        }

        static void arrayTest7(Foo[,] a)
        {
            Foo v2 = null;
            a[0,2] = v2;
        }*/

        /*static void arrayTest8()
        {
            var a = new int[510, 10];
            a[1, 0] = 1 ;
        }*/
    }

    class Foo
    {
        public int i;
        public string p;
    }
}
