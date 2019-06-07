using System;
using System.Diagnostics.Contracts;

namespace NUnitTests.Resources
{
    public class StringOperations
    {
        //function {:builtin "str.to.int"} stringToInt(string) : int;
        //function {:builtin "int.to.str"} intToString(int) : string;

        //function {:builtin "str.++"} concat(string, string) : string;
        public string PlusOperator(string a, string b)
        {
            //$r2 = System.String::Concat($r0, $r1);   //Backend.ThreeAddressCode.Instructions.MethodCallInstruction 
            return a + b;
        }

        // function {:builtin "str.len"} len(string) : int;
        public int LengthProperty(string a)
        {
            //$r1 = System.String::get_Length($r0);   //Backend.ThreeAddressCode.Instructions.MethodCallInstruction 
            return a.Length;
        }

        // function {:builtin "str.substr"} substr(string, int, int) : string;
        public string SubstringOperation(string a, int b, int e)
        {
            //$r3 = System.String::Substring($r0, $r1, $r2);   //Backend.ThreeAddressCode.Instructions.MethodCallInstruction
            return a.Substring(b, e);
        }

        // function {:builtin "str.indexof"} indexOf(string, string) : int;
        public int IndexOfOperation(string a, string b)
        {
            //$r2 = System.String::IndexOf($r0, $r1);   //Backend.ThreeAddressCode.Instructions.MethodCallInstruction 
            return a.IndexOf(b);
        }

        //function {:builtin "str.indexof"} indexOfFromOffset(string, string, int) : int;
        public int IndexOfFromOffsetOperation(string a, string b, int offset)
        {
            //$r3 = System.String::IndexOf($r0, $r1, $r2);   //Backend.ThreeAddressCode.Instructions.MethodCallInstruction 
            return a.IndexOf(b, offset);
        }

        //function {:builtin "str.at"} at(string, int) : string;
        public char AtOperation(string a, int p)
        {
            //$r2 = System.String::get_Chars($r0, $r1);   //Backend.ThreeAddressCode.Instructions.MethodCallInstruction 
            return a[p];
        }

        //function {:builtin "str.suffixof"} suffixOf(string, string) : bool;
        public bool SuffixOfOperation(string a, string b)
        {
            //$r2 = System.String::StartsWith($r0, $r1);   //Backend.ThreeAddressCode.Instructions.MethodCallInstruction 
            return a.StartsWith(b);
        }

        //function {:builtin "str.prefixof"} prefixOf(string, string) : bool;
        public bool PrefixOfOperation(string a, string b)
        {
            //$r2 = System.String::EndsWith($r0, $r1);   //Backend.ThreeAddressCode.Instructions.MethodCallInstruction 
            return a.EndsWith(b);
        }

        //function {:builtin "str.contains"} contains(string, string) : bool;
        public bool ContainsOperation(string a, string b)
        {
            //$r2 = System.String::Contains($r0, $r1);   //Backend.ThreeAddressCode.Instructions.MethodCallInstruction 
            return a.Contains(b);
        }

        //function {:builtin "str.replace"} replace(string, string, string) : string;
        public string ReplaceOperation(string a, string b, string c)
        {
            //$r3 = System.String::Replace($r0, $r1, $r2);   //Backend.ThreeAddressCode.Instructions.MethodCallInstruction
            return a.Replace(b, c);
        }

        public bool NotEqualOperator(string a, string b)
        {
            return a != b;
        }

        public bool EqualOperator(string a, string b)
        {
            return a == b;
        }
    }

    public class StringTest
    {
        public static void Test1_NoBugs(string name)
        {
            string Msg = "Hello " + name;
            Contract.Assert(Msg == "Hello " + name);
        }

        public static void Test1_Bugged(string name)
        {
            string Msg = "Hello " + name;
            Contract.Assert(Msg == "Helllloo " + name);
        }

        public static void Test2_NoBugs()
        {
            string Msg = "Please pass a name on the query string or in the request body";
            Contract.Assert(Msg == "Please pass a name on the query string or in the request body");
        }

        public static void Test2_Bugged()
        {
            string Msg = "Please pass a name on the query string or in the request body";
            Contract.Assert(Msg == "Please p a s s  a name on the query string or in the request body");
        }

        public static void Test3_NoBugs()
        {
            StringTest stringTest = new StringTest();
            stringTest.Msg = "Please pass a name on the query string or in the request body";
            Contract.Assert(stringTest.Msg == "Please pass a name on the query string or in the request body");
        }

        public static void Test3_Bugged()
        {
            StringTest stringTest = new StringTest();
            stringTest.Msg = "Please pass a name on the query string or in the request body";
            Contract.Assert(stringTest.Msg == "Please p a s s a name on the query string or in the request body");
        }

        public static void Test4_NoBugs(bool b, string name)
        {
            StringTest stringTest = new StringTest();
            if (b)
                stringTest.Msg = "Please pass a name on the query string or in the request body";
            else
                stringTest.Msg = "Hello " + name;

            Contract.Assert(stringTest.Msg == "Please pass a name on the query string or in the request body" || 
                stringTest.Msg == "Hello " + name);
        }

        public static void Test4_Bugged(bool b, string name)
        {
            StringTest stringTest = new StringTest();
            if (b)
                stringTest.Msg = "Please pass a name on the query string or in the request body";
            else
                stringTest.Msg = "Hello " + name;

            Contract.Assert(!(stringTest.Msg == "Please pass a name on the query string or in the request body" ||
                stringTest.Msg == "Hello " + name));
        }

        public static void Test5_NoBugs(string name)
        {
            StringTest stringTest = new StringTest();
            if (name == null)
                stringTest.Msg = "Please pass a name on the query string or in the request body";
            else
                stringTest.Msg = "Hello " + name;

            Contract.Assert(stringTest.Msg == "Please pass a name on the query string or in the request body" ||
                stringTest.Msg == "Hello " + name);
        }

        public static void Test5_Bugged(string name)
        {
            StringTest stringTest = new StringTest();
            if (name == null)
                stringTest.Msg = "Please pass a name on the query string or in the request body";
            else
                stringTest.Msg = "Hello " + name;

            Contract.Assert(!(stringTest.Msg == "Please pass a name on the query string or in the request body" ||
                stringTest.Msg == "Hello " + name));
        }

        public string Msg;

    }
    public class Program
    {
        static void Main(string[] args)
        {

        }
    }
}
