using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Loops
    {
        public int[] data = new int[4];

        public void Acum(int n)
        {
            Contract.Assume(n >= 0);

            int i = 0;
            int acum = 0;
            while (true)
            {
                if (i == n)
                    break;

                acum = 1 + acum;
                i++;
            }

            Contract.Assert(acum == n);
        }

        public void Acum1(int n)
        {
            Contract.Assume(n >= 0);
            int acum = 0;

            for (int i = 0; i < n; i++)
                acum++;

            Contract.Assert(acum == n);
        }

        public void Acum2(int n)
        {
            Contract.Assume(n >= 0);
            int acum = 0;

            int i = 0;
            while (i < n)
            {
                acum++;
                i++;
            }

            Contract.Assert(acum == n);
        }
    }
}
