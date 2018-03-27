using System;
using System.Diagnostics.Contracts;

/*class exec
{
    public int execute(Func<int, int> f, int x)
    {
        return f(x);
    }
}

class cMain
{
    public static void Main()
    {
        int s = 0;
        exec e1 = new exec();
        s = e1.execute((x => 2 * x), 3);
        Contract.Assert(s == 5);
    }
}*/

class cMain2
{
    public static void Main()
    {
        Func<int, int> f = foo;
        f(5);
        //Func<int, int> b = bar;
        //b(7);
        //Func<bool, bool> z = zoo;
        //z(true);
        //Func<int, int, int> x = doubleParameter;
        //x(1, 2);
    }

    public static void Main2(int x)
    {
        Func<int, int> f = null;
        if (x == 1)
        {
            f = foo;
        }
        else
            f = bar;

        f(1);
    }

    //delegate int MathAction(int num);

    public static void Main3()
    {
        //MathAction m = foo;
       // m(1);
    }

    public static void Main4(int x)
    {
        /*MathAction m = null;
        Func<int, int> f = null;

        m = bar;
        f = bar;

        m = foo;
        f = foo;

        m(5);
        f(6);*/
    }

    public static void Main5()
    {
        //Func<int, int, int> f = doubleParameter;
        //f(5,7);

    }

    public static bool zoo(bool x)
    {
        return !x;
    }

    public static int bar(int x)
    {
        return x + x;
    }

    public static int foo(int x)
    {
        return x;
    }

    public static int doubleParameter(int x, int y)
    {
        return x + y;
    }

    public static int objectParameter(Func<int,int> f)
    {
        return f(1);
    }
}

