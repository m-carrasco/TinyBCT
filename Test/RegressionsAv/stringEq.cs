using System.Diagnostics.Contracts;


class Test
{
    public static void ShouldFail()
    {
        int s = 0, t = 0;
        string str = "Hello";
        if (str == "Hello")
        {
            s++;
        }
        else
        {
            t++;
        }
        Contract.Assert(s == 0);
    }
    public static void ShouldPass()
    {
        int s = 0, t = 0;
        string str = "Hello";
        if (str == "Hello")
        {
            s++;
        }
        else
        {
            t++;
        }
        Contract.Assert(t == 0);
    }
    public static void ShouldPass2()
    {
        int s = 0, t = 0;
        string str = "Hello";
        if (str == "Hello")
        {
            s++;
        }
        else
        {
            t++;
        }
        Contract.Assert(s == 0 || t == 0);
    }
}
