using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Exceptions
    {
        public static void exceptions()
        {
            int i = 0;
            try
            {
                if (i == 0)
                    throw new Exception();
                else throw new NullReferenceException();
            }
            catch (NullReferenceException ex)
            {
                i = 1;
            }
            catch (Exception ex)
            {
                i = 1;
            }
            finally
            {
                i = 2;
            }
        }

        public static void exceptions1()
        {
            int i = 0;
            try
            {
                try
                {
                    throw new Exception();
                }
                catch (Exception ex)
                {
                    throw new NullReferenceException();
                }
            }
            catch (Exception ex)
            {
                i = 1;
            }
        }
    }
}
