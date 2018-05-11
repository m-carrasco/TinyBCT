using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public partial class TestsHelpers
{
    private static string pathAuxDir = System.IO.Path.Combine(Test.TestUtils.rootTinyBCT, @"Test\TestUtilsAux\");
    [TestMethod]
    public void TestsCorralResultNoBugs()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"no_bugs.bpl"));
        Assert.IsTrue(corralResult.NoBugs());
        Assert.IsFalse(corralResult.AssertionFails());
        Assert.IsFalse(corralResult.SyntaxErrors());
    }
    [TestMethod]
    public void TestsCorralResultAssertionFails()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"assertion_failure.bpl"));
        Assert.IsFalse(corralResult.NoBugs());
        Assert.IsTrue(corralResult.AssertionFails());
        Assert.IsFalse(corralResult.SyntaxErrors());
    }
    [TestMethod]
    public void TestsCorralResultSyntaxError()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"syntax_error.bpl"));
        Assert.IsTrue(corralResult.SyntaxErrors());
    }
    [TestMethod]
    public void TestsCorralResultNoBugsSyntaxErrorCausesException()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"syntax_error.bpl"));
        try
        {
            corralResult.NoBugs();
            Assert.Fail();
        }
        catch (Test.TestUtils.CorralResult.CorralOutputException)
        {
            // This is the expected code path, since there is a syntax error in the BPL file.
        }
        catch (Exception)
        {
            Assert.Fail();
        }
    }
    [TestMethod]
    public void TestsCorralResultAssertionFailsSyntaxErrorCausesException()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"syntax_error.bpl"));
        try
        {
            corralResult.AssertionFails();
            Assert.Fail();
        }
        catch (Test.TestUtils.CorralResult.CorralOutputException)
        {
            // This is the expected code path, since there is a syntax error in the BPL file.
        }
        catch (Exception)
        {
            Assert.Fail();
        }
    }
}

[TestClass]
public partial class Tests
{
    [TestMethod]
    public void TestsCorralIsPresent()
    {
        Console.WriteLine();
        Assert.IsTrue(
            System.IO.File.Exists(Test.TestUtils.corralPath),
            "This is not an actual test but a configuration check. Tests require corral.exe to be present. Configure Test.TestUtils.corralPath appropriately.");
    }


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
    class Main
    {
        public void l0()
        {
            var l = new List2<int>();

            //l.Add(1);
            //l.Add(2);
            //l.Add(3);
            //l.Add(4);

            int acum = 0;
            foreach (var item in Elems(l))
            {
                acum = acum + item;
            }
            Contract.Assert(acum == 0);
        }

        public void l1()
        {
            var l = new List2<int>();

            l.Add(1);
            //l.Add(2);
            //l.Add(3);
            //l.Add(4);

            int acum = 0;
            foreach (var item in Elems(l))
            {
                acum = acum + item;
            }
            Contract.Assert(acum == 1);
        }
        public void l1Fail()
        {
            var l = new List2<int>();

            l.Add(1);
            //l.Add(2);
            //l.Add(3);
            //l.Add(4);

            int acum = 0;
            foreach (var item in Elems(l))
            {
                acum = acum + item;
            }
            Contract.Assert(acum != 1);
        }
        public void l2()
        {
            var l = new List2<int>();

            l.Add(1);
            l.Add(2);
            //l.Add(3);
            //l.Add(4);

            int acum = 0;
            foreach (var item in Elems(l))
            {
                acum = acum + item;
            }
            Contract.Assert(acum == 3);
        }
        public void l3()
        {
            var l = new List2<int>();

            l.Add(1);
            l.Add(2);
            l.Add(3);
            //l.Add(4);

            int acum = 0;
            foreach (var item in Elems(l))
            {
                acum = acum + item;
            }
            Contract.Assert(acum == 6);
        }
        static IEnumerable<int> Elems(List2<int> list)
        {
            foreach (var elem in list)
            {
                yield return elem;

            }
        }
    }
    public class List2<T> : IEnumerable<T> 
    {
	    struct Enumerator : IEnumerator<T> {
	          List2<T> parent;
	          int iter;
	          public Enumerator(List2<T> l) { 
	              parent = l; 
		      iter = -1; 
	          }
	          public bool MoveNext() { 
	              iter = iter + 1; 
		      return (iter < parent.Count);
	          }
	          public T Current { get { return parent[iter]; } }
	          public void Dispose() {}
	          public void Reset() { iter = -1; }
                  object System.Collections.IEnumerator.Current { get { return this.Current; } }
        }

        public List<T> myList;
        public List2() 
        {
            myList = new List<T>();
        }
        public void Add(T elem)
        {
            myList.Add(elem);
        }

        private List2(bool dontCallMe) { }
        public IEnumerator<T> GetEnumerator() { return new Enumerator(this); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
	    public T this[int index] { get { return myList[index]; }  set { myList[index] = value;  } }
	    public int Count { get { return myList.Count; } }
        }
    }
        ";
        DoTest(source, "TestYield");
    }
    [TestMethod]
    public void TestsCollection()
    {
        var source = @"
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Test
{
    class List
    {
        public void l0()
        {
            var l = new List<int>();
            int acum = 0;
            foreach (var item in l)
            {
                acum = acum + item;
            }
            Contract.Assert(acum == 0);
        }
        public void l1()
        {
            var l = new List<int>();

            l.Add(1);

            int acum = 0;
            foreach (var item in l)
            {
                acum = acum + item;
            }
            Contract.Assert(acum == 1);
        }
        public void l2()
        {
            var l = new List<int>();

            l.Add(1);
            l.Add(2);
 
            int acum = 0;
            foreach (var item in l)
            {
                acum = acum + item;
            }
            Contract.Assert(acum == 3);
        }
        public void l3()
        {
            var l = new List<int>();

            l.Add(1);
            l.Add(2);
            l.Add(3);
 //          l.Add(4);

            int acum = 0;
            foreach (var item in l)
            {
                acum = acum + item;
            }
            Contract.Assert(acum == 6);
        }

    }
}
        ";
        DoTest(source, "TestCollection");
    }


    private void DoTest(string source, string assemblyName)
    {

        string[] references = null;
        // Didn't work because there are conflitcs with mscorelib...
        // var references = new string[] { "CollectionStubs.dll" };
        if (Test.TestUtils.CreateAssemblyDefinition(source, assemblyName, references))
        {
            TinyBCT.Program.Main(new string[] { "-i", assemblyName+".dll",
                @"..\..\Dependencies\CollectionStubs.dll",
                "-l", "true",
                "-b", @"..\..\Dependencies\poirot_stubs.bpl" });
        }
        else
        {
            Assert.Fail();
        }
    }
}
