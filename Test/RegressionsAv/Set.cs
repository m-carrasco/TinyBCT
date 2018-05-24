using System;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Collections.Generic;

class PoirotMain
{
    static Random r;

    public static void Main()
    {
        int i;
	r = new System.Random();
        HashSet<int> intSet = new HashSet<int>();
        intSet.Add(0);
        intSet.Add(1);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 0 || i == 1);
        intSet.Remove(0);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 1);
        int sum = 0;
        foreach (int x in intSet)
        {
            sum += x;
        }
        Contract.Assert(sum == 1);
        Contract.Assert(intSet.Contains(0));
    }
    public static void ShouldPass1()
    {
        int i;
	r = new System.Random();
        HashSet<int> intSet = new HashSet<int>();
        intSet.Add(0);
        intSet.Add(1);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 0 || i == 1);
    }

    public static void ShouldPass2()
    {
        int i;
	r = new System.Random();
        HashSet<int> intSet = new HashSet<int>();
        intSet.Add(0);
        intSet.Add(1);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 0 || i == 1);
        intSet.Remove(0);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 1);
    }
    public static void ShouldPass3()
    {
        int i;
	r = new System.Random();
        HashSet<int> intSet = new HashSet<int>();
        intSet.Add(0);
        intSet.Add(1);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 0 || i == 1);
        intSet.Remove(0);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 1);
        int sum = 0;
        foreach (int x in intSet)
        {
            sum += x;
        }
        Contract.Assert(sum > 0);
    }
    public static void ShouldPass4()
    {
        int i;
	r = new System.Random();
        HashSet<int> intSet = new HashSet<int>();
        intSet.Add(0);
        intSet.Add(1);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 0 || i == 1);
        intSet.Remove(0);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 1);
        int sum = 0;
        foreach (int x in intSet)
        {
            sum += x;
        }
        Contract.Assert(sum == 1);
    }
    public static void ShouldPass5()
    {
        int i;
	r = new System.Random();
        HashSet<int> intSet = new HashSet<int>();
        intSet.Add(0);
        intSet.Add(1);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 0 || i == 1);
	int i2 = r.Next();
	Contract.Assume(i2 != 0 && i2 != 1);
	Contract.Assert(!intSet.Contains(i2));
    }
    public static void ShouldPass6()
    {
        int i;
	r = new System.Random();
        HashSet<int> intSet = new HashSet<int>();
        intSet.Add(0);
        intSet.Add(1);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        intSet.Remove(0);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        int sum = 0;
        foreach (int x in intSet)
        {
            Contract.Assert(x == 1);
            sum += x;
        }
    }
    public static void ShouldFail1()
    {
        int i;
	r = new System.Random();
        HashSet<int> intSet = new HashSet<int>();
        intSet.Add(0);
        intSet.Add(1);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
	Contract.Assert(false);
    }
    public static void ShouldFail2()
    {
        int i;
	r = new System.Random();
        HashSet<int> intSet = new HashSet<int>();
        intSet.Add(0);
        intSet.Add(1);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        intSet.Remove(0);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(false);
    }
    public static void ShouldFail3()
    {
        int i;
	r = new System.Random();
        HashSet<int> intSet = new HashSet<int>();
        intSet.Add(0);
        intSet.Add(1);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 0 || i == 1);
        intSet.Remove(0);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 1);
        int sum = 0;
        foreach (int x in intSet)
        {
            sum += x;
        }
        Contract.Assert(sum == 0);
    }
    public static void ShouldFail4()
    {
        int i;
	r = new System.Random();
        HashSet<int> intSet = new HashSet<int>();
        intSet.Add(0);
        intSet.Add(1);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        intSet.Remove(0);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        int sum = 0;
        foreach (int x in intSet)
        {
            sum += x;
	    Contract.Assert(false);
        }
    }
    public static void ShouldFail5()
    {
        int i;
	r = new System.Random();
        HashSet<int> intSet = new HashSet<int>();
        intSet.Add(0);
        intSet.Add(1);
        i = r.Next();
        Contract.Assume(intSet.Contains(i));
        Contract.Assert(i == 0 || i == 1);
	int i2 = r.Next();
	Contract.Assume(i2 != 0 && i2 != 1);
	Contract.Assert(intSet.Contains(i2));
    }
}
