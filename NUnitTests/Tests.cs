using System;
using System.Collections.Generic;
using NUnit.Framework;
using MemOpt = TinyBCT.ProgramOptions.MemoryModelOption;
using TinyBCT;
using NUnitTests;
using System.IO;
using NUnitTests.Utils;
using System.Diagnostics.Contracts;
using Resource = NUnitTests.Utils.Resource;

class TestsHelpers :  TestsBase
{
    [Category("Call-Corral"), Test, Timeout(10000)]
    public void TestsCorralResultNoBugs()
    {
        var bplFile = Path.ChangeExtension(Path.GetTempFileName(), ".bpl");
        var content = Resource.GetResourceAsString("NUnitTests.Resources.TestsAux.no_bugs.bpl");
        File.WriteAllText(bplFile, content);

        var corralOptions = new CorralRunner.CorralOptions() { InputBplFile = bplFile};
        var runner = new CorralRunner();
        var corralResult = runner.Run(corralOptions);

        Assert.IsTrue(corralResult.NoBugs());
        Assert.IsFalse(corralResult.AssertionFails());
        Assert.IsFalse(corralResult.SyntaxErrors());
    }
    [Category("Call-Corral")]
    [Test]
    [Timeout(0)]
    public void TestsCorralResultAssertionFails()
    {
        var bplFile = Path.ChangeExtension(Path.GetTempFileName(), ".bpl");
        var content = Resource.GetResourceAsString("NUnitTests.Resources.TestsAux.assertion_failure.bpl");
        File.WriteAllText(bplFile, content);

        var corralOptions = new CorralRunner.CorralOptions() { InputBplFile = bplFile };
        var runner = new CorralRunner();
        var corralResult = runner.Run(corralOptions);

        Assert.IsFalse(corralResult.NoBugs());
        Assert.IsTrue(corralResult.AssertionFails());
        Assert.IsFalse(corralResult.SyntaxErrors());
    }
    [Category("Call-Corral")]
    [Test]
    [Timeout(10000)]
    public void TestsCorralResultSyntaxError()
    {
        var bplFile = Path.ChangeExtension(Path.GetTempFileName(), ".bpl");
        var content = Resource.GetResourceAsString("NUnitTests.Resources.TestsAux.syntax_error.bpl");
        File.WriteAllText(bplFile, content);

        var corralOptions = new CorralRunner.CorralOptions() { InputBplFile = bplFile };
        var runner = new CorralRunner();
        var corralResult = runner.Run(corralOptions);

        Assert.IsTrue(corralResult.SyntaxErrors());
    }
    [Category("Call-Corral")]
    [Test]
    [Timeout(10000)]
    public void TestsCorralResultNameResolutionError()
    {
        var bplFile = Path.ChangeExtension(Path.GetTempFileName(), ".bpl");
        var content = Resource.GetResourceAsString("NUnitTests.Resources.TestsAux.name_resolution_error.bpl");
        File.WriteAllText(bplFile, content);

        var corralOptions = new CorralRunner.CorralOptions() { InputBplFile = bplFile };
        var runner = new CorralRunner();
        var corralResult = runner.Run(corralOptions);

        Assert.IsTrue(corralResult.NameResolutionErrors());
    }
    [Test]
    [Category("Call-Corral")]
    [Timeout(10000)]
    public void TestsCorralResultNoBugsSyntaxErrorCausesException()
    {
        NUnit.Framework.TestDelegate t = () =>
        {
            var bplFile = Path.ChangeExtension(Path.GetTempFileName(), ".bpl");
            var content = Resource.GetResourceAsString("NUnitTests.Resources.TestsAux.syntax_error.bpl");
            File.WriteAllText(bplFile, content);

            var corralOptions = new CorralRunner.CorralOptions() { InputBplFile = bplFile };
            var runner = new CorralRunner();
            var corralResult = runner.Run(corralOptions);

            corralResult.NoBugs();
        };

        Assert.Throws<CorralRunner.CorralResult.CorralOutputException>(t);
    }
    [Category("Call-Corral")]
    [Test]
    [Timeout(10000)]
    public void TestsCorralResultAssertionFailsSyntaxErrorCausesException()
    {
        NUnit.Framework.TestDelegate t = () =>
        {
            var bplFile = Path.ChangeExtension(Path.GetTempFileName(), ".bpl");
            var content = Resource.GetResourceAsString("NUnitTests.Resources.TestsAux.syntax_error.bpl");
            File.WriteAllText(bplFile, content);

            var corralOptions = new CorralRunner.CorralOptions() { InputBplFile = bplFile };
            var runner = new CorralRunner();
            var corralResult = runner.Run(corralOptions);
            corralResult.AssertionFails();
        };

        Assert.Throws<CorralRunner.CorralResult.CorralOutputException>(t);
    }

    [Category("Call-Corral")]
    [Test]
    [Timeout(10000)]
    public void TestsCorralResultGetOutputSyntaxErrorCausesException()
    {
        NUnit.Framework.TestDelegate t = () => {
            var bplFile = Path.ChangeExtension(Path.GetTempFileName(), ".bpl");
            var content = Resource.GetResourceAsString("NUnitTests.Resources.TestsAux.syntax_error.bpl");
            File.WriteAllText(bplFile, content);

            var corralOptions = new CorralRunner.CorralOptions() { InputBplFile = bplFile };
            var runner = new CorralRunner();
            var corralResult = runner.Run(corralOptions);
            corralResult.getOutput();
        };

        Assert.Throws<CorralRunner.CorralResult.CorralOutputException>(t);
    }

    [Category("Call-Corral"), Test, Timeout(10000)]
    public void TestsCorralIsPresent()
    {
        Console.WriteLine();
        Assert.IsTrue(
            System.IO.File.Exists(corralPath),
            "This is not an actual test but a configuration check. Tests require corral.exe to be present. Configure Test.TestUtils.corralPath appropriately.");
    }
}

class TestsBase
{
    static TestsBase()
    {
        rootTinyBCT = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
        corralPath = Path.Combine(rootTinyBCT, "..", "corral", "bin", "Debug", "corral.exe");
    }

    protected static string rootTinyBCT;
    protected static string corralPath;

    protected ProgramOptions CreateDefaultTinyBctOptions()
    {
        return new ProgramOptions();
    }

    protected CorralRunner.CorralOptions CreateDefaultCorralOptions(string name)
    {
        return new CorralRunner.CorralOptions() { MainProcedure = name };
    }

    protected CorralRunner.CorralResult TestDll(string dllPath, ProgramOptions options, CorralRunner.CorralOptions corralOptions)
    {
        Contract.Assert(File.Exists(dllPath));

        var inputFiles = new List<String>() { dllPath };
        inputFiles.AddRange(options.GetInputFiles());
        options.SetInputFiles(inputFiles);
        TinyBctRunner bctRunner = new TinyBctRunner();
        var bplFile = bctRunner.Run(options);

        CorralRunner corralRunner = new CorralRunner();
        corralOptions.InputBplFile = bplFile;
        var corralResult = corralRunner.Run(corralOptions);

        return corralResult;
    }

    protected CorralRunner.CorralResult TestSingleCSharpFile(string source, ProgramOptions options, CorralRunner.CorralOptions corralOptions)
    {
        Contract.Assert(!String.IsNullOrEmpty(source));

        var dll = Compiler.CompileSource(source);
        return TestDll(dll, options, corralOptions);
    }

    protected CorralRunner.CorralResult TestSingleCSharpResourceFile(string resource, ProgramOptions options, CorralRunner.CorralOptions corralOptions)
    {
        Contract.Assert(!String.IsNullOrEmpty(resource));

        var source = Resource.GetResourceAsString(resource);
        return TestSingleCSharpFile(source, options, corralOptions);
    }

    [OneTimeSetUp]
    public static void ClassInit()
    {
        var location = System.IO.Path.GetDirectoryName(typeof(Compiler).Assembly.Location);
        System.IO.Directory.SetCurrentDirectory(location);
    }

    [SetUp]
    public void TestInitialize()
    {
        TinyBCT.Program.ResetStaticVariables();
    }
}

class SimpleTests : TestsBase
{
    [Test, Timeout(10000)]
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

        var dll = Compiler.CompileSource(source);
        TinyBctRunner tinyBctRunner = new TinyBctRunner();
        var defaultOpts = CreateDefaultTinyBctOptions();
        defaultOpts.SetInputFiles(new List<string>() { dll });
        tinyBctRunner.Run(defaultOpts);
    }

    [Test, Timeout(10000)]
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

        var dll = Compiler.CompileSource(source);
        TinyBctRunner tinyBctRunner = new TinyBctRunner();
        var defaultOpts = CreateDefaultTinyBctOptions();
        defaultOpts.SetInputFiles(new List<string>() { dll });
        tinyBctRunner.Run(defaultOpts);
    }
    [Test, Timeout(10000)]
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
        var dll = Compiler.CompileSource(source);
        TinyBctRunner tinyBctRunner = new TinyBctRunner();
        var defaultOpts = CreateDefaultTinyBctOptions();
        defaultOpts.SetInputFiles(new List<string>() { dll });
        tinyBctRunner.Run(defaultOpts);
    }
    [Test, Timeout(10000)]
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

        var dll = Compiler.CompileSource(source);
        TinyBctRunner tinyBctRunner = new TinyBctRunner();
        var defaultOpts = CreateDefaultTinyBctOptions();
        defaultOpts.SetInputFiles(new List<string>() { dll });
        tinyBctRunner.Run(defaultOpts);
    }

    [Test, Timeout(10000)]
    public void TestRef()
    {
        string source = @"
using System;
using System.Diagnostics.Contracts;

class A {
  public static void Main() {
    var a = new A();
    Contract.Assert(a != null);
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("A.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Timeout(10000)]
    // TODO(rcastano): Check what the intention of this test was. Ignoring for now, since it fails.
    // This test was added in commit 49e45a41664982a1d05326b395d41c2e064a3a9b, with commit message:
    //"Fix source location in annotations (full name)
    // Add attribute to global intializer
    // Remove copyPropagation"
    [Ignore("")]
    public void TestBitConverter()
    {
        string source = @"
using System;
using System.Diagnostics.Contracts;

class A {
  public static void Main() {
    var res =  System.BitConverter.ToInt32(new System.Byte[] {1,2,3,4}, 0);
    Contract.Assert(res != 0);
  }
}
        ";

        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("A.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
}

class AvRegressionTests : TestsBase
{
    [Test, Category("Av-Regressions")]
    public void TestAsSimple()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.AsSimple.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("TestAs.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions")]
    public void TestAsNotSubtype()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.AsNotSubtype.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("TestAs.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestAsSubtypeOk()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.AsSubtypeOk.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("TestAs.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestAsSubtypeFails()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.AsSubtypeFails.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("TestAs.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions")]
    public void TestIsGenerics1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;

class Holds<T> {
 T value;
}
class Test {
  public static bool IsHoldsString<S>(Holds<S> h) {
    return h is Holds<string>;
  }
  public static void Main() {
    Holds<string> holds_strings = new Holds<string>();
    Contract.Assert(IsHoldsString(holds_strings));
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Generics")]
    public void TestIsGenerics2()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;

class Holds<T> {
 T value;
}
class Test {
  public static bool IsHoldsString<S>(Holds<S> h) {
    return h is Holds<string>;
  }
  public static void Main() {
    Holds<Int32> holds_ints = new Holds<Int32>();
    Contract.Assert(IsHoldsString(holds_ints));
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Generics")]
    public void TestDynamicDispatchGenerics1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;

class Holds<T> {
 T value;
 virtual public void Foo() {
  Contract.Assert(false);
 }
}

class SubHolds<T> : Holds<T> {
 override public void Foo() {
 }
}

class Test {
  public static void Main() {
    Holds<Int32> holds_ints = new SubHolds<Int32>();
    holds_ints.Foo();
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Generics")]
    public void TestDynamicDispatchGenerics2()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;

class Holds<T> {
 T value;
 virtual public void Foo() {
 }
}

class SubHolds<T> : Holds<T> {
 override public void Foo() {
 }
}

class Test {
  public static void Main() {
    Holds<Int32> holds_ints = new SubHolds<Int32>();
    holds_ints.Foo();
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test,Category("MissingTypes")]
    public void TestAsUndeclaredType1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;

class Test {
  public static void Main() {
    object o = new object();
    if (((int?)o) != null) { // Convert to Nullable<int>
      Contract.Assert(false);
    }
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Repro")]
    public void TestReturns()
    {
        var source = @"
using System.Diagnostics.Contracts;
class Test {
    public bool Foo(bool b, bool c)
    {
        return b && c;
    }
    public void Main()
    {
        Contract.Assert(Foo(true, true) == true);
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Repro")]
    public void TestExternMethod1()
    {
        var source = @"
class Test {
    public string toString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        return sb.ToString();
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.toString"));
        Assert.IsFalse(corralResult.NameResolutionErrors());
    }
    [Test, Category("Repro")]
    public void TestCast1()
    {
        var source = @"
class Test {
    public void Main(double d)
    {
        int i = (int) d;
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main$System.Double"));
        Assert.IsFalse(corralResult.SyntaxErrors());
    }
    [Test, Category("Repro")]
    public void TestDynamicDispatch1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
public interface Base {
    void Foo();
}
interface Base2 : Base {
    void Bar();
}
class Derived : Base2 {
    public virtual void Foo() {
        Contract.Assert(false);
    }
    public virtual void Bar() {
    }
}
class Test {
    public void Main(Base2 b) {
        b.Foo();
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main$Base2"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Repro")]

    public void TestInterfaceParameterOptionTrue()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
public interface Base {
    int Foo();
}
interface Base2 : Base {
    void Bar();
}
class Derived : Base2 {
    public virtual int Foo() {
        return 5;
    }
    public virtual void Bar() {
    }
}
class Test {
    public void Main(Base2 b) {
        var a = b.Foo();
        Contract.Assert(a == 5);
    }
}
        ";
        var options = new ProgramOptions();
        var corralResult = TestSingleCSharpFile(source, options, CreateDefaultCorralOptions("Test.Main$Base2"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Repro")]
    public void TestDynamicDispatch2()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
public interface Base {
    void Foo();
}
interface Base2 : Base {
    void Bar();
}
class Derived : Base2 {
    public virtual void Foo() {
    }
    public virtual void Bar() {
    }
}
class Test {
    public void Main(Base2 b) {
        if (b != null) {
            b.Foo();
        }
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main$Base2"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Repro"), Category("DefaultKeyword")]
    public void DefaultNull1()
    {

        var source = @"
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
class A {
}
class Test {
  public static void Main() {
    A a = default;
    Contract.Assert(a == null);
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Repro"), Category("DefaultKeyword")]
    public void DefaultNull2()
    {

        var source = @"
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
class A {
}
class Test {
  public static void Main() {
    A a = default;
    Contract.Assert(a != null);
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Repro"), Category("DefaultKeyword")]
    public void DefaultDouble1()
    {

        var source = @"
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

class Test {
  public static double Abs(double d) {
    if (d > 0.0) return d;
    return -d;
  }
  public static void Main() {
    double a = default;
    Contract.Assert(Abs(a - 0.0) < 0.001);
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Repro"), Category("DefaultKeyword")]
    public void DefaultDouble2()
    {

        var source = @"
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

class Test {
  public static double Abs(double d) {
    if (d >= 0.0) return d;
    return -d;
  }
  public static void Main() {
    double a = default;
    Contract.Assert(!(Abs(a - 0.0) < 0.001));
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Repro"), Category("DefaultKeyword")]
    public void DefaultInt1()
    {

        var source = @"
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

class Test {
  public static void Main() {
    int a = default;
    Contract.Assert(a == 0);
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Repro"), Category("DefaultKeyword")]
    public void DefaultInt2()
    {

        var source = @"
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

class Test {
  public static void Main() {
    int a = default;
    Contract.Assert(a != 0);
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("NullPtrInstrumentation")]
    public void TestNullPointerInstrumentation1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
public class Base {
    public void Foo() {
       Contract.Assert(false);
    }
}
class Test {
    public void Main() {
        Base b = null;
        // This method call is instrumented with the following Boogie code:
        // assume {:nonnull} b != null;
        // As such, the assert false within Base.Foo will not be reached.
        b.Foo();
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("NullPtrInstrumentation")]
    public void TestNullPointerInstrumentation2()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
public class A {
   public int value;
}
class Test {
    public void Foo(ref A a) {
        a.value = 5;
        // This should not fail since the dereference will be instrumented.
        Contract.Assert(false);
    }
    public void Main() {
        A a = null;
        Foo(ref a);
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("NullPtrInstrumentation")]
    public void TestNullPointerInstrumentation3()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
public class A {
}
class Test {
    public void Foo(ref A a) {
        // This should fail and the reference creation should *not* be instrumented.
        Contract.Assert(false);
    }
    public void Main() {
        A a = null;
        Foo(ref a);
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("NullPtrInstrumentation")]
    public void TestNullPointerInstrumentation4()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
public class Base {
    public void Foo() {
    }
}
class Test {
    public void Main() {
        Base b = null;
        // This method call is instrumented with the following Boogie code:
        // assert {:nonnull} b != null;
        // This assertion will fail.
        b.Foo();
    }
}
        ";
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.CheckNullDereferences = true;
        var corralResult = TestSingleCSharpFile(source, options, CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("NullPtrInstrumentation")]
    public void TestNullPointerInstrumentation5()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
public class A {
    public void Bar(int n) {
    }
}
class Test {
    public static void Main() {
        A a = null;
        Action<int> new_delegate = delegate (int x) { a.Bar(x); };
        new_delegate(5);
    }
}
        ";
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.CheckNullDereferences = true;
        var corralResult = TestSingleCSharpFile(source, options, CreateDefaultCorralOptions("$Main_Wrapper_Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("NullPtrInstrumentation")]
    public void TestNullPointerInstrumentation6()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
public class A {
    public static void Bar(int n) {
    }
}
class Test {
    public static void Main() {
        // This should not be instrumented for null pointer dereference since there is no pointer.
        Action<int> new_delegate = delegate (int x) { A.Bar(x); };
        new_delegate(5);
    }
}
        ";
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.CheckNullDereferences = true;
        var corralResult = TestSingleCSharpFile(source, options, CreateDefaultCorralOptions("$Main_Wrapper_Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("NullPtrInstrumentation")]
    public void TestNullPointerInstrumentation7()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static void Main() {
        int [] a = null;
        // This should be instrumented for null pointer dereference (with an assume statement) and the assert false should not be reached.
        int x = a[0];
        Contract.Assert(false);
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("NullPtrInstrumentation")]
    public void TestNullPointerInstrumentation8()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static void Main() {
        int [] a = null;
        // This should be instrumented for null pointer dereference (with an assume statement) and the assert false should not be reached.
        int x = a.Length;
        Contract.Assert(false);
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("NullPtrInstrumentation")]
    public void TestNullPointerInstrumentation9()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static void Main() {
        int [] a = null;
        // This should be instrumented for null pointer dereference (with an assume statement) and the assert false should not be reached.
        a[0] = 5;
        Contract.Assert(false);
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("NullPtrInstrumentation")]
    public void TestNullPointerInstrumentation10()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static void Main() {
        int [] a = null;
        // This should be instrumented for null pointer dereference with an assertion that should fail.
        int x = a[0];
    }
}
        ";
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.CheckNullDereferences = true;
        var corralResult = TestSingleCSharpFile(source, options, CreateDefaultCorralOptions("$Main_Wrapper_Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("NullPtrInstrumentation")]
    public void TestNullPointerInstrumentation11()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static void Main() {
        int [] a = null;
        // This should be instrumented for null pointer dereference with an assertion that should fail.
        int x = a.Length;
    }
}
        ";
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.CheckNullDereferences = true;
        var corralResult = TestSingleCSharpFile(source, options, CreateDefaultCorralOptions("$Main_Wrapper_Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("NullPtrInstrumentation")]
    public void TestNullPointerInstrumentation12()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static void Main() {
        int [] a = null;
        // This should be instrumented for null pointer dereference with an assertion that should fail.
        a[0] = 5;
    }
}
        ";
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.CheckNullDereferences = true;
        var corralResult = TestSingleCSharpFile(source, options, CreateDefaultCorralOptions("$Main_Wrapper_Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("DocumentedImprecision")]
    public void TestCast2()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static int ToInt(double d)
    {
        // Cast is modelled as havoc
        return (int) d;
    }
    public void Main() {
        int i = ToInt(5.0);
        Contract.Assert(i == 5);
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("VariableAssignment")]
    public void TestLargeIntegers1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static void Main()
    {
        int x = 5000000;
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("VariableAssignment")]
    public void TestLargeIntegers2()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static void Main()
    {
        int x = 5000000;
        int y = 1000000;
        int z = 4000000;
        Contract.Assert(x == y + z);
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("VariableAssignment")]
    public void TestLargeIntegers3()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static void Main()
    {
        int x = 5000000;
        int y = 1000000;
        int z = 4000001;
        Contract.Assert(x == y + z);
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Repro")]
    public void TestStringNull1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static void Foo(String s) {
        if (s != null) {
            Contract.Assert(false);
        }
    }
    public static void Main()
    {
        string s = ""Hello world"";
        Foo(s);
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Repro")]
    public void TestArray1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static void Main()
    {
        string[] s = new string[5];
        s[1] = ""Hello World"";
        s[2] = ""Hello World"";
        Contract.Assert(s[1] == s[2]);
    }
}
        ";
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpFile(source, options, CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Repro")]
    public void TestAssumeTypeArguments1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Base {
    virtual public void Foo() {
    }
}
class Derived : Base {
    override public void Foo() {
    }
}
class Test {
    public static void Main(Base b)
    {
        b.Foo();
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main$Base"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Repro")]
    public void TestAxiomsGenerics1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;

class Holds<T3> {
 T3 other_value;
 virtual public void Foo() {
  Contract.Assert(false);
 }
}

class SubHolds<T1, T2> : Holds<T2> {
 T1 value_t1;
 override public void Foo() {
 }
}

class Test {
  public static void Main() {
    Holds<String> holds_ints = new SubHolds<Int32, String>();
    holds_ints.Foo();
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Repro")]
    public void TestAxiomsGenerics2()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;

class InnerBase<T3> {
 T3 other_value;
}

class A<T> {
   class Inner : InnerBase<T> {
   }
}

class Test {
  public static void Main() {
    A<String> a = new A<String>();
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Arrays")]
    public void TestArrays1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;

class Test {
  public static void Main() {
    int [] arr = new int[3];
    var copy_arr = arr;
    copy_arr[2] = 4;
    Contract.Assert(arr[2] == 4);
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    // Remove when issue #63 is fixed.
    [Ignore(""), Test, Category("Repro")]
    public void TestBidimensionalArrays1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;

class Test {
  public static void Main() {
    int [,] arr = new int[2,4];
    arr[0,2] = 4;
    arr[1,2] = 5;
    Contract.Assert(arr[0,2] == 4);
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Fernan")]
    public void TestConstructors1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class A {
  int value;
  public A(int x) {
    value = x;
    Contract.Assert(false);
  }
  public A() : this(5) {
  }
}
class Test {
    public static void Main()
    {
        A a = new A();
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Fernan")]
    public void TestConstructors2()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class A {
  int value;
  public A(int x) {
    value = x;
  }
  public A() : this(5) {
    Contract.Assert(value == 5);
  }
}
class Test {
    public static void Main()
    {
        A a = new A();
    }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Fernan")]
    public void TestSizeof1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Test {
    public static void Main()
    {
        Contract.Assert(2 == sizeof(short));
    }
}
            ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Fernan")]
    public void TestBase1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
class Derived : Base {
  public override void Foo() {
    base.Foo();
  }
}
class Base {
  public virtual void Foo() {
    Contract.Assert(false);
  }
}

class Test {
    public static void Main()
    {
        Derived d = new Derived();
        d.Foo();
    }
}
            ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("AvRegressions")]
    public void TestDynamicDispatchGenerics3()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;

class Holds<T> {
 T value;
 virtual public void Foo() {
 }
}

class SubHolds<T> : Holds<T> {
 override public void Foo() {
  Contract.Assert(false);
 }
}

class Test {
  public static void Main() {
    Holds<Int32> holds_ints = new SubHolds<Int32>();
    holds_ints.Foo();
  }
}
        ";
        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("AvRegressions"), Timeout(30000)]
    public void TestForeachOK()
    {
        NUnitTests.TestOptions t = new NUnitTests.TestOptions();
        t.UseCollectionStubs();
        t.UsePoirotStubs();

        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions() { MainProcedure = "PoirotMain.Main" };
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.ForEachOK.cs", t, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions")]
    public void TestListSumOK()
    {
        NUnitTests.TestOptions t = new NUnitTests.TestOptions();
        t.UseCollectionStubs();
        t.UsePoirotStubs();

        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions() { MainProcedure = "$Main_Wrapper_PoirotMain.Main" };
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.ListSum.cs", t, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestListSum2Fail()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.ListSum2.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("PoirotMain.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestForeach2Bug()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.ForEach2Bug.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("PoirotMain.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestComplexExprBug()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.ComplexExpr.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("PoirotMain.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestComplexExpr2()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.ComplexExpr.cs", options, CreateDefaultCorralOptions("PoirotMain.ShouldPass"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestDoubleQuestion1()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.DoubleQuestion.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("PoirotMain.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestDoubleQuestion2()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.DoubleQuestion.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("PoirotMain.ShouldPass"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestDoubleQuestionBug()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.DoubleQuestion.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("PoirotMain.ShouldFail$IntContainer"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestEx1()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.ex1.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("cMain.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions")]
    public void TestEx2()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.ex2.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("cMain.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(0)]
    public void TestAbstractClassDLL()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.AbstractClassDLL.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.doStuff$Int$System.Int32"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestArgsBug1()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Args.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main$System.Stringarray"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestArgsBug2()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Args.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main2$System.Stringarray"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions"), Timeout(0)]
    public void TestStringEqOperator1()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.stringEq.cs", options, CreateDefaultCorralOptions("Test.ShouldFail"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator2()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.stringEq.cs", options, CreateDefaultCorralOptions("Test.ShouldPass"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator3()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.stringEq.cs", options, CreateDefaultCorralOptions("Test.ShouldPass2"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator4()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.stringEq.cs", options, CreateDefaultCorralOptions("Test.IneqShouldFail"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator5()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.stringEq.cs", options, CreateDefaultCorralOptions("Test.IneqShouldPass"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator6()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.stringEq.cs", options, CreateDefaultCorralOptions("Test.IneqShouldPass2"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator7()
    {
        var source = @"
using System.Diagnostics.Contracts;

class Test {
  public static void Main() {
    var s1 = ""Hello world"";
    var s2 = ""Hello world"";
    Contract.Assert(s1 == s2);
  }
}
        ";
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpFile(source, options, CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator8()
    {
        var source = @"
using System.Diagnostics.Contracts;

class Test {
  public static void Main() {
    var s1 = ""Hello world"";
    var s2 = ""Hello"";
    Contract.Assert(s1 != s2);
  }
}
        ";
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpFile(source, options, CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqWithEquals1()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.stringEqWithEquals.cs", options, CreateDefaultCorralOptions("Test.ShouldFail"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqWithEquals2()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.stringEqWithEquals.cs", options, CreateDefaultCorralOptions("Test.ShouldPass"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqWithEquals3()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.stringEqWithEquals.cs", options, CreateDefaultCorralOptions("Test.ShouldPass2"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSet1()
    {
        NUnitTests.TestOptions t = new NUnitTests.TestOptions();
        t.UsePoirotStubs();

        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions() { MainProcedure = "PoirotMain.ShouldPass1" };
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Set.cs", t, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSet2()
    {
        NUnitTests.TestOptions t = new NUnitTests.TestOptions();
        t.UsePoirotStubs();

        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions() { MainProcedure = "PoirotMain.ShouldPass2" };
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Set.cs", t, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(30000)]
    public void TestSet3()
    {
        NUnitTests.TestOptions t = new NUnitTests.TestOptions();
        t.UseCollectionStubs();
        t.UsePoirotStubs();

        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions() { MainProcedure = "PoirotMain.ShouldPass3" };
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Set.cs", t, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(20000)]
    public void TestSet4()
    {
        NUnitTests.TestOptions t = new NUnitTests.TestOptions();
        t.UseCollectionStubs();
        t.UsePoirotStubs();
        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions() { MainProcedure = "PoirotMain.ShouldPass4" };
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Set.cs", t, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSet5()
    {
        NUnitTests.TestOptions t = new NUnitTests.TestOptions();
        t.UsePoirotStubs();

        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions() { MainProcedure = "PoirotMain.ShouldPass5" };
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Set.cs", t, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(30000)]
    public void TestSet6()
    {
        NUnitTests.TestOptions t = new NUnitTests.TestOptions();
        t.UseCollectionStubs();
        t.UsePoirotStubs();

        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions() { MainProcedure = "PoirotMain.ShouldPass6" };
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Set.cs", t, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSetBug1()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Set.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("PoirotMain.ShouldFail1"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSetBug2()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Set.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("PoirotMain.ShouldFail2"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSetBug3()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Set.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("PoirotMain.ShouldFail3"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSetBug4()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Set.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("PoirotMain.ShouldFail4"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSetBug5()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Set.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("PoirotMain.ShouldFail5"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(30000)]
    public void TestSetBug6()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AvRegressions.Set.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("PoirotMain.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
}

class TestsXor : TestsBase
{
    [Test, Category("DocumentedImprecision")]
    public void XorImprecision()
    {
        var source = @"
using System.Diagnostics.Contracts;

class Test {
  public static void Main() {
    int a = 4;
    int b = 2;
    int c = a ^ b;;
    Contract.Assert(c == 6);
  }
}
        ";

        var corralResult = TestSingleCSharpFile(source, CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
}
class TestsManu : TestsBase
{
    [Ignore(""), Test, Timeout(10000), Category("NotImplemented")]
    public void Subtype()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test7$DynamicDispatch.Animal"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("NotImplemented")]
    public void Loops()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Loops.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Loops.Acum$System.Int32"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void SyntaxTest1([Values(MemOpt.Addresses, MemOpt.Mixed)]MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AddressesSimple.cs", options, CreateDefaultCorralOptions("Test.AddressesSimple.SyntaxTest1"));
        
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void SyntaxTest4([Values(MemOpt.Addresses, MemOpt.Mixed)]MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AddressesSimple.cs", options, CreateDefaultCorralOptions("Test.AddressesSimple.SyntaxTest4"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void SyntaxTest3([Values(MemOpt.Addresses, MemOpt.Mixed)]MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AddressesSimple.cs", options, CreateDefaultCorralOptions("Test.AddressesSimple.SyntaxTest3$Test.AddressesSimple"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void Test1([Values(MemOpt.Addresses, MemOpt.Mixed)]MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        options.Z3Strings = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AddressesSimple.cs", options, CreateDefaultCorralOptions("Test.AddressesSimple.Test1"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void Test2([Values(MemOpt.Addresses, MemOpt.Mixed)]MemOpt memOp)
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AddressesSimple.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.AddressesSimple.Test2$Test.AddressesSimple"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void Test3([Values(MemOpt.Addresses, MemOpt.Mixed)]MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AddressesSimple.cs", options, CreateDefaultCorralOptions("Test.AddressesSimple.Test3$Test.AddressesSimple"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Addresses")]
    public void Test4([Values(MemOpt.Addresses, MemOpt.Mixed)]MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AddressesSimple.cs", options, CreateDefaultCorralOptions("Test.AddressesSimple.Test4$Test.AddressesSimple"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void Test5([Values(MemOpt.Addresses, MemOpt.Mixed)]MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AddressesSimple.cs", options, CreateDefaultCorralOptions("Test.AddressesSimple.Test5$Test.AddressesSimple"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Addresses")]
    public void Test6([Values(MemOpt.Addresses, MemOpt.Mixed)]MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AddressesSimple.cs", options, CreateDefaultCorralOptions("Test.AddressesSimple.Test6$Test.AddressesSimple"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void Test7([Values(MemOpt.Addresses, MemOpt.Mixed)]MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AddressesSimple.cs", options, CreateDefaultCorralOptions("Test.AddressesSimple.Test7$Test.AddressesSimple"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void Test8_NoBugs([Values(MemOpt.Addresses, MemOpt.Mixed)]MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AddressesSimple.cs", options, CreateDefaultCorralOptions("Test.AddressesSimple.Test8_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void Test8_Bugged([Values(MemOpt.Addresses, MemOpt.Mixed)]MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.AddressesSimple.cs", options, CreateDefaultCorralOptions("Test.AddressesSimple.Test8_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void ModOperator1()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.BinaryOperators.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.BinaryOperators.ModTest1"));
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu")]
    public void ModOperator2()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.BinaryOperators.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.BinaryOperators.ModTest2"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Ignore(""), Test, Category("NotImplemented")]
    public void ModOperator3()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.BinaryOperators.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.BinaryOperators.ModTest3"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void SplitFields1()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.SplitFields.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.SplitFields.test1"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses"), Category("RefKeyword")]
    public void RefKeyword1([Values(MemOpt.Addresses, MemOpt.Mixed)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.RefKeyword.cs", options, CreateDefaultCorralOptions(@"Test.RefKeyword.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses"), Category("RefKeyword"), Sequential]
    public void RefKeyword2([Values(MemOpt.Addresses, MemOpt.Mixed)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.RefKeyword.cs", options, CreateDefaultCorralOptions(@"Test.RefKeyword.TestField$Test.RefKeyword"));
        Assert.IsTrue(corralResult.NoBugs());
    }


    // ************************************* Boxing ******************************

    [Test, Category("Manu")]
    public void Boxing1()
    {
        NUnitTests.TestOptions t = new NUnitTests.TestOptions();
        t.UsePoirotStubs();

        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions() { MainProcedure = "Test.Boxing.Test1" };
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Boxing.cs", t, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    // MOVE ONEC EDGARD FIXED ISSUE https://github.com/edgardozoppi/analysis-net/issues/7
    [Ignore(""), Test, Category("Repro")]
    public void Boxing2()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Boxing.cs", options, CreateDefaultCorralOptions(@"Test.Boxing.Test2"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void Boxing3()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Boxing.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Boxing.Test3"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    // MOVE ONEC EDGARD FIXED ISSUE https://github.com/edgardozoppi/analysis-net/issues/7
    [Ignore(""), Test, Category("Repro")]
    public void Boxing4()
    {

        var source = @"
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
class Test {
  public static void Main() {
    List<Object> h = new List<Object>();
    h.Add(false);
    bool pos0 = (bool) h[0];
    Contract.Assert(pos0 == false);
  }
}
        ";
        NUnitTests.TestOptions t = new NUnitTests.TestOptions();
        t.UseCollectionStubs();

        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions() { MainProcedure = "Test.Main" };
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Boxing4.cs", t, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* Array Atomic Initilization ******************************

    [Test, Category("Manu")]
    public void ArrayAtomicInit1_NoBugs()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.ArrayAtomicInit1_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    // Remove when issue #51 is solved.
    [Ignore(""), Test, Category("Repro")]
    public void ArrayAtomicInitThrowsException1()
    {
        NUnit.Framework.TestDelegate t = () =>
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
class Test {
  public static void Main() {
    int[] b = { 1, 2, 3, 4, 5 };
  }
}
        ";
            var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.ArrayAtomicInitThrowsException1.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.Main"));
            Assert.Fail();
        };

        Assert.Throws<NotImplementedException>(t);


    }

    [Test, Category("Arrays")]
    public void ArrayAtomicInit1_Bugged([Values(MemOpt.Addresses/*, MemOpt.Mixed,*/, MemOpt.SplitFields)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.ArrayAtomicInit1_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Arrays"), Category("Addresses")]
    public void ArrayAtomicInit1_Bugged_Addresses([Values(MemOpt.Addresses/*, MemOpt.Mixed,*/, MemOpt.SplitFields)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.ArrayAtomicInit1_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Arrays")]
    public void ArrayAtomicInit2_Bugged([Values(MemOpt.Addresses/*, MemOpt.Mixed,*/, MemOpt.SplitFields)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.ArrayAtomicInit2_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Arrays"), Category("Addresses")]
    public void ArrayAtomicInit2_Bugged_Addresses([Values(MemOpt.Addresses/*, MemOpt.Mixed,*/, MemOpt.SplitFields)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.ArrayAtomicInit2_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void ArrayAtomicInit3_Bugged([Values(MemOpt.Addresses/*, MemOpt.Mixed,*/, MemOpt.SplitFields)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.ArrayAtomicInit3_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void ArrayAtomicInit4_NoBugs([Values(MemOpt.Addresses/*, MemOpt.Mixed,*/, MemOpt.SplitFields)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.ArrayAtomicInit4_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* Switch ******************************

    [Test, Category("Manu")]
    public void Switch1_NoBugs([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Switch.cs", options, CreateDefaultCorralOptions(@"Test.Switch.test1_NoBugs$System.Int32"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void Switch1_Bugged([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Switch.cs", options, CreateDefaultCorralOptions(@"Test.Switch.test1_Bugged$System.Int32"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void Switch2_NoBugs([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Switch.cs", options, CreateDefaultCorralOptions(@"Test.Switch.test2_NoBugs$System.Int32"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void Switch2_Bugged([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Switch.cs", options, CreateDefaultCorralOptions(@"Test.Switch.test2_Bugged$System.Int32"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void Switch3_NoBugs([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Switch.cs", options, CreateDefaultCorralOptions(@"Test.Switch.test3_NoBugs$System.Int32"));
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu")]
    public void Switch3_Bugged([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Switch.cs", options, CreateDefaultCorralOptions(@"Test.Switch.test3_Bugged$System.Int32"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void Switch4_NoBugs([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Switch.cs", options, CreateDefaultCorralOptions(@"Test.Switch.test4_NoBugs$System.Int32"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void Switch4_Bugged([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Switch.cs", options, CreateDefaultCorralOptions(@"Test.Switch.test4_Bugged$System.Int32"));
        Assert.IsTrue(corralResult.AssertionFails());
    }


    // ************************************* MissingConstructorInitializations ******************************

    [Test, Category("Manu")]
    public void MissingConstructorInitializations1([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.MissingConstructorInitializations.cs", options, CreateDefaultCorralOptions(@"Test.MissingConstructorInitializations.testInt"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void MissingConstructorInitializations2([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.MissingConstructorInitializations.cs", options, CreateDefaultCorralOptions(@"Test.MissingConstructorInitializations.testFloat"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void MissingConstructorInitializations3([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.MissingConstructorInitializations.cs", options, CreateDefaultCorralOptions(@"Test.MissingConstructorInitializations.testDouble"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void MissingConstructorInitializations4([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.MissingConstructorInitializations.cs", options, CreateDefaultCorralOptions(@"Test.MissingConstructorInitializations.testRef"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void MissingConstructorInitializations5([Values(MemOpt.Addresses, MemOpt.Mixed, MemOpt.SplitFields)] MemOpt memOp)
    {
        ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.MissingConstructorInitializations.cs", options, CreateDefaultCorralOptions(@"$Main_Wrapper_Test.MissingConstructorInitializations.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* DYNAMIC DISPATCH ******************************

    [Test, Category("Manu")]
    public void DynamicDispatch1_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test1_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void DynamicDispatch1_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test1_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch2_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test2_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch2_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test2_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch3_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test3_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch3_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test3_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch4_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test4_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch4_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test4_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch5_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test5_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch5_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test5_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch6_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test8_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch6_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test8_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch7_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test9_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch7_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicDispatch.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"DynamicDispatch.DynamicDispatch.test9_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }


    // ************************************* DELEGATES ******************************


    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesGenerics1()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.generics1"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesGenerics2()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.generics2"));
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesGenerics3()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.generics3"));
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesGenerics4()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.generics4"));
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DelegateEmptyGroup()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.EmtpyGroup$Test.Delegates.DelegateEmpty"));
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates1_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.Delegates1_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates2_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.Delegates2_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates3_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.Delegates3_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates4_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.Delegates4_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates5_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.Delegates5_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates6_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.Delegates6_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates1_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.Delegates1_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates2_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.Delegates2_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates3_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.Delegates3_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates4_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.Delegates4_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates5_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.Delegates5_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates6_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.Delegates6_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch1_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.DelegatesDynamicDispatch1_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch2_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.DelegatesDynamicDispatch2_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch3_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.DelegatesDynamicDispatch3_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch4_NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.DelegatesDynamicDispatch4_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch1_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.DelegatesDynamicDispatch1_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch2_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.DelegatesDynamicDispatch2_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch3_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.DelegatesDynamicDispatch3_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(20000)]
    public void DelegatesDynamicDispatch4_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.DelegatesDynamicDispatch4_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch5_Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Delegates.DelegatesDynamicDispatch5_Bugged$Test.Delegates.Dog"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    // ************************************* ARRAYS ******************************

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayStoreLoad1()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.arrayStoreLoad1"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayStoreLoad2()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.arrayStoreLoad2"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayStoreLoad3()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.arrayStoreLoad3"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayStoreLoad4()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.arrayStoreLoad4"));
        Assert.IsTrue(corralResult.AssertionFails());
    }


    // to be implemented
    /*[TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayStoreLoad3()
    {
        var corralResult = TestSingleCSharpFile("NUnitTests.Resources.Arrays.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions(@"Test.Arrays.arrayStoreLoad3"));
        Assert.IsTrue(corralResult.AssertionFails());
    }*/

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayCreate1()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.arrayCreate1"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayCreate2()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.arrayCreate2"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayOfArrays1()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.arrayOfArrays1"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayOfArrays2()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.arrayOfArrays2"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayOfArrays3()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.arrayOfArrays3"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayOfArrays4()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.arrayOfArrays2"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayLength()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.arrayLength$System.Int32array"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayLengthIteration()
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions(@"Test.Arrays.arrayFor"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Arrays"), Timeout(10000)]
    public void ArgsLength([Values(MemOpt.Addresses/*, MemOpt.Mixed,*/, MemOpt.SplitFields)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions("Test.Arrays.ArgsLength$System.Stringarray"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Arrays"), Category("Addresses")]
    public void ArgsLengthAddresses([Values(MemOpt.Addresses/*, MemOpt.Mixed,*/, MemOpt.SplitFields)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.AtomicInitArray = true;
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Arrays.cs", options, CreateDefaultCorralOptions("Test.Arrays.ArgsLength$System.Stringarray"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    // ************************************* Immutable Arguments ******************************

    [Test, Category("Manu"), Timeout(10000)]
    public void ImmutableArgumentTest1()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.ImmutableArgument.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.ImmutableArgument.test1$System.Int32"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ImmutableArgumentTest2()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.ImmutableArgument.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.ImmutableArgument.test2$System.Int32"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    // ************************************* Split Fields ******************************

    [Test, Category("Manu"), Timeout(10000)]
    public void SplitFields2()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.SplitFields.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.SplitFields.test2$Test.SplitFields.Foo"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void SplitFields3()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.SplitFields.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.SplitFields.test3"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void SplitFields4()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.SplitFields.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.SplitFields.test4"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void SplitFields5()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.SplitFields.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.SplitFields.test5"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void SplitFields6()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.SplitFields.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.SplitFields.test6"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* Unary Operations ******************************

    [Test, Category("Manu"), Timeout(10000)]
    public void UnaryOperations1()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.UnaryOperations.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.UnaryOperations.test1"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void UnaryOperations2()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.UnaryOperations.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.UnaryOperations.test2$System.Int32"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void UnaryOperations3()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.UnaryOperations.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.UnaryOperations.test3"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void UnaryOperations4()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.UnaryOperations.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.UnaryOperations.test1_float"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void UnaryOperations5()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.UnaryOperations.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.UnaryOperations.test2_float$System.Single"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void UnaryOperations6()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.UnaryOperations.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.UnaryOperations.test3_float"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(0)]
    public void DynamicKeyword1()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.DynamicKeyword.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.DynamicKeyword.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Ignore(""), Test, Category("Async")]
    public void Async1()
    {
        var source = @"
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

internal class Async
{
    public static void Async1()
    {
        Task<int> task = DoSomethingAsync(10);
        task.Wait();
        int x = task.Result;
        Contract.Assert(x == 11);
    }

    private static async Task<int> DoSomethingAsync(int initValue)
    {
        int count = initValue + 1;
        return count;
    }
}
            ";
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async1.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Async.Async1"));
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Ignore(""), Test, Category("Async"), Category("NotImplemented")]
    public void Await1()
    {
        var source = @"
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

internal class Async
{
    public static void Await1()
    {
        Task<int> task = DoSomethingAsync(10);
        task.Wait();
        int x = task.Result;
        Contract.Assert(x == 11);
    }

    private static async Task<int> DoSomethingAsync(int initValue)
    {
        int count = initValue + 1;
        await RunAsync();
        return count;
    }
    private static Task RunAsync()
    {
        return Task.CompletedTask;
    }
}
            ";
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Await1.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Async.Await1"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* Exceptions ******************************

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest1NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTest1NoBugs.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest1Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTest1Bugged.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest2NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTest2NoBugs.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest2Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTest2Bugged.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest3NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTest3NoBugs.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest3Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTest3Bugged.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest5NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTest5NoBugs.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest5Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTest5Bugged.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(30000)]
    public void ExceptionTest6NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTest6NoBugs.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest6Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTest6Bugged.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest7NoBugs()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTest7NoBugs.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest7Bugged()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTest7Bugged.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTestInitializeExceptionVariable()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("Test.ExceptionTestInitializeExceptionVariable.test"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTestRethrow1()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTestRethrow1.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(20000)]
    public void ExceptionTestRethrow2()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Exceptions.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.ExceptionTestRethrow2.Main"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Ignore(""), Test, Category("Repro")]
    // Remove when the type error is resolved: https://github.com/edgardozoppi/analysis-net/issues/10#issuecomment-416497029
    public void TestExceptionsWhen()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.TestExceptionsWhen.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("TestExceptionsWhen.Main"));
    }

    [Test, Category("Repro")]
    public void Delegates1()
    {
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Delegates1.cs", CreateDefaultTinyBctOptions(), CreateDefaultCorralOptions("$Main_Wrapper_Test.Main"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Struct"), Timeout(0)]
    public void TestStructs1_NoBugs([Values(MemOpt.Addresses, MemOpt.Mixed)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Struct.cs", options, CreateDefaultCorralOptions(@"Test.TestStructs.Test1_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Struct"), Timeout(0)]
    public void TestStructs2_NoBugs([Values(MemOpt.Addresses, MemOpt.Mixed)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Struct.cs", options, CreateDefaultCorralOptions(@"Test.TestStructs.Test2_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Struct"), Timeout(0)]
    public void TestStructs3_NoBugs([Values(MemOpt.Addresses, MemOpt.Mixed)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Struct.cs", options, CreateDefaultCorralOptions(@"Test.TestStructs.Test3_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Struct"), Timeout(0)]
    public void TestStructs4_NoBugs([Values(MemOpt.Addresses, MemOpt.Mixed)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Struct.cs", options, CreateDefaultCorralOptions(@"Test.TestStructs.Test4_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Struct"), Timeout(0)]
    public void TestStructs1_Bugged([Values(MemOpt.Addresses, MemOpt.Mixed)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Struct.cs", options, CreateDefaultCorralOptions(@"Test.TestStructs.Test1_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Struct"), Timeout(0)]
    public void TestStructs2_Bugged([Values(MemOpt.Addresses, MemOpt.Mixed)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Struct.cs", options, CreateDefaultCorralOptions(@"Test.TestStructs.Test2_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Struct"), Timeout(0)]
    public void TestStructs3_Bugged([Values(MemOpt.Addresses, MemOpt.Mixed)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;

        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Struct.cs", options, CreateDefaultCorralOptions(@"Test.TestStructs.Test3_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Struct"), Timeout(0)]
    public void TestStructs4_Bugged([Values(MemOpt.Addresses, MemOpt.Mixed)] MemOpt memOp)
    {
        TinyBCT.ProgramOptions options = new ProgramOptions();
        options.MemoryModel = memOp;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Struct.cs", options, CreateDefaultCorralOptions(@"Test.TestStructs.Test4_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
}

public class TestStringHelpers
{

    [Test, Category("TestsForHelpers"), Timeout(10000)]
    public void TestReplaceIllegalCharsSimple()
    {
        Assert.AreEqual("Hello#1#World", TinyBCT.Helpers.Strings.ReplaceIllegalChars(@"Hello:World"));
    }
    [Test, Category("TestsForHelpers"), Timeout(10000)]
    public void TestReplaceSpaces1()
    {
        Assert.AreEqual("Hello#0#World", TinyBCT.Helpers.Strings.ReplaceIllegalChars(@"Hello World"));
    }
    [Test, Category("TestsForHelpers"), Timeout(10000)]
    public void TestReplaceSpaces2()
    {
        Assert.AreEqual("Hello#0#World", TinyBCT.Helpers.Strings.ReplaceIllegalChars(@"Hello World"));
    }
    [Test, Category("TestsForHelpers"), Timeout(10000)]
    public void TestReplaceIllegalCharsEscape()
    {
        Assert.AreEqual("Hello##World", TinyBCT.Helpers.Strings.ReplaceIllegalChars(@"Hello#World"));
    }
}

class TestsAzure : TestsBase
{
    private string CopyToSystemTempDir(string file, string newExtension = ".dll")
    {
        var tmpDll = Path.ChangeExtension(Path.GetTempFileName(), ".dll");
        File.WriteAllBytes(tmpDll, File.ReadAllBytes(file));
        return tmpDll;
    }

    [Test]
    public void Test1_NoBugs()
    {
        var dllLocation = CopyToSystemTempDir(typeof(HelloWorld.ReferenceToHelloWorldDll).Assembly.Location);
        ProgramOptions programOptions = new ProgramOptions();
        programOptions.StubGettersSetters = true;
        programOptions.Z3Strings = true;
        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions();
        corralOptions.MainProcedure = "HelloWorld.Function1.Run_NoBugs$System.Net.Http.HttpRequestMessage$Microsoft.Azure.WebJobs.Host.TraceWriter";
        var corralResult = TestDll(dllLocation, programOptions, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void Test1_Bugged()
    {
        var dllLocation = CopyToSystemTempDir(typeof(HelloWorld.ReferenceToHelloWorldDll).Assembly.Location);
        ProgramOptions programOptions = new ProgramOptions();
        programOptions.StubGettersSetters = true;
        //programOptions.Z3Strings = true;
        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions();
        corralOptions.MainProcedure = "HelloWorld.Function1.Run_Bugged$System.Net.Http.HttpRequestMessage$Microsoft.Azure.WebJobs.Host.TraceWriter";
        var corralResult = TestDll(dllLocation, programOptions, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test2_NoBugs()
    {
        var dllLocation = CopyToSystemTempDir(typeof(HelloWorld.ReferenceToHelloWorldDll).Assembly.Location);
        ProgramOptions programOptions = new ProgramOptions();
        programOptions.StubGettersSetters = true;
        programOptions.Z3Strings = true;
        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions();
        corralOptions.MainProcedure = "HelloWorld.Function1.Run2_NoBugs$System.Net.Http.HttpRequestMessage$Microsoft.Azure.WebJobs.Host.TraceWriter";
        var corralResult = TestDll(dllLocation, programOptions, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void Test2_Bugged()
    {
        var dllLocation = CopyToSystemTempDir(typeof(HelloWorld.ReferenceToHelloWorldDll).Assembly.Location);
        ProgramOptions programOptions = new ProgramOptions();
        programOptions.StubGettersSetters = true;
        programOptions.Z3Strings = true;
        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions();
        corralOptions.MainProcedure = "HelloWorld.Function1.Run2_Bugged$System.Net.Http.HttpRequestMessage$Microsoft.Azure.WebJobs.Host.TraceWriter";
        var corralResult = TestDll(dllLocation, programOptions, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void CreateEmail_NoBugs()
    {
        var dllLocation = CopyToSystemTempDir(typeof(BillingFunctionsModified.NotifyInvoiceFunc).Assembly.Location);
        ProgramOptions programOptions = new ProgramOptions();
        programOptions.StubGettersSetters = true;
        programOptions.Z3Strings = true;
        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions();
        corralOptions.MainProcedure = "BillingFunctionsModified.NotifyInvoiceFunc.CreateEmail_NoBugs$Shared.Printing.InvoiceNotificationRequest";
        var corralResult = TestDll(dllLocation, programOptions, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void CreateEmail_Bugged()
    {
        var dllLocation = CopyToSystemTempDir(typeof(BillingFunctionsModified.NotifyInvoiceFunc).Assembly.Location);
        ProgramOptions programOptions = new ProgramOptions();
        programOptions.StubGettersSetters = true;
        programOptions.Z3Strings = true;
        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions();
        corralOptions.MainProcedure = "BillingFunctionsModified.NotifyInvoiceFunc.CreateEmail_Bugged$Shared.Printing.InvoiceNotificationRequest";
        var corralResult = TestDll(dllLocation, programOptions, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Ignore("Corral is taking too long to verify this.")]
    public void CreateSMS_NoBugs()
    {
        var dllLocation = CopyToSystemTempDir(typeof(BillingFunctionsModified.NotifyInvoiceFunc).Assembly.Location);
        ProgramOptions programOptions = new ProgramOptions();
        programOptions.StubGettersSetters = true;
        programOptions.Z3Strings = true;
        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions();
        corralOptions.MainProcedure = "BillingFunctionsModified.NotifyInvoiceFunc.CreateSMS_NoBugs$Shared.Printing.InvoiceNotificationRequest";
        var corralResult = TestDll(dllLocation, programOptions, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Ignore("Corral is taking too long to verify this.")]
    public void CreateSMS_Bugged()
    {
        var dllLocation = CopyToSystemTempDir(typeof(BillingFunctionsModified.NotifyInvoiceFunc).Assembly.Location);
        ProgramOptions programOptions = new ProgramOptions();
        programOptions.StubGettersSetters = true;
        programOptions.Z3Strings = true;
        CorralRunner.CorralOptions corralOptions = new CorralRunner.CorralOptions();
        corralOptions.MainProcedure = "BillingFunctionsModified.NotifyInvoiceFunc.CreateSMS_Bugged$Shared.Printing.InvoiceNotificationRequest";
        var corralResult = TestDll(dllLocation, programOptions, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }



    //BillingFunctionsModified.NotifyInvoiceFunc.CreateEmail$Shared.Printing.InvoiceNotificationRequest
}

class TestsStrings : TestsBase
{
    [Test]
    public void Test1_NoBugs()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.StringOperations.cs", options, CreateDefaultCorralOptions("NUnitTests.Resources.StringTest.Test1_NoBugs$System.String"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void Test1_Bugged()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.StringOperations.cs", options, CreateDefaultCorralOptions("NUnitTests.Resources.StringTest.Test1_Bugged$System.String"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test2_NoBugs()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.StringOperations.cs", options, CreateDefaultCorralOptions("NUnitTests.Resources.StringTest.Test2_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void Test2_Bugged()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.StringOperations.cs", options, CreateDefaultCorralOptions("NUnitTests.Resources.StringTest.Test2_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test3_NoBugs()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.StringOperations.cs", options, CreateDefaultCorralOptions("NUnitTests.Resources.StringTest.Test3_NoBugs"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void Test3_Bugged()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.StringOperations.cs", options, CreateDefaultCorralOptions("NUnitTests.Resources.StringTest.Test3_Bugged"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test4_NoBugs()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.StringOperations.cs", options, CreateDefaultCorralOptions("NUnitTests.Resources.StringTest.Test4_NoBugs$System.Boolean$System.String"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void Test4_Bugged()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.StringOperations.cs", options, CreateDefaultCorralOptions("NUnitTests.Resources.StringTest.Test4_Bugged$System.Boolean$System.String"));
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test5_NoBugs()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.StringOperations.cs", options, CreateDefaultCorralOptions("NUnitTests.Resources.StringTest.Test5_NoBugs$System.String"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void Test5_Bugged()
    {
        var options = CreateDefaultTinyBctOptions();
        options.Z3Strings = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.StringOperations.cs", options, CreateDefaultCorralOptions("NUnitTests.Resources.StringTest.Test5_Bugged$System.String"));
        Assert.IsTrue(corralResult.AssertionFails());
    }
} 

class TestAsync : TestsBase
{
    [Test]
    public void Test1_EmptyMethod()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.AsyncMethod.cs", options, CreateDefaultCorralOptions("AsyncClass.EmptyAsyncMethod$System.TimeSpan$System.Int32"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void Test1_OneDelayMethod()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.AsyncMethod.cs", options, CreateDefaultCorralOptions("AsyncClass.OneDelayMethod$System.TimeSpan$System.Int32"));
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void Test1_TwoDelaysMethod()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("AsyncClass.TwoDelaysMethod$System.TimeSpan$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.AsyncMethod.cs", options, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test]
    public void Test_Atomic_Transaction_Reachability()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Atomic_Transaction_Reachability$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 3;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestAtomicTransactionReachability.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Withdraw_Reachability()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Withdraw_Reachability$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 3;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestWithdrawReachability.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Withdraw_Reachability_2()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Withdraw_Reachability_2$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestWithdrawReachability2.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Deposit_Reachability()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Deposit_Reachability$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 3;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestDepositReachability.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Deposit_Reachability_2()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Deposit_Reachability_2$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 3;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestDepositReachability2.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Atomic_Transaction_1()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Atomic_Transaction_1$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 3;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestAtomicTransaction1.cs", options, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void Test_Atomic_Transaction_2()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Atomic_Transaction_2$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 3;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestAtomicTransaction2.cs", options, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void Test_Atomic_Transaction_3()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Atomic_Transaction_3$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 3;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestAtomicTransaction3.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Atomic_Transaction_Withdraw_1()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Atomic_Transaction_Withdraw_1$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 3;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestAtomicTransactionWithdraw1.cs", options, corralOptions);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test]
    public void Test_Atomic_Transaction_Withdraw_2()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Atomic_Transaction_Withdraw_2$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 3;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestAtomicTransactionWithdraw2.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction1()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction1$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransaction1.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction2()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction2$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransaction2.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction3()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction3$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransaction3.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction4()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction4$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransaction4.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction5()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction5$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransaction5.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction6()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupport = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction6$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransaction6.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Withdraw_Reachability_Generics()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupportGenerics = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Withdraw_Reachability$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 3;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestWithdrawReachabilityGenerics.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Withdraw_Reachability_Generics_2()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupportGenerics = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Withdraw_Reachability_2$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestWithdrawReachabilityGenerics2.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction_Generics1()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupportGenerics = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction1$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransactionGenerics1.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction_Generics1_B()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupportGenerics = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction1$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransactionGenerics1_B.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction_Generics2()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupportGenerics = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction2$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransactionGenerics2.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction_Generics3()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupportGenerics = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction3$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransactionGenerics3.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction_Generics4()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupportGenerics = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction4$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransactionGenerics4.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction_Generics5()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupportGenerics = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction5$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransactionGenerics5.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test]
    public void Test_Interleaved_Transaction_Generics6()
    {
        var options = CreateDefaultTinyBctOptions();
        options.MemoryModel = MemOpt.Mixed;
        options.AsyncSupportGenerics = true;
        var corralOptions = CreateDefaultCorralOptions("Bank.Test_Interleaved_Transaction6$System.Int32");
        corralOptions.TrackAllVars = true;
        corralOptions.RecursionBound = 10;
        corralOptions.Cooperative = true;
        var corralResult = TestSingleCSharpResourceFile("NUnitTests.Resources.Async.TestInterleavedTransactionGenerics6.cs", options, corralOptions);
        Assert.IsTrue(corralResult.AssertionFails());
    }
}
