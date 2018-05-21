using System;
using System.Collections.Generic;
using Backend.ThreeAddressCode.Values;
using Microsoft.Cci;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Test.TestUtils;

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
    [ClassInitialize()]
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
    }
    protected string pathSourcesDir = System.IO.Path.Combine(Test.TestUtils.rootTinyBCT, @"Test\RegressionsAv\");
    private static string pathTempDir = System.IO.Path.Combine(Test.TestUtils.rootTinyBCT, @"Test\TempDirForTests");


    protected virtual CorralResult CorralTestHelper(string testName, string mainMethod, int recusionBound, string additionalOptions = "")
    {
        var testBpl = System.IO.Path.ChangeExtension(testName, ".bpl");
        string source = System.IO.File.ReadAllText(System.IO.Path.Combine(pathSourcesDir, System.IO.Path.ChangeExtension(testName, ".cs")));
        var uniqueDir = DoTest(source, testName, prefixDir: pathTempDir);
        Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(uniqueDir, testBpl)));
        var corralResult = Test.TestUtils.CallCorral(10, System.IO.Path.Combine(uniqueDir, testBpl), additionalArguments: "/main:" + mainMethod);
        return corralResult;
    }
    protected static string DoTest(string source, string assemblyName, string prefixDir = "")
    {
        System.Diagnostics.Contracts.Contract.Assume(
            prefixDir.Equals("") ||
            System.IO.Directory.Exists(prefixDir));
        string uniqueDir = System.IO.Path.Combine(prefixDir, Test.TestUtils.getFreshDir(assemblyName));
        System.IO.Directory.CreateDirectory(uniqueDir);
        string[] references = null;
        // Didn't work because there are conflitcs with mscorelib...
        // var references = new string[] { "CollectionStubs.dll" };
        if (Test.TestUtils.CreateAssemblyDefinition(source, assemblyName, references, prefixDir: uniqueDir))
        {
            TinyBCT.Program.Main(new string[] { "-i", System.IO.Path.Combine(uniqueDir, assemblyName)+".dll",
                @"..\..\Dependencies\CollectionStubs.dll",
                "-l", "true",
                "-b", @"..\..\Dependencies\poirot_stubs.bpl" });
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
}

[TestClass]
public partial class AvRegressionTests : TestsBase
{

    [TestCategory("Av-Regressions")]
    [TestMethod, Timeout(10000)]
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
    [TestMethod, Timeout(10000)]
    public void TestForeachOK()
    {
        var corralResult = CorralTestHelper("ForEachOK", "PoirotMain.Main", 10);
        Assert.IsTrue(corralResult.NoBugs());
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
    public void TestArgs()
    {
        var corralResult = CorralTestHelper("Args", "Test.Main$System.Stringarray", 10);
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
}


[TestClass]
public class TestsManu : TestsBase
{
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates1()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.creation_invoke1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates2()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.invoke_invoke1_plus", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }


    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates3()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.generics1", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }
    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates4()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.generics2", 10);
        Assert.IsTrue(corralResult.AssertionFails());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates5()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.generics3", 10);
        Assert.IsTrue(corralResult.NoBugs());
    }

    [TestMethod, Timeout(10000)]
    [TestCategory("Manu")]
    public void Delegates6()
    {
        var corralResult = CorralTestHelper("Delegates", @"Test.Delegates.generics4", 10);
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


    protected override CorralResult CorralTestHelper(string testName, string mainMethod, int recusionBound, string additionalOptions = "")
    {
        pathSourcesDir = System.IO.Path.Combine(Test.TestUtils.rootTinyBCT, @"Test\");
        return base.CorralTestHelper(testName, mainMethod, recusionBound, additionalOptions);
    }
 }
