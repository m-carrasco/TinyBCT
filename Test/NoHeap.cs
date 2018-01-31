using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class NoHeap
    {
        public int factorial(int x)
        {
            int res = 1;
            for (int i = 1; i <= x; i++)
            {
                res = i * res;
            }

            return res;
        }

        public int add(int x, int y)
        {
            return x + y;
        }

        public int subtract(int x, int y)
        {
            return x - y;
        }

        public int multiplyBy2Adding(int x)
        {
            int i = add(x, x);
            return i;
        }

        public int multiply(int a, int x)
        {
            int i = 0;
            while (i < x)
            {
                i++;
                a = a + a;
            }

            return a;
        }

        public int alias(int a)
        {
            int x = a;
            return x;
        }
    }
}
