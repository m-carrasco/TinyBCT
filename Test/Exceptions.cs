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

    class Exceptions
    {
        // works
        public void test1()
        {
            int i = 0;
            try
            {
                throw new Exception();
            }
            catch(Exception ex)
            {
                i = 10;
            }

            Contract.Assert(i == 10);
        }

        public void test2()
        {
            int i = 0;
            try
            {
                throw new Exception();
            }
            catch (Exception ex)
            {
                i = 10;
            }
            finally
            {
                i += 10;
            }

            Contract.Assert(i == 20);
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

            }

            i = 10;
        }

        public void test6()
        {
            int i = 0;
            try
            {
                throw new ExceptionB();
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
    }
}
