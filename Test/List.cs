using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class List
    {
        public void foo()
        {
            var l = new LinkedList<int>();

            l.AddFirst(1);
            l.AddFirst(2);
            l.AddFirst(3);
            l.AddFirst(4);

            int acum = 0;
            foreach (var item in l)
            {
                acum = acum + item;
            }
        }

    }
}
