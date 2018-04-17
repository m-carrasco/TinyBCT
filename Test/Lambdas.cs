using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Lambdas
    {
        // the TAC code found here is from Analysis-Viewer

        /*
            // TAC for lambda1

            Func<Int32, Int32> $r0;
            Func<Int32, Int32> $r1;
            <>c $r2;
            Int32(Int32) $r3;
            Func<Int32, Int32> $r4;
            Func<Int32, Int32> $r5;
            Func<Int32, Int32> l;
            int res;

            L_0000:  nop;
            L_0001:  $r0 = <>c::<>9__0_0;
            L_0006:  $r1 = $r0;
            L_0007:  if $r0 == true goto L_0020; // bug of analysis-net it should be $r0 != null
            L_000A:  $r2 = <>c::<>9;
            L_000F:  $r3 = &<>c::Int32 <>c::<lambda1>b__0_0(Int32);
            L_0015:  $r4 = new Func<Int32, Int32>;
            L_0015:  Func::.ctor($r4, $r2, $r3);
            L_0015:  $r0 = $r4;
            L_001A:  $r5 = $r0;
            L_001B:  <>c::<>9__0_0 = $r5;
            L_0020:  l = $r0;
            L_0023:  res = Func::Invoke(l, 1);
            L_0021:  return;

            // C# version by me

            public static void lambda1(){
                Func<Int32, Int32> $r0;
                Func<Int32, Int32> $r4;
                Func<Int32, Int32> l;
                int res;

                r0 = c.9__0_0;
                if (r0  != null){
                    l = r0;
                } else {
                    r4 = reference to instance method <>c::Int32 <>c::<lambda1>b__0_0(Int32) of instance c.9;
                    c.9__0_0 = r4;
                    l = r4;
                }

                // if corral just assumes it is not null
                // it could happen that it is a delegate to another function or a different instance than c.9
                res = InvokeDelegate(l, 1);
            }
         */

        public static void lambda1()
        {
            Func<int, int> l = (x => x * x);
            var r = l(1);
            Contract.Assert(r == 1);
        }

        /*
            // TAC for lambda2

            parameter Int32 y;

            <>c__DisplayClass1_0 $r0;
            <>c__DisplayClass1_0 $r1;
            <>c__DisplayClass1_0 CS$<>8__locals0;
            <>c__DisplayClass1_0 $r2;
            Int32 $r3;
            <>c__DisplayClass1_0 $r4;
            Int32(Int32) $r5;
            Func<Int32, Int32> $r6;
            Func<Int32, Int32> $r7;
            Func<Int32, Int32> l;
            Func<Int32, Int32> $r8;
            Int32 $r9;
            Int32 $r10;

            L_0000:  $r0 = new <>c__DisplayClass1_0;
            L_0000:  <>c__DisplayClass1_0::.ctor($r0);
            L_0000:  $r1 = $r0;
            L_0005:  CS$<>8__locals0 = $r1;
            L_0006:  $r2 = CS$<>8__locals0;
            L_0007:  $r3 = y;
            L_0008:  $r2.y = $r3;
            L_000D:  nop;
            L_000E:  $r4 = CS$<>8__locals0;
            L_000F:  $r5 = &<>c__DisplayClass1_0::Int32 <>c__DisplayClass1_0::<lambda2>b__0(Int32);
            L_0015:  $r6 = new Func<Int32, Int32>;
            L_0015:  Func::.ctor($r6, $r4, $r5);
            L_0015:  $r7 = $r6;
            L_001A:  l = $r7;
            L_001B:  $r8 = l;
            L_001C:  $r9 = 1;
            L_001D:  $r10 = Func::Invoke($r8, $r9);
            L_0023:  return;

            // C# version handmade version
            public static void lambda2(int y){
                <>c__DisplayClass1_0 CS$<>8__locals0;

                CS$<>8__locals0 = new <>c__DisplayClass1_0();
                CS$<>8__locals0.y = y;
                r6 = reference to instance method <>c__DisplayClass1_0::Int32 <>c__DisplayClass1_0::<lambda2>b__0(Int32) of instance CS$<>8__locals0;
                r10 = InvokeDelegate(r6, 1)
            }


         */
        public static void lambda2(int y)
        {
            Func<int, int> l = (x => y * x);
            l(1);
        }
    }
}
