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

    class Exceptions
    {
        public void test1()
        {
            int i = 0;
            try
            {
                var e = new Exception();
                i = 1; 
                // this way shows that it is necessary to initialize the global $Exception variable
                throw e;
            }
            catch(Exception ex)
            {
                i = i+ 10;
            }

            Contract.Assert(i == 11);
        }

        public void test2()
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

        public void test3()
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

        public void throwException()
        {
            throw new Exception();
        }

        public void safe()
        {

        }

        // works
        public void test5()
        {
            int i = 0;
            try
            {
                if (i == 0)
                {
                    throwException();
                }

            } catch (Exception ex)
            {
                i = i + 5;
            }

            i = i+ 10;

            Contract.Assert(i == 15);
        }

        public void test6()
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

        public void test7()
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
            }

            Contract.Assert(i == 50);
        }

        public void test8()
        {
            int i = 0;
            try
            {
                i++;
            }
            finally
            {
                if (i == 5)
                {
                    if (i == 6)
                    {
                        i = 7;
                    }
                    else
                        i = 9;
                }
                else
                    i = 10;
            }
        }
    }
}
