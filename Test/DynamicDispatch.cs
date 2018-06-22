using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
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
        public static int test1_NoBugs()
        {
            // correct order of checks 
            // fist check mammal
            // second check reptil
            // third abstract version animal

            Animal a = new Mammal();

            int old_oxy = a.oxygen;
            int oxy = a.Breathe();

            if (a is Mammal)
                Contract.Assert(oxy == old_oxy * 2);
            else if (a is Reptile)
                Contract.Assert(oxy == old_oxy + 1);
            else
                Contract.Assert(false);

            a = new Dog();

            old_oxy = a.oxygen;
            oxy = a.Breathe();

            if (a is Mammal)
                Contract.Assert(oxy == old_oxy * 2);
            else if (a is Reptile)
                Contract.Assert(oxy == old_oxy + 1);
            else
                Contract.Assert(false);

            a = new Reptile();
            old_oxy = a.oxygen;
            oxy = a.Breathe();

            if (a is Mammal)
                Contract.Assert(oxy == old_oxy * 2);
            else if (a is Reptile)
                Contract.Assert(oxy == old_oxy + 1);
            else
                Contract.Assert(false);

            return oxy;
        }

        public static int test1_Bugged()
        {
            // correct order of checks 
            // fist check mammal
            // second check reptil
            // third abstract version animal

            Animal a = new Mammal();

            int old_oxy = a.oxygen;
            int oxy = a.Breathe();

            if (a is Mammal)
                Contract.Assert(oxy == old_oxy * 2);
            else if (a is Reptile)
                Contract.Assert(oxy == old_oxy + 1);
            else
                Contract.Assert(false);

            a = new Dog();

            old_oxy = a.oxygen;
            oxy = a.Breathe();

            if (a is Mammal)
                Contract.Assert(oxy == old_oxy * 2);
            else if (a is Reptile)
                Contract.Assert(oxy == old_oxy + 1);
            else
                Contract.Assert(false);

            a = new Reptile();
            old_oxy = a.oxygen;
            oxy = a.Breathe();

            if (a is Mammal)
                Contract.Assert(oxy == old_oxy * 2);
            else if (a is Reptile)
                Contract.Assert(oxy == old_oxy + 1);
            else
                Contract.Assert(false);

            Contract.Assert(false);

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

        public static int test2_NoBugs()
        {
            // correct order of checks 
            // fist check mammal
            // second check reptil
            // third abstract version animal

            // this order could be more precise, the only possible method is the mammal implementation
            Dog d = new Dog();
            int old_oxy = d.oxygen;
            int oxy = d.Breathe();

            Contract.Assert(old_oxy * 2 == oxy);

            return oxy;
        }

        public static int test2_Bugged()
        {
            // correct order of checks 
            // fist check mammal
            // second check reptil
            // third abstract version animal

            // this order could be more precise, the only possible method is the mammal implementation
            Dog d = new Dog();
            int old_oxy = d.oxygen;
            int oxy = d.Breathe();

            Contract.Assert(old_oxy * 2 == oxy);
            Contract.Assert(false);

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

        public static int test3_NoBugs()
        {
            // mammal
            // dog
            Mammal m = new Mammal();


            int old_hair = m.hair;
            int hair = m.GrowHair();

            if (m is Dog)
                Contract.Assert(old_hair * old_hair == hair);
            else if (m is Mammal)
                Contract.Assert(old_hair + 1 == hair);
            else
                Contract.Assert(false);

             m = new Dog();

            old_hair = m.hair;
            hair = m.GrowHair();

            if (m is Dog)
                Contract.Assert(old_hair * old_hair == hair);
            else if (m is Mammal)
                Contract.Assert(old_hair + 1 == hair);
            else
                Contract.Assert(false);

            return hair;
        }

        public static int test3_Bugged()
        {
            // mammal
            // dog
            Mammal m = new Mammal();


            int old_hair = m.hair;
            int hair = m.GrowHair();

            if (m is Dog)
                Contract.Assert(old_hair * old_hair == hair);
            else if (m is Mammal)
                Contract.Assert(old_hair + 1 == hair);
            else
                Contract.Assert(false);

            m = new Dog();

            old_hair = m.hair;
            hair = m.GrowHair();

            if (m is Dog)
                Contract.Assert(old_hair * old_hair == hair);
            else if (m is Mammal)
                Contract.Assert(old_hair + 1 == hair);
            else
                Contract.Assert(false);

            Contract.Assert(false);

            return hair;
        }

        void test4_NoBugs()
        {
            Reptile a = new Reptile();

            if (a.SampleMethod() != 1)
                Contract.Assert(false);
        }

        void test4_Bugged()
        {
            Reptile a = new Reptile();

            if (a.SampleMethod() == 1)
                Contract.Assert(false);
        }

        void test5_NoBugs()
        {
            Mammal a = new Dog();
            Contract.Assert(a.SampleMethod() == 2);
        }

        void test5_Bugged()
        {
            Mammal a = new Dog();
            Contract.Assert(a.SampleMethod() != 2);
        }

        void test6()
        {
            Mammal a = new Mammal();

            if (a.SampleMethod() != 3)
                Contract.Assert(false);
        }

        void test6(ISampleInterface i)
        {
            int r = i.SampleMethod();
        }

        static void test7(Animal a)
        {
            if (a is Dog)
            {

            } else if (a is Reptile)
            {

            } else if (a is Mammal)
            {

            }
            else
            {
                Contract.Assert(a == null);
            }
        }

        [DllImport("dummy.dll")]
        public static extern Terrier ExternTerrier();

        static void test8_NoBugs()
        {
            // since this is an extern procedure
            // there will be no alloc
            // and no type information.
            Dog d = ExternTerrier();
            d.hair = 3;

            Dog d_alias = d;

            var newHair = d_alias.GrowHair();

            // if we don't add extra information telling that d is a terrier 
            // dynamic dispatch will fail
            Contract.Assert(newHair == 3 * 3 * 3);
        }

        static void test8_Bugged()
        {
            // since this is an extern procedure
            // there will be no alloc
            // and no type information.
            Dog d = ExternTerrier();
            d.hair = 3;

            Dog d_alias = d;

            var newHair = d_alias.GrowHair();

            // if we don't add extra information telling that d is a terrier 
            // dynamic dispatch will fail
            Contract.Assert(newHair == 3 * 3);
        }

        static void test9_Bugged()
        {
            // since this is an extern procedure
            // there will be no alloc
            // and no type information.
            Func<Terrier> t = ExternTerrier;
            Dog d = ExternTerrier();
            d.hair = 3;

            Dog d_alias = d;

            var newHair = d_alias.GrowHair();

            // if we don't add extra information telling that d is a terrier 
            // dynamic dispatch will fail
            Contract.Assert(newHair == 3 * 3);
        }

        static void test9_NoBugs()
        {
            // since this is an extern procedure
            // there will be no alloc
            // and no type information.
            Func<Terrier> t = ExternTerrier;
            Dog d = ExternTerrier();
            d.hair = 3;

            Dog d_alias = d;

            var newHair = d_alias.GrowHair();

            // if we don't add extra information telling that d is a terrier 
            // dynamic dispatch will fail
            Contract.Assert(newHair == 3 * 3 * 3);
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

    class Mammal : Animal, ISampleInterface
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

        public int SampleMethod()
        {
            return 3;
        }
    }

    class Dog : Mammal, ISampleInterface
    {
        public override int GrowHair()
        {
            hair = hair * hair;
            return hair;
        }

        public new int SampleMethod()
        {
            return 2;
        }
    }

    class Terrier : Dog
    {
        public override int GrowHair()
        {
            hair =  hair * hair * hair;
            return hair;
        }
    }

    class Reptile : Animal, ISampleInterface
    {
        public override int Breathe()
        {
            oxygen = oxygen + 1;
            return oxygen;
        }

        public int SampleMethod()
        {
            return 1;
        }
    }


    class ClassA
    {
        public virtual void X()
        {
            //Console.WriteLine("A");
        }
    }

    class ClassB : ClassA
    {
        public override void X()
        {
            //Console.WriteLine("B");
        }
    }

    class ClassC : ClassB, ISampleInterface
    {
        public int SampleMethod()
        {
            return 0;
        }

        public override void X()
        {

        }
    }


    interface ISampleInterface
    {
        int SampleMethod();
    }

}
