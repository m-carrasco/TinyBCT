using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Boxing
    {
        public static void Test1()
        {
            List<Object> list = new List<Object>();
            list.Add(1);
            list.Add(false);
            list.Add(1.5);
            list.Add(1.5f);

            Contract.Assert(list.Count == 4);
        }

        public static void Test2()
        {
            List<Object> list = new List<Object>();
            list.Add(1);
            list.Add(false);
            list.Add(1.5);
            list.Add(1.5f);

            int pos0 = (int)list[0];
            bool pos1 = (bool)list[1];
            double pos2 = (double)list[2];
            float pos3 = (float)list[3];

            Contract.Assert(pos0 == 1);
            Contract.Assert(pos1 == false);
            Contract.Assert(pos2 == 1.5);
            Contract.Assert(pos3 == 1.5f);
        }

        public static void Test3()
        {
            // list.add(false) is wrongly translated by analysis-net 
            // it makes the assert false unreachable

            List<Object> list = new List<Object>();
            list.Add(false); // here false in tac is a zero
            Contract.Assert(false);
            bool pos1 = (bool)list[1];

            //Contract.Assert(false);
        }
    }
}
