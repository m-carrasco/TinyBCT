using System.Diagnostics.Contracts;
class TestType
{
	public int a;
	public TestType()
	{
		a = 0;
	}
}

class NotSubType
{
	public int x;
	public NotSubType()
	{
		x = 7;
	}
}

class TestAs
{
	public static int Foo(object a)
	{
		var v = a as NotSubType;
		Contract.Assert(v != null);
		int s = v.x;
		return s;
	}

	public static void Main()
	{
		TestType v1 = new TestType();
		Foo(v1);
		return;
	}
}

