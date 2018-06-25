using System;
using System.Collections.Generic;
using Backend.ThreeAddressCode.Values;
using Microsoft.Cci;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Test.TestUtils;
using Backend;

[TestClass]
public partial class TestsHelpers
{
    private static string pathAuxDir = System.IO.Path.Combine(Test.TestUtils.rootTinyBCT, @"Test\TestUtilsAux\");
    [TestMethod, Timeout(10000)]
    [TestCategory("Call-Corral")]
    public void TestsCorralResultNoBugs()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"no_bugs.bpl"));
        Assert.IsTrue(corralResult.NoBugs());
        Assert.IsFalse(corralResult.AssertionFails());
        Assert.IsFalse(corralResult.SyntaxErrors());
    }
    [TestCategory("Call-Corral")]
    [TestMethod, Timeout(10000)]
    public void TestsCorralResultAssertionFails()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"assertion_failure.bpl"));
        Assert.IsFalse(corralResult.NoBugs());
        Assert.IsTrue(corralResult.AssertionFails());
        Assert.IsFalse(corralResult.SyntaxErrors());
    }
    [TestCategory("Call-Corral")]
    [TestMethod, Timeout(10000)]
    public void TestsCorralResultSyntaxError()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"syntax_error.bpl"));
        Assert.IsTrue(corralResult.SyntaxErrors());
    }
    [TestCategory("Call-Corral")]
    [TestMethod, Timeout(10000)]
    public void TestsCorralResultNameResolutionError()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"name_resolution_error.bpl"));
        Assert.IsTrue(corralResult.NameResolutionErrors());
    }
    [TestCategory("Call-Corral")]
    [TestMethod, Timeout(10000)]
    [ExpectedException(typeof(Test.TestUtils.CorralResult.CorralOutputException))]
    public void TestsCorralResultNoBugsSyntaxErrorCausesException()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"syntax_error.bpl"));
        corralResult.NoBugs();
    }
    [TestCategory("Call-Corral")]
    [TestMethod, Timeout(10000)]
    [ExpectedException(typeof(Test.TestUtils.CorralResult.CorralOutputException))]
    public void TestsCorralResultAssertionFailsSyntaxErrorCausesException()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"syntax_error.bpl"));
        corralResult.AssertionFails();
    }

    [TestCategory("Call-Corral")]
    [TestMethod, Timeout(10000)]
    [ExpectedException(typeof(Test.TestUtils.CorralResult.CorralOutputException))]
    public void TestsCorralResultGetOutputSyntaxErrorCausesException()
    {
        var corralResult = Test.TestUtils.CallCorral(1, System.IO.Path.Combine(pathAuxDir, @"syntax_error.bpl"));
        corralResult.getOutput();
    }


    [TestCategory("Call-CSC")]
    [TestMethod]
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
}

[TestClass]
public partial class Tests
{
    [TestMethod, Timeout(10000)]
    [TestCategory("Call-Corral")]
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
    [TestCategory("Av-Regressions")]
    [AssemblyInitialize()]
    public static void ClassInit(TestContext context)
    {
        Console.WriteLine(context.TestName);
        // Create temporary directory
        System.IO.Directory.CreateDirectory(pathTempDir);
        Test.TestUtils.DeleteAllFiles(pathTempDir);
        foreach (var dir in System.IO.Directory.EnumerateDirectories(pathTempDir))
        {
            Test.TestUtils.DeleteAllFiles(dir);
            System.IO.Directory.Delete(dir);
        }
    }
    [TestInitialize]
    public void TestInitialize()
    {
        TinyBCT.Helpers.methodsTranslated = new System.Collections.Generic.HashSet<string>();
        TinyBCT.Helpers.Strings.stringLiterals = new System.Collections.Generic.HashSet<string>();
        TinyBCT.Helpers.Strings.specialCharacters = new Dictionary<Char, int>() { { ' ', 0 } };

        TinyBCT.Translators.InstructionTranslator.ExternMethodsCalled = new System.Collections.Generic.HashSet<Microsoft.Cci.IMethodReference>();
        TinyBCT.Translators.InstructionTranslator.PotentiallyMissingMethodsCalled = new System.Collections.Generic.HashSet<Microsoft.Cci.IMethodReference>();
        TinyBCT.Translators.InstructionTranslator.MentionedClasses = new HashSet<ITypeReference>();
        TinyBCT.Translators.FieldTranslator.fieldNames = new Dictionary<IFieldReference, String>();

        TinyBCT.Translators.DelegateStore.methodIdentifiers = new Dictionary<IMethodReference, string>();
        TinyBCT.Translators.DelegateStore.MethodGrouping = new Dictionary<string, ISet<IMethodReference>>();

        TinyBCT.Translators.TypeDefinitionTranslator.classes = new HashSet<ITypeDefinition>();
        TinyBCT.Translators.TypeDefinitionTranslator.parents = new HashSet<ITypeDefinition>();
        TinyBCT.Translators.TypeDefinitionTranslator.normalizedTypeStrings = new HashSet<string>();

        TinyBCT.Translators.StaticInitializer.mainMethods = new HashSet<IMethodDefinition>();
        TinyBCT.Translators.StaticInitializer.staticConstructors = new HashSet<IMethodDefinition>();

        TinyBCT.ImmutableArguments.MethodToMapping = new Dictionary<MethodBody, IDictionary<IVariable, IVariable>>();
    }
    protected string pathSourcesDir = System.IO.Path.Combine(Test.TestUtils.rootTinyBCT, @"Test\RegressionsAv\");
    private static string pathTempDir = System.IO.Path.Combine(Test.TestUtils.rootTinyBCT, @"Test\TempDirForTests");

    protected virtual CorralResult CorralTestHelperCode(string testName, string mainMethod, int recursionBound, string source, bool useStubs = true, string additionalOptions = "", bool useCSC = false) {
        var testBpl = System.IO.Path.ChangeExtension(testName, ".bpl");
        var uniqueDir = DoTest(source, testName, useStubs, prefixDir: pathTempDir, useCSC: useCSC);
        Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(uniqueDir, testBpl)));
        var corralResult = Test.TestUtils.CallCorral(10, System.IO.Path.Combine(uniqueDir, testBpl), additionalArguments: "/main:" + mainMethod);
        Console.WriteLine(corralResult.ToString());
        return corralResult;
    }
    protected virtual CorralResult CorralTestHelper(string testName, string mainMethod, int recursionBound, string additionalOptions = "")
    {
        string source = System.IO.File.ReadAllText(System.IO.Path.Combine(pathSourcesDir, System.IO.Path.ChangeExtension(testName, ".cs")));
        return CorralTestHelperCode(testName, mainMethod, recursionBound, source, additionalOptions: additionalOptions);
    }

    protected static string DoTest(string source, string assemblyName, bool useStubs = true, string prefixDir = "", bool useCSC = false)
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
            compileErrors  = !Test.TestUtils.CompileWithCSC(source, assemblyName, prefixDir: uniqueDir);
        }
        else
        {
            compileErrors = !Test.TestUtils.CreateAssemblyDefinition(source, assemblyName, references, prefixDir: uniqueDir, useCSC: useCSC);
        }
        // Didn't work because there are conflitcs with mscorelib...
        // var references = new string[] { "CollectionStubs.dll" };
        
        if (!compileErrors)
        {
            // If we need to recompile, use: csc /target:library /debug /D:DEBUG /D:CONTRACTS_FULL CollectionStubs.cs
            var dll = System.IO.Path.Combine(uniqueDir, assemblyName) + ".dll";
            var stubs = @"..\..\Dependencies\CollectionStubs.dll";
            var args = useStubs ?
                (new string[] { "-i", dll, stubs, "-l", "true",
                "-b", @"..\..\Dependencies\poirot_stubs.bpl" }) :
                (new string[] { "-i", dll, "-l", "true" });
            TinyBCT.Program.Main(args);
        }
        else
        {
            Assert.Fail();
        }
        return uniqueDir;
    }
}

[TestClass]
public class SimpleTests : TestsBase
{
    [TestMethod, Timeout(10000)]
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

    [TestMethod, Timeout(10000)]
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
    [TestMethod, Timeout(10000)]
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
    [TestMethod, Timeout(10000)]
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
    [TestMethod, Timeout(10000)]
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
}

[TestClass]
public partial class AvRegressionTests : TestsBase
{

    [TestCategory("Av-Regressions")]
    [TestMethod]
    public void TestAsSimple()
    {
        var corralResult = CorralTestHelper("AsSimple", "TestAs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestCategory("Av-Regressions")]
    [TestMethod, Timeout(10000)]
    public void TestAsNotSubtype()
    {
        var corralResult = CorralTestHelper("AsNotSubtype", "TestAs.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestCategory("Av-Regressions")]
    [TestMethod, Timeout(10000)]
    public void TestAsSubtypeOk()
    {
        var corralResult = CorralTestHelper("AsSubtypeOk", "TestAs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestCategory("Av-Regressions")]
    [TestMethod, Timeout(10000)]
    public void TestAsSubtypeFails()
    {
        var corralResult = CorralTestHelper("AsSubtypeFails", "TestAs.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestCategory("Av-Regressions")]
    [TestMethod]
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
    [TestCategory("Generics")]
    [TestMethod]
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
    [TestCategory("Generics")]
    [TestMethod]
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
    [TestCategory("Generics")]
    [TestMethod]
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
    [TestCategory("MissingTypes")]
    [TestMethod]
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
    [TestCategory("Repro")]
    [TestMethod]
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
    [TestCategory("Repro")]
    [TestMethod]
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

    [TestCategory("Repro")]
    [TestMethod]
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

    [TestCategory("DocumentedImprecision")]
    [TestMethod]
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
    [TestCategory("Repro")]
    [TestMethod]
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
    [TestCategory("Repro")]
    [TestMethod]
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
    [TestCategory("Repro")]
    [TestMethod]
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
    [TestCategory("Fernan")]
    [TestMethod]
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
    [TestCategory("Fernan")]
    [TestMethod]
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
    [TestCategory("Fernan")]
    [TestMethod]
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
    [TestCategory("Fernan")]
    [TestMethod]
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

    [TestCategory("AvRegressions")]
    [TestMethod]
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

    [TestCategory("Av-Regressions")]
    [TestMethod, Timeout(10000)]
    public void TestForeachOK()
    {
        var corralResult = CorralTestHelper("ForEachOK", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod]
    [TestCategory("Av-Regressions")]
    public void TestListSumOK()
    {
        var corralResult = CorralTestHelper("ListSum", "$Main_Wrapper_PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestListSum2Fail()
    {
        var corralResult = CorralTestHelper("ListSum2", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestForeach2Bug()
    {
        var corralResult = CorralTestHelper("ForEach2Bug", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestComplexExprBug()
    {
        var corralResult = CorralTestHelper("ComplexExpr", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestComplexExpr2()
    {
        var corralResult = CorralTestHelper("ComplexExpr", "PoirotMain.ShouldPass", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestDoubleQuestion1()
    {
        var corralResult = CorralTestHelper("DoubleQuestion", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestDoubleQuestion2()
    {
        var corralResult = CorralTestHelper("DoubleQuestion", "PoirotMain.ShouldPass", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestDoubleQuestionBug()
    {
        var corralResult = CorralTestHelper("DoubleQuestion", "PoirotMain.ShouldFail$IntContainer", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestEx1()
    {
        var corralResult = CorralTestHelper("ex1", "cMain.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestEx2()
    {
        var corralResult = CorralTestHelper("ex2", "cMain.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestAbstractClassDLL()
    {
        var corralResult = CorralTestHelper("AbstractClassDLL", "Test.doStuff$Int$System.Int32", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestArgsBug1()
    {
        var corralResult = CorralTestHelper("Args", "Test.Main$System.Stringarray", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestArgsBug2()
    {
        var corralResult = CorralTestHelper("Args", "Test.Main2$System.Stringarray", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestStringEqOperator1()
    {
        var corralResult = CorralTestHelper("stringEq", "Test.ShouldFail", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestStringEqOperator2()
    {
        var corralResult = CorralTestHelper("stringEq", "Test.ShouldPass", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestStringEqOperator3()
    {
        var corralResult = CorralTestHelper("stringEq", "Test.ShouldPass2", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestStringEqOperator4()
    {
        var corralResult = CorralTestHelper("stringEq", "Test.IneqShouldFail", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestStringEqOperator5()
    {
        var corralResult = CorralTestHelper("stringEq", "Test.IneqShouldPass", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestStringEqOperator6()
    {
        var corralResult = CorralTestHelper("stringEq", "Test.IneqShouldPass2", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
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
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
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

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestStringEqWithEquals1()
    {
        var corralResult = CorralTestHelper("stringEqWithEquals", "Test.ShouldFail", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestStringEqWithEquals2()
    {
        var corralResult = CorralTestHelper("stringEqWithEquals", "Test.ShouldPass", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestStringEqWithEquals3()
    {
        var corralResult = CorralTestHelper("stringEqWithEquals", "Test.ShouldPass2", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestSet1()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldPass1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestSet2()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldPass2", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestSet3()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldPass3", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestSet4()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldPass4", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestSet5()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldPass5", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestSet6()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldPass6", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestSetBug1()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldFail1", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestSetBug2()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldFail2", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestSetBug3()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldFail3", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestSetBug4()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldFail4", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestSetBug5()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.ShouldFail5", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Av-Regressions")]
    public void TestSetBug6()
    {
        var corralResult = CorralTestHelper("Set", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
}

[TestClass]
public class TestsXor : TestsBase
{
    [TestMethod]
    [TestCategory("DocumentedImprecision")]
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

[TestClass]
public class TestsManu : TestsBase
{

    [TestMethod, Timeout(10000)]
    [TestCategory("NotImplemented")]
    public void Subtype()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test7$DynamicDispatch.Animal", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod]
    [TestCategory("NotImplemented")]
    public void Loops()
    {
        var corralResult = CorralTestHelper("Loops", "Test.Loops.Acum$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [TestMethod]
    [TestCategory("Manu")]
    public void SplitFields1()
    {
        var corralResult = CorralTestHelper("SplitFields", "Test.SplitFields.test1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod]
    [TestCategory("Manu")]
    public void RefKeyword1()
    {
        var corralResult = CorralTestHelper("RefKeyword", @"Test.RefKeyword.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* Switch ******************************

    [TestMethod]
    [TestCategory("Manu")]
    public void Switch1_NoBugs()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test1_NoBugs$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [TestMethod]
    [TestCategory("Manu")]
    public void Switch1_Bugged()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test1_Bugged$System.Int32", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    [TestMethod]
    [TestCategory("Manu")]
    public void Switch2_NoBugs()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test2_NoBugs$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [TestMethod]
    [TestCategory("Manu")]
    public void Switch2_Bugged()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test2_Bugged$System.Int32", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }



    [TestMethod]
    [TestCategory("Manu")]
    public void Switch3_NoBugs()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test3_NoBugs$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [TestMethod]
    [TestCategory("Manu")]
    public void Switch3_Bugged()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test3_Bugged$System.Int32", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }



    [TestMethod]
    [TestCategory("Manu")]
    public void Switch4_NoBugs()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test4_NoBugs$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [TestMethod]
    [TestCategory("Manu")]
    public void Switch4_Bugged()
    {
        var corralResult = CorralTestHelper("Switch", @"Test.Switch.test4_Bugged$System.Int32", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    // ************************************* MissingConstructorInitializations ******************************

    [TestMethod]
    [TestCategory("Manu")]
    public void MissingConstructorInitializations1()
    {
        var corralResult = CorralTestHelper("MissingConstructorInitializations", @"Test.MissingConstructorInitializations.testInt", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod]
    [TestCategory("Manu")]
    public void MissingConstructorInitializations2()
    {
        var corralResult = CorralTestHelper("MissingConstructorInitializations", @"Test.MissingConstructorInitializations.testFloat", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod]
    [TestCategory("Manu")]
    public void MissingConstructorInitializations3()
    {
        var corralResult = CorralTestHelper("MissingConstructorInitializations", @"Test.MissingConstructorInitializations.testDouble", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod]
    [TestCategory("Manu")]
    public void MissingConstructorInitializations4()
    {
        var corralResult = CorralTestHelper("MissingConstructorInitializations", @"Test.MissingConstructorInitializations.testRef", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod]
    [TestCategory("Manu")]
    public void MissingConstructorInitializations5()
    {
        var corralResult = CorralTestHelper("MissingConstructorInitializations", @"$Main_Wrapper_Test.MissingConstructorInitializations.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* DYNAMIC DISPATCH ******************************

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch1_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test1_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch1_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test1_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch2_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test2_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch2_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test2_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch3_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test3_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch3_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test3_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch4_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test4_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch4_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test4_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch5_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test5_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch5_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test5_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch6_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test8_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch6_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test8_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch7_NoBugs()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test9_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DynamicDispatch7_Bugged()
    {
        var corralResult = CorralTestHelper("DynamicDispatch", @"DynamicDispatch.DynamicDispatch.test9_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }


    // ************************************* DELEGATES ******************************

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesGenerics1()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.generics1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesGenerics2()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.generics2", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesGenerics3()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.generics3", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesGenerics4()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.generics4", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegateEmptyGroup()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.EmtpyGroup$Test.Delegates.DelegateEmpty", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates1_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates1_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates2_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates2_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates3_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates3_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates4_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates4_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates5_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates5_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates6_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates6_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates1_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates1_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates2_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates2_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates3_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates3_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates4_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates4_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates5_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates5_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates6_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.Delegates6_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesDynamicDispatch1_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch1_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesDynamicDispatch2_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch2_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesDynamicDispatch3_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch3_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesDynamicDispatch4_NoBugs()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch4_NoBugs", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesDynamicDispatch1_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch1_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesDynamicDispatch2_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch2_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesDynamicDispatch3_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch3_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesDynamicDispatch4_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch4_Bugged", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void DelegatesDynamicDispatch5_Bugged()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.DelegatesDynamicDispatch5_Bugged$Test.Delegates.Dog", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    // ************************************* ARRAYS ******************************

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayStoreLoad1()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayStoreLoad1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayStoreLoad2()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayStoreLoad2", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayStoreLoad3()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayStoreLoad3", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayStoreLoad4()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayStoreLoad4", 10);
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

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayCreate1()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayCreate1", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayCreate2()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayCreate2", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayOfArrays1()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayOfArrays1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayOfArrays2()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayOfArrays2", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayOfArrays3()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayOfArrays3", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayOfArrays4()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayOfArrays2", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayLength()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayLength$System.Int32array", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArrayLengthIteration()
    {
        var corralResult = CorralTestHelper("Arrays", @"Test.Arrays.arrayFor", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ArgsLength()
    {
        var corralResult = CorralTestHelper("Arrays", "Test.Arrays.ArgsLength$System.Stringarray", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    // ************************************* Immutable Arguments ******************************

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ImmutableArgumentTest1()
    {
        var corralResult = CorralTestHelper("ImmutableArgument", "Test.ImmutableArgument.test1$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ImmutableArgumentTest2()
    {
        var corralResult = CorralTestHelper("ImmutableArgument", "Test.ImmutableArgument.test2$System.Int32", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    // ************************************* Split Fields ******************************

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void SplitFields2()
    {
        var corralResult = CorralTestHelper("SplitFields", "Test.SplitFields.test2$Test.SplitFields.Foo", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void SplitFields3()
    {
        var corralResult = CorralTestHelper("SplitFields", "Test.SplitFields.test3", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod]
    [TestCategory("Manu"), Timeout(10000)]
    public void SplitFields4()
    {
        var corralResult = CorralTestHelper("SplitFields", "Test.SplitFields.test4", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void SplitFields5()
    {
        var corralResult = CorralTestHelper("SplitFields", "Test.SplitFields.test5", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void SplitFields6()
    {
        var corralResult = CorralTestHelper("SplitFields", "Test.SplitFields.test6", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* Unary Operations ******************************

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void UnaryOperations1()
    {
        var corralResult = CorralTestHelper("UnaryOperations", "Test.UnaryOperations.test1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void UnaryOperations2()
    {
        var corralResult = CorralTestHelper("UnaryOperations", "Test.UnaryOperations.test2$System.Int32", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void UnaryOperations3()
    {
        var corralResult = CorralTestHelper("UnaryOperations", "Test.UnaryOperations.test3", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void UnaryOperations4()
    {
        var corralResult = CorralTestHelper("UnaryOperations", "Test.UnaryOperations.test1_float", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void UnaryOperations5()
    {
        var corralResult = CorralTestHelper("UnaryOperations", "Test.UnaryOperations.test2_float$System.Single", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void UnaryOperations6()
    {
        var corralResult = CorralTestHelper("UnaryOperations", "Test.UnaryOperations.test3_float", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    // ************************************* Exceptions ******************************

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ExceptionTest1NoBugs()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest1NoBugs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ExceptionTest1Bugged()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest1Bugged.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ExceptionTest2NoBugs()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest2NoBugs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ExceptionTest2Bugged()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest2Bugged.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ExceptionTest3NoBugs()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest3NoBugs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ExceptionTest3Bugged()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest3Bugged.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod]
    [TestCategory("Manu"), Timeout(10000)]
    public void ExceptionTest5NoBugs()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest5NoBugs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ExceptionTest5Bugged()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest5Bugged.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod]
    [TestCategory("Manu"), Timeout(10000)]
    public void ExceptionTest6NoBugs()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest6NoBugs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ExceptionTest6Bugged()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest6Bugged.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod]
    [TestCategory("Manu"), Timeout(10000)]
    public void ExceptionTest7NoBugs()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest7NoBugs.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ExceptionTest7Bugged()
    {
        var corralResult = CorralTestHelper("Exceptions", "$Main_Wrapper_Test.ExceptionTest7Bugged.Main", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void ExceptionTestInitializeExceptionVariable()
    {
        var corralResult = CorralTestHelper("Exceptions", "Test.ExceptionTestInitializeExceptionVariable.test", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    protected override CorralResult CorralTestHelper(string testName, string mainMethod, int recusionBound, string additionalOptions = "")
    {
        pathSourcesDir = System.IO.Path.Combine(Test.TestUtils.rootTinyBCT, @"Test\");
        return base.CorralTestHelper(testName, mainMethod, recusionBound, additionalOptions);
    }
 }

[TestClass]
public class TestStringHelpers
{

    [TestMethod, Timeout(10000)]
    [TestCategory("TestsForHelpers")]
    public void TestReplaceIllegalCharsSimple()
    {
        Assert.AreEqual("Hello#1#World", TinyBCT.Helpers.Strings.ReplaceIllegalChars(@"Hello:World"));
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("TestsForHelpers")]
    public void TestReplaceSpaces1()
    {
        Assert.AreEqual("Hello#0#World", TinyBCT.Helpers.Strings.ReplaceIllegalChars(@"Hello World"));
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("TestsForHelpers")]
    public void TestReplaceSpaces2()
    {
        Assert.AreEqual("Hello#0#World", TinyBCT.Helpers.Strings.ReplaceIllegalChars(@"Hello World"));
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("TestsForHelpers")]
    public void TestReplaceIllegalCharsEscape()
    {
        Assert.AreEqual("Hello##World", TinyBCT.Helpers.Strings.ReplaceIllegalChars(@"Hello#World"));
    }
}
