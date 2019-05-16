using System;
using System.Diagnostics.Contracts;

namespace Test
{
    public class TestStructs
    {
        public void Test1_Bugged()
        {
            TestStructsClass t
                = new TestStructsClass();

            Contract.Assert(t.Get() != 0);
        }

        public void Test2_Bugged()
        {
            TestStructsClass t
                = new TestStructsClass();

            t.Set(50);
            Contract.Assert(t.Get() != 50);
        }

        public void Test3_Bugged()
        {
            TestStructsClass t
                = new TestStructsClass();

            ref StructX refStruct = ref t.structX;
            refStruct.x = 50;

            Contract.Assert(t.Get() != 50);
        }

        public void Test4_Bugged()
        {
            TestStructsClass t
                = new TestStructsClass();

            ref int refInt = ref t.structX.x;
            refInt = 50;

            Contract.Assert(t.Get() != 50);
        }

        public void Test1_NoBugs()
        {
            TestStructsClass t
                = new TestStructsClass();

            Contract.Assert(t.Get() == 0);
        }

        public void Test2_NoBugs()
        {
            TestStructsClass t
                = new TestStructsClass();

            t.Set(50);
            Contract.Assert(t.Get() == 50);
        }

        public void Test3_NoBugs()
        {
            TestStructsClass t
                = new TestStructsClass();

            ref StructX refStruct = ref t.structX;
            refStruct.x = 50;

            Contract.Assert(t.Get() == 50);
        }

        public void Test4_NoBugs()
        {
            TestStructsClass t
                = new TestStructsClass();

            ref int refInt = ref t.structX.x;
            refInt = 50;

            Contract.Assert(t.Get() == 50);
        }
    }

    public struct StructX
    {
        public int x;
    }

    public class TestStructsClass
    {
        public TestStructsClass()
        {
            // C# compiler does not values initialization if they were not explict. Otherwise, they are hanlded by the runtime.
            // For classes we workaround this but we did not for structs
            // we are adding this as a workaround
            structX.x = 0;
        }

        public StructX structX;
        public int Get() { return structX.x; }
        public void Set(int a) { structX.x = a; }
    }
}
