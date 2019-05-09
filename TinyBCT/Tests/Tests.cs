using System;
using System.Collections.Generic;
using Backend.ThreeAddressCode.Values;
using Microsoft.Cci;
using static Test.TestUtils;
using Backend;
using NUnit.Framework;

public partial class TestsHelpers
{
    private static string pathAuxDir = System.IO.Path.Combine(Test.TestUtils.rootTinyBCT, "Test", "TestUtilsAux");

    [Category("Call-Corral"), Test, Timeout(10000)]
    public void TestsCorralResultNoBugs()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"no_bugs.bpl"));
        Assert.IsTrue(corralResult.NoBugs());
        Assert.IsFalse(corralResult.AssertionFails());
        Assert.IsFalse(corralResult.SyntaxErrors());
    }
    [Category("Call-Corral")]
    [Test]
    [Timeout(10000)]
    public void TestsCorralResultAssertionFails()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"assertion_failure.bpl"));
        Assert.IsFalse(corralResult.NoBugs());
        Assert.IsTrue(corralResult.AssertionFails());
        Assert.IsFalse(corralResult.SyntaxErrors());
    }
    [Category("Call-Corral")]
    [Test]
    [Timeout(10000)]
    public void TestsCorralResultSyntaxError()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"syntax_error.bpl"));
        Assert.IsTrue(corralResult.SyntaxErrors());
    }
    [Category("Call-Corral")]
    [Test]
    [Timeout(10000)]
    public void TestsCorralResultNameResolutionError()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"name_resolution_error.bpl"));
        Assert.IsTrue(corralResult.NameResolutionErrors());
    }
    [Test]
    [Category("Call-Corral")]
    [Timeout(10000)]
    public void TestsCorralResultNoBugsSyntaxErrorCausesException()
    {
        NUnit.Framework.TestDelegate t = () =>
        {
            var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"syntax_error.bpl"));
            corralResult.NoBugs();
        };

        Assert.Throws<Test.TestUtils.CorralResult.CorralOutputException>(t);
    }
    [Category("Call-Corral")]
    [Test]
    [Timeout(10000)]
    public void TestsCorralResultAssertionFailsSyntaxErrorCausesException()
    {
        NUnit.Framework.TestDelegate t = () =>
        {
            var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"syntax_error.bpl"));
            corralResult.AssertionFails();
        };

        Assert.Throws<Test.TestUtils.CorralResult.CorralOutputException>(t);
    }

    [Category("Call-Corral")]
    [Test]
    [Timeout(10000)]
    public void TestsCorralResultGetOutputSyntaxErrorCausesException()
    {
        NUnit.Framework.TestDelegate t = () => {
            var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"syntax_error.bpl"));
            corralResult.getOutput();
        };
        
        Assert.Throws<Test.TestUtils.CorralResult.CorralOutputException>(t);
    }

    [Category("Call-CSC")]
    [Test]
    [Timeout(10000)]
    public void TestsCSCCompiles()
    {
        var source = @"
class Test {
	public static void Main()
	{
        int a = 5;
	}
}
        ";
        var path = System.IO.Path.Combine(pathAuxDir, "TestCSCCompiles");
        if (System.IO.Directory.Exists(path))
        {
            foreach (var f in System.IO.Directory.EnumerateFiles(path))
            {
                System.IO.File.Delete(f);
            }
        }
        else
        {
            System.IO.Directory.CreateDirectory(path);
        }
        Test.TestUtils.CompileWithCSC(source, "TestCSC", prefixDir: path);
        Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(path, "TestCSC.dll")));
    }

    [Category("Call-Corral"), Test, Timeout(10000)]
    public void TestsCorralIsPresent()
    {
        Console.WriteLine();
        Assert.IsTrue(
            System.IO.File.Exists(Test.TestUtils.corralPath),
            "This is not an actual test but a configuration check. Tests require corral.exe to be present. Configure Test.TestUtils.corralPath appropriately.");
    }

}


public class TestsBase
{
    [OneTimeSetUp]
    public static void ClassInit()
    {
        var location = System.IO.Path.GetDirectoryName(typeof(TestsBase).Assembly.Location);
        System.IO.Directory.SetCurrentDirectory(location);

        //Console.WriteLine(context.TestName);
        // Create temporary directory
        System.IO.Directory.CreateDirectory(pathTempDir);
        Test.TestUtils.DeleteAllFiles(pathTempDir);
        foreach (var dir in System.IO.Directory.EnumerateDirectories(pathTempDir))
        {
            Test.TestUtils.DeleteAllFiles(dir);
            System.IO.Directory.Delete(dir);
        }
    }
    [SetUp]
    public void TestInitialize()
    {
        TinyBCT.BoogieGenerator.singleton = null;

        TinyBCT.Helpers.methodsTranslated = new System.Collections.Generic.HashSet<string>();
        TinyBCT.BoogieLiteral.Strings.stringLiterals = new System.Collections.Generic.HashSet<string>();
        TinyBCT.Helpers.Strings.specialCharacters = new Dictionary<Char, int>() { { ' ', 0 } };

        TinyBCT.Translators.InstructionTranslator.ExternMethodsCalled = new System.Collections.Generic.HashSet<Microsoft.Cci.IMethodReference>();
        TinyBCT.Translators.InstructionTranslator.PotentiallyMissingMethodsCalled = new System.Collections.Generic.HashSet<Microsoft.Cci.IMethodReference>();
        TinyBCT.Translators.InstructionTranslator.MentionedClasses = new HashSet<ITypeReference>();
        TinyBCT.Translators.FieldTranslator.fieldNames = new Dictionary<IFieldReference, String>();

        TinyBCT.Translators.DelegateStore.methodIdentifiers = new Dictionary<IMethodReference, TinyBCT.DelegateExpression>();
        TinyBCT.Translators.DelegateStore.MethodGrouping = new Dictionary<string, ISet<IMethodReference>>();

        TinyBCT.Translators.TypeDefinitionTranslator.classes = new HashSet<ITypeDefinition>();
        TinyBCT.Translators.TypeDefinitionTranslator.parents = new HashSet<ITypeDefinition>();
        TinyBCT.Translators.TypeDefinitionTranslator.normalizedTypeStrings = new HashSet<string>();

        TinyBCT.Translators.StaticInitializer.mainMethods = new HashSet<IMethodDefinition>();
        TinyBCT.Translators.StaticInitializer.staticConstructors = new HashSet<IMethodDefinition>();

        TinyBCT.ImmutableArguments.MethodToMapping = new Dictionary<MethodBody, IDictionary<IVariable, IVariable>>();
    }
    protected string pathSourcesDir = System.IO.Path.Combine(Test.TestUtils.rootTinyBCT, "Test", "RegressionsAv");
    private static string pathTempDir = System.IO.Path.Combine(Test.TestUtils.rootTinyBCT, "Test", "TempDirForTests");

    protected virtual CorralResult CorralTestHelperCode(string testName, string mainMethod, int recursionBound, string source, bool useStubs = false, TinyBCT.ProgramOptions options = null, bool useCSC = false)
    {
        var testBpl = System.IO.Path.ChangeExtension(testName, ".bpl");
        if (!System.IO.Directory.Exists(pathTempDir))
        {
            System.IO.Directory.CreateDirectory(pathTempDir);
        }
        var uniqueDir = DoTest(source, testName, useStubs: useStubs, prefixDir: pathTempDir, useCSC: useCSC, options:options);
        Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(uniqueDir, testBpl)));
        var corralResult = Test.TestUtils.CallCorral(10, System.IO.Path.Combine(uniqueDir, testBpl), additionalArguments: "/main:" + mainMethod);
        Console.WriteLine(corralResult.ToString());
        return corralResult;
    }
    protected virtual CorralResult CorralTestHelper(string testName, string mainMethod, int recursionBound, bool useStubs = false, TinyBCT.ProgramOptions options = null)
    {
        string source = System.IO.File.ReadAllText(System.IO.Path.Combine(pathSourcesDir, System.IO.Path.ChangeExtension(testName, ".cs")));
        return CorralTestHelperCode(testName, mainMethod, recursionBound, source, useStubs: useStubs, options:options);
    }

    protected static string DoTest(string source, string assemblyName, bool useStubs = false, string prefixDir = "", bool useCSC = false, TinyBCT.ProgramOptions options = null)
    {
        System.Diagnostics.Contracts.Contract.Assume(
            prefixDir.Equals("") ||
            System.IO.Directory.Exists(prefixDir));
        string uniqueDir = Test.TestUtils.getFreshDir(System.IO.Path.Combine(prefixDir, assemblyName));
        System.IO.Directory.CreateDirectory(uniqueDir);
        string[] references = null;
        bool compileErrors = false;
        if (useCSC)
        {
            compileErrors = !Test.TestUtils.CompileWithCSC(source, assemblyName, prefixDir: uniqueDir);
        }
        else
        {
            compileErrors = !Test.TestUtils.CreateAssemblyDefinition(source, assemblyName, references, prefixDir: uniqueDir, useCSC: useCSC);
        }
        // Didn't work because there are conflitcs with mscorelib...
        // var references = new string[] { "CollectionStubs.dll" };

        if (!compileErrors)
        {
            var pathToSelf = System.IO.Path.GetDirectoryName(typeof(TinyBCT.Program).Assembly.Location);
            // If we need to recompile, use: csc /target:library /debug /D:DEBUG /D:CONTRACTS_FULL CollectionStubs.cs
            options = options == null ? new TinyBCT.ProgramOptions() : options;
            var dll = System.IO.Path.Combine(uniqueDir, assemblyName) + ".dll";
            var stubs = System.IO.Path.Combine(pathToSelf, "..", "..", "Dependencies", "CollectionStubs.dll");
            List<string> argsList = new List<string>();
            argsList.Add("/i:" + dll);
            var inputFiles = new List<string>(){dll};
            if (useStubs){
                inputFiles.Add(stubs);
                argsList.Add(stubs);
            }
                
            options.SetInputFiles(inputFiles);
            argsList.Add("/l:true");
            options.EmitLineNumbers = true;
            if (useStubs)
            {
                argsList.Add("/b:" + System.IO.Path.Combine(pathToSelf, "..", "..", "Dependencies", "poirot_stubs.bpl"));
                var bplFile = System.IO.Path.Combine(pathToSelf, "..", "..", "Dependencies", "poirot_stubs.bpl");
                options.SetBplFiles(new List<string>(){bplFile});

            }
            //if (additionalTinyBCTOptions != String.Empty)
            //{
            //    argsList.AddRange(additionalTinyBCTOptions.Split());
            //}
            TinyBCT.Program.Start(options);
            //var p = String.Join(" ", argsList);
            //Console.WriteLine(p);
            //TinyBCT.Program.Main(argsList.ToArray());
        }
        else
        {
            Assert.Fail();
        }
        return uniqueDir;
    }
}

public class SimpleTests : TestsBase
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
        if (Test.TestUtils.CreateAssemblyDefinition(source, "SimpleTest"))
        {
            TinyBCT.Program.Main(new string[] { "-i", "SimpleTest.dll", "-l", "false" });
        }
        else
        {
            Assert.Fail();
        }
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
        if (Test.TestUtils.CreateAssemblyDefinition(source, "GenericsTest"))
        {
            TinyBCT.Program.Main(new string[] { "-i", "GenericsTest.dll", "-l", "true" });
        }
        else
        {
            Assert.Fail();
        }
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
        DoTest(source, "TestYield");
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
        DoTest(source, "TestCollection");
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
        var corralResult = CorralTestHelperCode("Ref", "A.Main", 10, source);
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
        var corralResult = CorralTestHelperCode("BitConverter", "A.Main", 10, source);
        Assert.IsTrue(corralResult.NoBugs());
    }
}

public partial class AvRegressionTests : TestsBase
{

    [Test, Category("Av-Regressions")]
    public void TestAsSimple()
    {
        var corralResult = CorralTestHelper("AsSimple", "TestAs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions")]
    public void TestAsNotSubtype()
    {
        var corralResult = CorralTestHelper("AsNotSubtype", "TestAs.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestAsSubtypeOk()
    {
        var corralResult = CorralTestHelper("AsSubtypeOk", "TestAs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestAsSubtypeFails()
    {
        var corralResult = CorralTestHelper("AsSubtypeFails", "TestAs.Main", 10);
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
        var corralResult = CorralTestHelperCode("IsWithGenerics1", "Test.Main", 10, source);
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
        var corralResult = CorralTestHelperCode("IsWithGenerics2", "Test.Main", 10, source);
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
        var corralResult = CorralTestHelperCode("DynamicDispatchGenerics", "Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("DynamicDispatchGenerics2", "Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestAsUndeclaredType1", "Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestReturns", "Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestExternMethod1", "Test.toString", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestCast1", "Test.Main$System.Double", 10, source, useStubs: false, useCSC: true);
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
        var corralResult = CorralTestHelperCode("TestDynamicDispatch1", "Test.Main$Base2", 10, source, useStubs: false);
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
        var options = new TinyBCT.ProgramOptions();
        options.AvoidSubtypeCheckingForInterfaces = false;
        var corralResult = CorralTestHelperCode("TestInterfaceParameterOptionTrue", "Test.Main$Base2", 10, source, useStubs: false, options: options);
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
        var corralResult = CorralTestHelperCode("TestDynamicDispatch2", "Test.Main$Base2", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("DefaultNull1", "Test.Main", 10, source, useStubs: true);
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
        var corralResult = CorralTestHelperCode("DefaultNull2", "Test.Main", 10, source, useStubs: true);
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
        var corralResult = CorralTestHelperCode("DefaultDouble1", "Test.Main", 10, source, useStubs: true);
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
        var corralResult = CorralTestHelperCode("DefaultDouble2", "Test.Main", 10, source, useStubs: true);
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
        var corralResult = CorralTestHelperCode("DefaultInt1", "Test.Main", 10, source, useStubs: true);
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
        var corralResult = CorralTestHelperCode("DefaultInt2", "Test.Main", 10, source, useStubs: true);
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
        var corralResult = CorralTestHelperCode("TestNullPointerInstrumentation1", "Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestNullPointerInstrumentation2", "Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestNullPointerInstrumentation3", "Test.Main", 10, source, useStubs: false);
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
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.CheckNullDereferences = true;
        var corralResult = CorralTestHelperCode("TestNullPointerInstrumentation4", "Test.Main", 10, source, useStubs: false, options: options);
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
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.CheckNullDereferences = true;
        var corralResult = CorralTestHelperCode("TestNullPointerInstrumentation5", "$Main_Wrapper_Test.Main", 10, source, useStubs: false, options: options);
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
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.CheckNullDereferences = true;
        var corralResult = CorralTestHelperCode("TestNullPointerInstrumentation6", "$Main_Wrapper_Test.Main", 10, source, useStubs: false, options: options);
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
        var corralResult = CorralTestHelperCode("TestNullPointerInstrumentation7", "$Main_Wrapper_Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestNullPointerInstrumentation8", "$Main_Wrapper_Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestNullPointerInstrumentation9", "$Main_Wrapper_Test.Main", 10, source, useStubs: false);
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
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.CheckNullDereferences = true;
        var corralResult = CorralTestHelperCode("TestNullPointerInstrumentation7", "$Main_Wrapper_Test.Main", 10, source, useStubs: false, options: options);
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
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.CheckNullDereferences = true;
        var corralResult = CorralTestHelperCode("TestNullPointerInstrumentation8", "$Main_Wrapper_Test.Main", 10, source, useStubs: false, options: options);
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
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.CheckNullDereferences = true;
        var corralResult = CorralTestHelperCode("TestNullPointerInstrumentation9", "$Main_Wrapper_Test.Main", 10, source, useStubs: false, options: options);
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
        var corralResult = CorralTestHelperCode("TestCast2", "Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestLargeIntegers1", "Test.Main", 10, source, useStubs: false, useCSC: true);
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
        var corralResult = CorralTestHelperCode("TestLargeIntegers2", "Test.Main", 10, source, useStubs: false, useCSC: true);
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
        var corralResult = CorralTestHelperCode("TestLargeIntegers3", "Test.Main", 10, source, useStubs: false, useCSC: true);
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
        var corralResult = CorralTestHelperCode("TestStringNull1", "Test.Main", 10, source, useStubs: false, useCSC: true);
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
        var corralResult = CorralTestHelperCode("TestArray1", "Test.Main", 10, source, useStubs: false, useCSC: true);
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
        var corralResult = CorralTestHelperCode("TestAssumeTypeArguments1", "Test.Main$Base", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestAxiomsGenerics1", "Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestAxiomsGenerics2", "Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestArrays1", "Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestBidimensionalArrays1", "Test.Main", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("TestConstructors1", "Test.Main", 10, source, useStubs: false, useCSC: true);
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
        var corralResult = CorralTestHelperCode("TestConstructors2", "Test.Main", 10, source, useStubs: false, useCSC: true);
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
        var corralResult = CorralTestHelperCode("TestSizeof1", "Test.Main", 10, source, useStubs: false, useCSC: true);
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
        var corralResult = CorralTestHelperCode("TestBase1", "Test.Main", 10, source, useStubs: false, useCSC: true);
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
        var corralResult = CorralTestHelperCode("DynamicDispatchGenerics3", "Test.Main", 10, source, useStubs: false);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("AvRegressions"), Timeout(30000)]
    public void TestForeachOK()
    {
        var corralResult = CorralTestHelper("ForEachOK", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions")]
    public void TestListSumOK()
    {
        var corralResult = CorralTestHelper("ListSum", "$Main_Wrapper_PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestListSum2Fail()
    {
        var corralResult = CorralTestHelper("ListSum2", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestForeach2Bug()
    {
        var corralResult = CorralTestHelper("ForEach2Bug", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestComplexExprBug()
    {
        var corralResult = CorralTestHelper("ComplexExpr", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestComplexExpr2()
    {
        var corralResult = CorralTestHelper("ComplexExpr", "PoirotMain.ShouldPass", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestDoubleQuestion1()
    {
        var corralResult = CorralTestHelper("DoubleQuestion", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestDoubleQuestion2()
    {
        var corralResult = CorralTestHelper("DoubleQuestion", "PoirotMain.ShouldPass", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestDoubleQuestionBug()
    {
        var corralResult = CorralTestHelper("DoubleQuestion", "PoirotMain.ShouldFail$IntContainer", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestEx1()
    {
        var corralResult = CorralTestHelper("ex1", "cMain.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions")]
    public void TestEx2()
    {
        var corralResult = CorralTestHelper("ex2", "cMain.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestAbstractClassDLL()
    {
        var corralResult = CorralTestHelper("AbstractClassDLL", "Test.doStuff$Int$System.Int32", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestArgsBug1()
    {
        var corralResult = CorralTestHelper("Args", "Test.Main$System.Stringarray", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestArgsBug2()
    {
        var corralResult = CorralTestHelper("Args", "Test.Main2$System.Stringarray", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator1()
    {
        var corralResult = CorralTestHelper("stringEq", "Test.ShouldFail", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator2()
    {
        var corralResult = CorralTestHelper("stringEq", "Test.ShouldPass", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator3()
    {
        var corralResult = CorralTestHelper("stringEq", "Test.ShouldPass2", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator4()
    {
        var corralResult = CorralTestHelper("stringEq", "Test.IneqShouldFail", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator5()
    {
        var corralResult = CorralTestHelper("stringEq", "Test.IneqShouldPass", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqOperator6()
    {
        var corralResult = CorralTestHelper("stringEq", "Test.IneqShouldPass2", 10);
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
        var corralResult = CorralTestHelperCode("stringEqSpaces", "Test.Main", 10, source);
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
        var corralResult = CorralTestHelperCode("stringEqPrecision", "Test.Main", 10, source);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqWithEquals1()
    {
        var corralResult = CorralTestHelper("stringEqWithEquals", "Test.ShouldFail", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqWithEquals2()
    {
        var corralResult = CorralTestHelper("stringEqWithEquals", "Test.ShouldPass", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestStringEqWithEquals3()
    {
        var corralResult = CorralTestHelper("stringEqWithEquals", "Test.ShouldPass2", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSet1()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldPass1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSet2()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldPass2", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(30000)]
    public void TestSet3()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldPass3", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(20000)]
    public void TestSet4()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldPass4", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSet5()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldPass5", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(30000)]
    public void TestSet6()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldPass6", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSetBug1()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldFail1", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSetBug2()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldFail2", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSetBug3()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldFail3", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSetBug4()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldFail4", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(10000)]
    public void TestSetBug5()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldFail5", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Av-Regressions"), Timeout(30000)]
    public void TestSetBug6()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
}

public class TestsXor : TestsBase
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
        var corralResult = CorralTestHelperCode("XorImprecision", "Test.Main", 10, source);
        Assert.IsTrue(corralResult.AssertionFails());
    }
}
public class TestsManu : TestsBase
{
    [Ignore(""), Test, Timeout(10000), Category("NotImplemented")]
    public void Subtype()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test7$DynamicDispatch.Animal", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("NotImplemented")]
    public void Loops()
    {
        var corralResult = CorralTestHelper("Loops", "Test.Loops.Acum$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void SyntaxTest1()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("AddressesSimple", "Test.AddressesSimple.SyntaxTest1", 10, useStubs: false, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void SyntaxTest4()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("AddressesSimple", "Test.AddressesSimple.SyntaxTest4", 10, useStubs: false, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void SyntaxTest3()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("AddressesSimple", "Test.AddressesSimple.SyntaxTest3$Test.AddressesSimple", 10, useStubs: false, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void Test1()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("AddressesSimple", "Test.AddressesSimple.Test1", 10, useStubs: false, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void Test2()
    {
        var corralResult = CorralTestHelper("AddressesSimple", "Test.AddressesSimple.Test2$Test.AddressesSimple", 10, useStubs: false /*, additionalTinyBCTOptions: "/NewAddrModelling=true"*/);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void Test3()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("AddressesSimple", "Test.AddressesSimple.Test3$Test.AddressesSimple", 10, useStubs: false, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Addresses")]
    public void Test4()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("AddressesSimple", "Test.AddressesSimple.Test4$Test.AddressesSimple", 10, useStubs: false, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void Test5()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("AddressesSimple", "Test.AddressesSimple.Test5$Test.AddressesSimple", 10, useStubs: false, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Addresses")]
    public void Test6()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("AddressesSimple", "Test.AddressesSimple.Test6$Test.AddressesSimple", 10, useStubs: false, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses")]
    public void Test7()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("AddressesSimple", "Test.AddressesSimple.Test7$Test.AddressesSimple", 10, useStubs: false, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void ModOperator1()
    {
        var corralResult = CorralTestHelper("BinaryOperators", "Test.BinaryOperators.ModTest1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu")]
    public void ModOperator2()
    {
        var corralResult = CorralTestHelper("BinaryOperators", "Test.BinaryOperators.ModTest2", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Ignore(""), Test, Category("NotImplemented")]
    public void ModOperator3()
    {
        var corralResult = CorralTestHelper("BinaryOperators", "Test.BinaryOperators.ModTest3", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void SplitFields1()
    {
        var corralResult = CorralTestHelper("SplitFields", "Test.SplitFields.test1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses"), Category("RefKeyword")]
    public void RefKeyword1()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("RefKeyword", @"Test.RefKeyword.Main", 10, useStubs: false, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Addresses"), Category("RefKeyword")]
    public void RefKeyword2()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("RefKeyword", @"Test.RefKeyword.TestField$Test.RefKeyword", 10, useStubs: false, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }


    // ************************************* Boxing ******************************

    [Test, Category("Manu")]
    public void Boxing1()
    {
        var corralResult = CorralTestHelper("Boxing", @"Test.Boxing.Test1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    // MOVE ONEC EDGARD FIXED ISSUE https://github.com/edgardozoppi/analysis-net/issues/7
    [Ignore(""), Test, Category("Repro")]
    public void Boxing2()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Boxing", @"Test.Boxing.Test2", 10, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void Boxing3()
    {
        var corralResult = CorralTestHelper("Boxing", @"Test.Boxing.Test3", 10);
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
        var corralResult = CorralTestHelperCode("Boxing4", "Test.Main", 10, source, useStubs: true);
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* Array Atomic Initilization ******************************

    [Test, Category("Manu")]
    public void ArrayAtomicInit1_NoBugs()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.ArrayAtomicInit1_NoBugs", 10, options:options);
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
            var corralResult = CorralTestHelperCode("ArrayAtomicInitThrowsException1", "Test.Main", 10, source, useStubs: false);
            Assert.Fail();
        };

        Assert.Throws<NotImplementedException>(t);


    }

    [Test, Category("Arrays")]
    public void ArrayAtomicInit1_Bugged()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.ArrayAtomicInit1_Bugged", 10, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Arrays"), Category("Addresses")]
    public void ArrayAtomicInit1_Bugged_Addresses()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.ArrayAtomicInit1_Bugged", 10, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Arrays")]
    public void ArrayAtomicInit2_Bugged()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.ArrayAtomicInit2_Bugged", 10, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Arrays"), Category("Addresses")]
    public void ArrayAtomicInit2_Bugged_Addresses()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.ArrayAtomicInit2_Bugged", 10, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void ArrayAtomicInit3_Bugged()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.ArrayAtomicInit3_Bugged", 10, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void ArrayAtomicInit4_NoBugs()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.ArrayAtomicInit4_NoBugs", 10, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* Switch ******************************

    [Test, Category("Manu")]
    public void Switch1_NoBugs()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test1_NoBugs$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void Switch1_Bugged()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test1_Bugged$System.Int32", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void Switch2_NoBugs()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test2_NoBugs$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void Switch2_Bugged()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test2_Bugged$System.Int32", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void Switch3_NoBugs()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test3_NoBugs$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu")]
    public void Switch3_Bugged()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test3_Bugged$System.Int32", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu")]
    public void Switch4_NoBugs()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test4_NoBugs$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void Switch4_Bugged()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test4_Bugged$System.Int32", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    // ************************************* MissingConstructorInitializations ******************************

    [Test, Category("Manu")]
    public void MissingConstructorInitializations1()
    {
        var corralResult = CorralTestHelper("MissingConstructorInitializations", @"Test.MissingConstructorInitializations.testInt", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void MissingConstructorInitializations2()
    {
        var corralResult = CorralTestHelper("MissingConstructorInitializations", @"Test.MissingConstructorInitializations.testFloat", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void MissingConstructorInitializations3()
    {
        var corralResult = CorralTestHelper("MissingConstructorInitializations", @"Test.MissingConstructorInitializations.testDouble", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void MissingConstructorInitializations4()
    {
        var corralResult = CorralTestHelper("MissingConstructorInitializations", @"Test.MissingConstructorInitializations.testRef", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void MissingConstructorInitializations5()
    {
        var corralResult = CorralTestHelper("MissingConstructorInitializations", @"$Main_Wrapper_Test.MissingConstructorInitializations.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* DYNAMIC DISPATCH ******************************

    [Test, Category("Manu")]
    public void DynamicDispatch1_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test1_NoBugs", 10, useStubs: false);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu")]
    public void DynamicDispatch1_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test1_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch2_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test2_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch2_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test2_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch3_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test3_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch3_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test3_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch4_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test4_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch4_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test4_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch5_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test5_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch5_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test5_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch6_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test8_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch6_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test8_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch7_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test9_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DynamicDispatch7_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test9_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    // ************************************* DELEGATES ******************************


    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesGenerics1()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.generics1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesGenerics2()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.generics2", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesGenerics3()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.generics3", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesGenerics4()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.generics4", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void DelegateEmptyGroup()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.EmtpyGroup$Test.Delegates.DelegateEmpty", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates1_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates1_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates2_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates2_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates3_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates3_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates4_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates4_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates5_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates5_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates6_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates6_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates1_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates1_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates2_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates2_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates3_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates3_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates4_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates4_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates5_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates5_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Manu"), Timeout(10000)]
    public void Delegates6_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates6_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch1_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch1_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch2_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch2_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch3_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch3_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch4_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch4_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch1_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch1_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch2_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch2_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch3_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch3_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(20000)]
    public void DelegatesDynamicDispatch4_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch4_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void DelegatesDynamicDispatch5_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch5_Bugged$Test.Delegates.Dog", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    // ************************************* ARRAYS ******************************

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayStoreLoad1()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayStoreLoad1", 10, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayStoreLoad2()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayStoreLoad2", 10, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayStoreLoad3()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayStoreLoad3", 10, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayStoreLoad4()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayStoreLoad4", 10, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    // to be implemented
    /*[TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayStoreLoad3()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayStoreLoad3", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }*/

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayCreate1()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayCreate1", 10, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayCreate2()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayCreate2", 10, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayOfArrays1()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayOfArrays1", 10, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayOfArrays2()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayOfArrays2", 10, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayOfArrays3()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayOfArrays3", 10, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayOfArrays4()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayOfArrays2", 10, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayLength()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayLength$System.Int32array", 10, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ArrayLengthIteration()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayFor", 10, options:options);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Arrays"), Timeout(10000)]
    public void ArgsLength()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        var corralResult = CorralTestHelper("Arrays", "Test.Arrays.ArgsLength$System.Stringarray", 10, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [Test, Category("Arrays"), Category("Addresses")]
    public void ArgsLengthAddresses()
    {
        TinyBCT.ProgramOptions options = new TinyBCT.ProgramOptions();
        options.AtomicInitArray = true;
        options.NewAddrModelling = true;
        var corralResult = CorralTestHelper("Arrays", "Test.Arrays.ArgsLength$System.Stringarray", 10, useStubs: false, options:options);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    // ************************************* Immutable Arguments ******************************

    [Test, Category("Manu"), Timeout(10000)]
    public void ImmutableArgumentTest1()
    {
        var corralResult = CorralTestHelper("ImmutableArgument", "Test.ImmutableArgument.test1$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ImmutableArgumentTest2()
    {
        var corralResult = CorralTestHelper("ImmutableArgument", "Test.ImmutableArgument.test2$System.Int32", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    // ************************************* Split Fields ******************************

    [Test, Category("Manu"), Timeout(10000)]
    public void SplitFields2()
    {
        var corralResult = CorralTestHelper("SplitFields", "Test.SplitFields.test2$Test.SplitFields.Foo", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void SplitFields3()
    {
        var corralResult = CorralTestHelper("SplitFields", "Test.SplitFields.test3", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void SplitFields4()
    {
        var corralResult = CorralTestHelper("SplitFields", "Test.SplitFields.test4", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void SplitFields5()
    {
        var corralResult = CorralTestHelper("SplitFields", "Test.SplitFields.test5", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void SplitFields6()
    {
        var corralResult = CorralTestHelper("SplitFields", "Test.SplitFields.test6", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* Unary Operations ******************************

    [Test, Category("Manu"), Timeout(10000)]
    public void UnaryOperations1()
    {
        var corralResult = CorralTestHelper("UnaryOperations", "Test.UnaryOperations.test1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void UnaryOperations2()
    {
        var corralResult = CorralTestHelper("UnaryOperations", "Test.UnaryOperations.test2$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void UnaryOperations3()
    {
        var corralResult = CorralTestHelper("UnaryOperations", "Test.UnaryOperations.test3", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void UnaryOperations4()
    {
        var corralResult = CorralTestHelper("UnaryOperations", "Test.UnaryOperations.test1_float", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void UnaryOperations5()
    {
        var corralResult = CorralTestHelper("UnaryOperations", "Test.UnaryOperations.test2_float$System.Single", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void UnaryOperations6()
    {
        var corralResult = CorralTestHelper("UnaryOperations", "Test.UnaryOperations.test3_float", 10);
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
        var corralResult = CorralTestHelperCode("Async1", "Async.Async1", 10, source, useStubs: false);
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
        var corralResult = CorralTestHelperCode("Await1", "Async.Await1", 10, source, useStubs: false);
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* Exceptions ******************************

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest1NoBugs()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest1NoBugs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest1Bugged()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest1Bugged.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest2NoBugs()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest2NoBugs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest2Bugged()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest2Bugged.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest3NoBugs()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest3NoBugs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest3Bugged()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest3Bugged.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest5NoBugs()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest5NoBugs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest5Bugged()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest5Bugged.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(30000)]
    public void ExceptionTest6NoBugs()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest6NoBugs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest6Bugged()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest6Bugged.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest7NoBugs()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest7NoBugs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTest7Bugged()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest7Bugged.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTestInitializeExceptionVariable()
    {
        var corralResult = CorralTestHelper("Exceptions", "Test.ExceptionTestInitializeExceptionVariable.test", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Test, Category("Manu"), Timeout(10000)]
    public void ExceptionTestRethrow1()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTestRethrow1.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [Test, Category("Manu"), Timeout(20000)]
    public void ExceptionTestRethrow2()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTestRethrow2.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [Ignore(""), Test, Category("Repro")]
    // Remove when the type error is resolved: https://github.com/edgardozoppi/analysis-net/issues/10#issuecomment-416497029
    public void TestExceptionsWhen()
    {
        var corralResult = CorralTestHelper("TestExceptionsWhen", "TestExceptionsWhen.Main", 10);
    }

    [Test, Category("Repro")]
    public void Delegates1()
    {
        var source = @"
using System;
using System.Diagnostics.Contracts;
public class A {
    public static void Bar(int n) {
        Contract.Assert(n==5);
    }

    public static void Foo(int n) {
    }
}
class Test {
    public static void Main() {
        Action<int> new_delegate = delegate (int x) { A.Bar(x); };
        new_delegate(5);
    }
}
        ";
        var corralResult = CorralTestHelperCode("Delegates1", "$Main_Wrapper_Test.Main", 10, source, useStubs: false);
        Assert.IsTrue(corralResult.NoBugs());
    }

    protected override CorralResult CorralTestHelper(string testName, string mainMethod, int recusionBound, bool useStubs = false, TinyBCT.ProgramOptions options = null)
    {
        pathSourcesDir = System.IO.Path.Combine(Test.TestUtils.rootTinyBCT, "Test");
        return base.CorralTestHelper(testName, mainMethod, recusionBound, useStubs: useStubs, options: options);
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
