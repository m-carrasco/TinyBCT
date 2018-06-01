using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    public class ExceptionA : Exception
    {
    }

    public class ExceptionB : Exception
    {
    }

    public class ExceptionC : Exception
    {
    }

    public class ExceptionSubB : ExceptionB
    {
    }

    class ExceptionTest1NoBugs
    {
        public static void Main()
        {
            test1();
        }

        public static void test1()
        {
            int i = 0;
            try
            {
                var e = new Exception();
                i = 1;
                // Exception constructor is called and then $Exception value is checked
                // if it was not initialized, it may return the method at this point.
                // this could be an example showing that it is necessary to initialize the global $Exception variable
                throw e;
            }
            catch (Exception ex)
            {
                i = i + 10;
            }

            Contract.Assert(i == 11);
        }
    }

    class ExceptionTest1Bugged
    {
        public static void Main()
        {
            test1();
        }

        public static void test1()
        {
            int i = 0;
            try
            {
                var e = new Exception();
                i = 1;
                // Exception constructor is called and then $Exception value is checked
                // if it was not initialized, it jumps to the catch handler before assinging i = 1
                // this could be an example showing that it is necessary to initialize the global $Exception variable
                throw e;
            }
            catch (Exception ex)
            {
                i = i + 10;
            }

            Contract.Assert(i != 11);
        }
    }

    class ExceptionTest2NoBugs
    {
        public static void Main()
        {
            test2();
        }

        public static void test2()
        {
            int i = 0;
            try
            {
                var e = new Exception();
                i = 1;
                throw e;
            }
            catch (Exception ex)
            {
                i = i + 10;
            }
            finally
            {
                i += 10;
            }

            Contract.Assert(i == 21);
        }
    }

    class ExceptionTest2Bugged
    {
        public static void Main()
        {
            test2();
        }

        public static void test2()
        {
            int i = 0;
            try
            {
                var e = new Exception();
                i = 1;
                throw e;
            }
            catch (Exception ex)
            {
                i = i + 10;
            }
            finally
            {
                i += 10;
            }

            Contract.Assert(i != 21);
        }
    }


    class ExceptionTest3NoBugs
    {
        public static void Main()
        {
            test3();
        }

        public static void test3()
        {
            int i = 0;
            try
            {
            }
            catch (Exception ex)
            {
                i = 10;
                Contract.Assert(false);
            }
            finally
            {
                i += 10;
            }

            Contract.Assert(i == 10);
        }
    }

    class ExceptionTest3Bugged
    {
        public static void Main()
        {
            test3();
        }

        public static void test3()
        {
            int i = 0;
            try
            {
            }
            catch (Exception ex)
            {
                i = 10;
                Contract.Assert(false);
            }
            finally
            {
                i += 10;
            }

            Contract.Assert(i != 10);
        }
    }

    class ExceptionTest5NoBugs
    {
        public static void Main()
        {
            test5();
        }

        public static void throwException()
        {
            throw new Exception();
        }

        public static void test5()
        {
            int i = 0;
            try
            {
                if (i == 0)
                {
                    throwException();
                }

            }
            catch (Exception ex)
            {
                i = i + 5;
            }

            i = i + 10;

            Contract.Assert(i == 15);
        }
    }

    class ExceptionTest5Bugged
    {
        public static void Main()
        {
            test5();
        }

        public static void throwException()
        {
            throw new Exception();
        }

        public static void test5()
        {
            int i = 0;
            try
            {
                if (i == 0)
                {
                    throwException();
                }

            }
            catch (Exception ex)
            {
                i = i + 5;
            }

            i = i + 10;

            Contract.Assert(i != 15);
        }
    }

    class ExceptionTest6NoBugs
    {
        public static void Main()
        {
            test6();
        }

        public static void test6()
        {
            int i = 0;
            try
            {
                throw new ExceptionSubB();
            }
            catch (ExceptionA ex)
            {
                i = 10;
            }
            catch (ExceptionB ex)
            {
                i = 20;
            }
            catch (ExceptionC ex)
            {
                i = 30;
            }
            finally
            {
                i += 40;
            }

            Contract.Assert(i == 60);
        }
    }

    class ExceptionTest6Bugged
    {
        public static void Main()
        {
            test6();
        }

        public static void test6()
        {
            int i = 0;
            try
            {
                throw new ExceptionSubB();
            }
            catch (ExceptionA ex)
            {
                i = 10;
            }
            catch (ExceptionB ex)
            {
                i = 20;
            }
            catch (ExceptionC ex)
            {
                i = 30;
            }
            finally
            {
                i += 40;
            }

            Contract.Assert(i != 60);
        }
    }

    class ExceptionTest7NoBugs
    {
        public static void Main()
        {
            test7();
        }

        public static void test7()
        {
            int i = 0;
            try
            {
                try
                {
                    throw new ExceptionB();
                }
                catch (ExceptionC ex)
                {
                    i = 30;
                }
                finally
                {
                    i += 40;
                }
            }
            finally
            {
                i += 10;
                Contract.Assert(i == 50);
            }

            // it should never reach this part of the code
            // the exception is not handled, the flow will exit the method at the end of the last finally.
            Contract.Assert(false); 
        }
    }

    class ExceptionTest7Bugged
    {
        public static void Main()
        {
            test7();
        }

        public static void test7()
        {
            int i = 0;
            try
            {
                try
                {
                    throw new ExceptionB();
                }
                catch (ExceptionC ex)
                {
                    i = 30;
                }
                finally
                {
                    i += 40;
                }
            }
            finally
            {
                i += 10;
                Contract.Assert(i != 50);
            }

            // it should never reach this part of the code
            // the exception is not handled, the flow will exit the method at the end of the last finally.
        }
    }

    class ExceptionTestInitializeExceptionVariable
    {
        public static void safe()
        {

        }

        // if this method is used as an entry point
        // global exception variable won't be initialized
        // after safe() is called, the exception variable could be on
        // because it was not initialized

        // TinyBCT generates a wrapper for each main method found 
        // wrappers initialize global variables

        public static void test()
        {
            try
            {
                safe();
            } catch (Exception ex)
            {
                Contract.Assert(false);
            }
        }
    }

    /*class Exceptions
    {
        public void test4()
        {
            int i = 0;
            try
            {
                throw new Exception();
            }
            finally
            {
                i += 10;
                Contract.Assert(i == 10);
            }

            // compiler removes this piece of code - it is not present in bytecode
            Contract.Assert(false); // this should not be called.
        }
    }*/
}
