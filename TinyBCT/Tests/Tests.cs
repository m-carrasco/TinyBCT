using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public partial class Tests
{
    [TestMethod]
    public void SimpleTest()
    {
        var source = @"
using System.Diagnostics.Contracts;
class TestType
{
	public int a;
	public TestType()
	{
		a = 0;
	}
}

class SubType
{
	public int x;
	public SubType()
	{
		x = 7;
	}
}

class TestAs
{
	public static int Foo(object a)
	{
		var v = a as SubType;
		Contract.Assert(v != null);
		int s = v.x;
		return s;
	}

	public static int Bar(object a)
	{
		var v = a as TestType;
		Contract.Assert(v != null);
		int s = v.a;
		return s;
	}

	public static void Main()
	{
		TestType v1 = new TestType();
		Foo(v1);
		Bar(v1);
		return;
	}
}
            ";
        if (Test.TestUtils.CreateAssemblyDefinition(source, "SimpleTest"))
        {
            TinyBCT.Program.Main(new string[] { "-i", "SimpleTest.dll", "-l", "false"});
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void GenericsTest()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Holds {
 public int getValue() {
  return 5;
 }
}
class Holds<T> {
 T value;
 public Holds() {
  value = default(T);
 }
 public Holds(T new_val) {
  value = new_val;
 }
 public void setValue(T new_val) {
  value = new_val;
 }
 // Will not compile:
 // Program.cs(20,11): error CS0019: Operator '+' cannot be applied to operands of type 'T' and 'T'
 //public void applyOpPlus(T to_add) {
 // value = value + to_add;
 //}
 public T getValue() {
  return value;
 }
}

// Relevant reference:
// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/differences-between-cpp-templates-and-csharp-generics

class GenericsMain {
  public static void Main() {
    Holds<int> holds_default_int = new Holds<int>();
    Contract.Assert(holds_default_int.getValue() != 9);
    Holds<int> holds_int_1 = new Holds<int>(1);
    Contract.Assert(holds_int_1.getValue() == 1);
    Holds<int> holds_int_2 = new Holds<int>(2);
	// BCT fails assertion, as it should
    Contract.Assert(holds_int_2.getValue() == 1);
	
	Holds<string> holds_default_string = new Holds<string>();
    // Contract.Assert(!holds_default_string.getValue().Equals(""Hello""));
  }
}
            ";
        if (Test.TestUtils.CreateAssemblyDefinition(source, "GenericsTest"))
        {
            TinyBCT.Program.Main(new string[] { "-i", "GenericsTest.dll","-l","true" });
        }
        else
        {
            Assert.Fail();
        }
    }
    [TestMethod]
    public void TestsYield()
    {
        var source = @"
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Test
{
    class List
    {
        public void foo()
        {
            var l = new List<int>();

            l.Add(1);
            l.Add(2);
            l.Add(3);
            l.Add(4);

            int acum = 0;
            foreach (var item in Elems(l))
            {
                acum = acum + item;
            }
            Contract.Assert(acum >= 6);
        }
        static IEnumerable<int> Elems(ICollection<int> list)
        {
            foreach (var elem in list)
            {
                yield return elem;

            }
        }
    }
}
        ";
        DoTest(source, "TestYield");
    }

    private void DoTest(string source, string assemblyName)
    {
       
        if (Test.TestUtils.CreateAssemblyDefinition(source, assemblyName))
        {
            TinyBCT.Program.Main(new string[] { "-i", assemblyName+".dll", "-l", "true","-b", @"C:\Users\diegog\Source\Repos\corral\AddOns\AngelicVerifierNull\test\c#\poirot_stubs.bpl" });
        }
        else
        {
            Assert.Fail();
        }
    }
}
