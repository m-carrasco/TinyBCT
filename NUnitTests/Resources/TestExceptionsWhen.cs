using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Ex1 : Exception
{

}
class Ex2 : Exception
{

}
class TestExceptionsWhen
{
    public void Main(int y)
    {
        int x = 5;
        try
        {
            x = x / y;
        }
        catch (Exception ex) when (ex is Ex1 || ex is Ex2)
        {

        }
    }
}
