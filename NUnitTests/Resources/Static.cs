using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Static
    {
        public static int staticI;
        public static int staticZ = 0;
        public static int staticX;

        static Static()
        {
            staticX = 175;
        }

        int instanceI;
        int instanceZ = 0;
    }
}
