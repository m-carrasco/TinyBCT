using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class HeapExamples
    {
        int i = 0;
        public static HeapExamples createHeap()
        {
            return new Test.HeapExamples();
        }


        public void writeField()
        {
            i = 4;
        }
    }
}
