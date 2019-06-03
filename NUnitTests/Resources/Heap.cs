using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class C : HeapExamples { }
    class HeapExamples
    {
        public void WriteArguments(Object obj, int x)
        {
            obj = null;
            x = x + x;
        }

        int i = 0;
        public static HeapExamples createHeap()
        {
            return new Test.HeapExamples();
        }


        public void writeField()
        {
            i = 4;
        }

        public void useAs()
        {
            var a = new HeapExamples();
            var b = a as HeapExamples;
            var c = a as C;
            int d = -1;
            if(b==null)
            {
                d = 0;
            }
            else
            {
                d = 1;
            }
            if (c == null) { }
            else
            {
                d += 2;
            }
        }
    }
}
