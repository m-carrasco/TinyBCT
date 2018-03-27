using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Inheritance
    {
        public static void testingAS()
        {
            Contract.Assert(false);

            bool failed = false;

            // claseA cannot be subtype of any other class
            claseA cA = new claseA();
            var cast1 = cA as claseB;
            if (cast1 != null)
                Contract.Assert(failed);


            var cast2 = cA as claseC;
            if (cast2 != null)
                Contract.Assert(failed);

            var cast4 = cA as claseE;
            if (cast4 != null)
                Contract.Assert(failed);

            /* cast4:= $As(cA, T$Test.claseF());
            if (cast4 != null)
            {
                assert false;
            }*/

            //var cast5 = c as claseF; ->  C# compiler doesn't allow because they are siblings

            // classB can only be subclass of classA
            var cB = new claseB();
            var cast7 = cB as claseA;
            if (cast7 == null)
                Contract.Assert(failed);

            var cast8 = cB as claseC;
            if (cast8 != null)
                Contract.Assert(failed);

            //var cast10 = cB as claseE -> C# compiler doesn't allow because they are siblings
            //var cast11 = c as claseF; ->  C# compiler doesn't allow because they are siblings

            var cC = new claseC();
            var cast13 = cC as claseA; // ok
            if (cast13 == null)
                Contract.Assert(failed);
            var cast14 = cC as claseB;
            if (cast14 == null)
                Contract.Assert(failed);

            var cE = new claseE();
            var cast15 = cE as claseA;
            if (cast15 == null)
                Contract.Assert(failed);

            var cF = new claseF();

            // These are cases that the compiler doesn't allow
            // the following code was appended at the end of the boogie file.
            /*
		        cast4:= $As(cB, T$Test.claseE());
	            if (cast4 != null)
	            {
		            assert false;
	            }
	            cast4:= $As(cB, T$Test.claseF());
	            if (cast4 != null)
	            {
		            assert false;
	            }
	
	            cast4:= $As(cC, T$Test.claseE());
	            if (cast4 != null)
	            {
		            assert false;
	            }
	            cast4:= $As(cC, T$Test.claseF());
	            if (cast4 != null)
	            {
		            assert false;
	            }
	

	            cast4:= $As(cE, T$Test.claseC());
	            if (cast4 != null)
	            {
		            assert false;
	            }
	
	
	            cast4:= $As(cE, T$Test.claseB());
	            if (cast4 != null)
	            {
		            assert false;
	            }
	
	            cast4:= $As(cE, T$Test.claseF());
	            if (cast4 != null)
	            {
		            assert false;
	            }
	
	            cast4:= $As(cF, T$Test.claseA());
	            if (cast4 != null)
	            {
		            assert false;
	            }
	
	            cast4:= $As(cF, T$Test.claseB());
	            if (cast4 != null)
	            {
		            assert false;
	            }
	
	            cast4:= $As(cF, T$Test.claseC());
	            if (cast4 != null)
	            {
		            assert false;
	            }
	
	            cast4:= $As(cF, T$Test.claseE());
	            if (cast4 != null)
	            {
		            assert false;
	            } 
             */

        }
    }

    class claseA 
    {

    }

    class claseB : claseA
    {

    }
    
    class claseC : claseB
    {

    }

    class claseE : claseA
    {

    }

    class claseF
    {

    }
        
}
