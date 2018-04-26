using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDispatch
{
    class DynamicDispatch
    {
        /*      TAC
              parameter Animal a;

              Animal $r0;
              Int32 $r1;
              Int32 oxy;
              Int32 $r2;
              Int32 local_1;
              Int32 $r3;

              L_0000:  nop;
              L_0001:  $r0 = a;
              L_0002:  $r1 = Animal::Breathe($r0);
              L_0007:  oxy = $r1;
              L_0008:  $r2 = oxy;
              L_0009:  local_1 = $r2;
              L_000A:  goto L_000C;
              L_000C:  $r3 = local_1;
              L_000D:  return $r3;
         */
        public static int test1(Animal a)
        {
            // correct order of checks 
            // fist check mammal
            // second check reptil
            // third abstract version animal
            int oxy = a.Breathe();
            return oxy;
        }


        /*      TAC
              parameter Dog d;

              Dog $r0;
              Int32 $r1;
              Int32 oxy;
              Int32 $r2;
              Int32 local_1;
              Int32 $r3;

              L_0000:  nop;
              L_0001:  $r0 = d;
              L_0002:  $r1 = Animal::Breathe($r0);
              L_0007:  oxy = $r1;
              L_0008:  $r2 = oxy;
              L_0009:  local_1 = $r2;
              L_000A:  goto L_000C;
              L_000C:  $r3 = local_1;
              L_000D:  return $r3;

         */

        public static int test2(Dog d)
        {
            // correct order of checks 
            // fist check mammal
            // second check reptil
            // third abstract version animal

            // this order could be more precise, the only possible method is the mammal implementation

            int oxy = d.Breathe();
            return oxy;
        }

        /*      TAC
              parameter Mammal m;

              Mammal $r0;
              Int32 $r1;
              Int32 hair;
              Int32 $r2;
              Int32 local_1;
              Int32 $r3;

              L_0000:  nop;
              L_0001:  $r0 = m;
              L_0002:  $r1 = Mammal::GrowHair($r0);
              L_0007:  hair = $r1;
              L_0008:  $r2 = hair;
              L_0009:  local_1 = $r2;
              L_000A:  goto L_000C;
              L_000C:  $r3 = local_1;
              L_000D:  return $r3;
         */

        public static int test3(Mammal m)
        {
            // mammal
            // dog
            int hair = m.GrowHair();

            return hair;
        }

        /*
              parameter Mammal m;

              Mammal $r0;
              Int32 $r1;
              Int32 hair;
              Int32 $r2;
              Int32 local_1;
              Int32 $r3;

              L_0000:  nop;
              L_0001:  $r0 = m;
              L_0002:  $r1 = Animal::Breathe($r0);
              L_0007:  hair = $r1;
              L_0008:  $r2 = hair;
              L_0009:  local_1 = $r2;
              L_000A:  goto L_000C;
              L_000C:  $r3 = local_1;
              L_000D:  return $r3;

        */
        public static int test4(Mammal m)
        {
            // mammal
            int b = m.Breathe();

            return b;
        }

        public static void test5(ClassA a)
        {
            // order must be:
            // C
            // B
            // A
            a.X();
        }
    }

    abstract class Animal
    {
        public Animal()
        {
            oxygen = 1;
        }

        public int oxygen;

        public abstract int Breathe();
    }

    class Mammal : Animal
    {
        public Mammal()
        {
            hair = 1;
        }

        public override int Breathe()
        {
            oxygen = oxygen * 2;
            return oxygen;
        }

        public int hair;

        public virtual int GrowHair()
        {
            hair = hair + 1;
            return hair;
        }
    }

    class Dog : Mammal
    {
        public override int GrowHair()
        {
            hair = hair * hair;
            return hair;
        }
    }

    class Repitile : Animal
    {
        public override int Breathe()
        {
            oxygen = oxygen + 1;
            return oxygen;
        }
    }


    class ClassA
    {
        public virtual void X()
        {
            Console.WriteLine("A");
        }
    }

    class ClassB : ClassA
    {
        public override void X()
        {
            Console.WriteLine("B");
        }
    }

    class ClassC : ClassB
    {
        public override void X() => Console.WriteLine("C");
    }

}
