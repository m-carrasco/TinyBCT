type Addr;
type Object;

var $Alloc: [Addr]bool;
var $AllocObject: [Object]bool;

//const unique null_addr: Addr;
const unique null_object : Object;

type HeapInt = [Addr]int;
type HeapBool = [Addr]bool;
type HeapAddr = [Addr]Addr;
type HeapObject = [Addr]Object;

// for fields
type InstanceFieldAddr = [Object]Addr;

procedure {:inline 1} AllocAddr() returns (x: Addr);
  modifies $Alloc;

implementation {:inline 1} AllocAddr() returns (x: Addr)
{
    assume $Alloc[x] == false; //&& x != null_addr;
    $Alloc[x] := true;
}

procedure {:inline 1} AllocObject() returns (x: Object);
  modifies $AllocObject;

implementation {:inline 1} AllocObject() returns (x: Object)
{
    assume $AllocObject[x] == false && x != null_object;
    $AllocObject[x] := true;
}

var $memoryInt : HeapInt;
var $memoryAddr : HeapAddr;
var $memoryBool : HeapBool;
var $memoryObject : HeapObject;

// one heap per each field of every class
var $Heap$Animal$val1 : InstanceFieldAddr;
//var $Heap$Animal$f2 : InstanceFieldAddr;
//var $Heap$Animal$f3 : InstanceFieldAddr;

function {:inline true} LoadInstanceFieldAddr(H: InstanceFieldAddr, o: Object) : Addr
{
  H[o]
}

function {:inline true} StoreInstanceFieldAddr(H: InstanceFieldAddr, o: Object, v : Addr) : InstanceFieldAddr
{
  H[o := v]
}

function {:inline true} ReadInt(H: HeapInt, a: Addr) : int
{
  H[a]
}

function {:inline true} WriteInt(H: HeapInt, a: Addr, v : int) : HeapInt
{
  H[a := v]
}

function {:inline true} ReadAddr(H: HeapAddr, a: Addr) : Addr
{
  H[a]
}

function {:inline true} WriteAddr(H: HeapAddr, a: Addr, v : Addr) : HeapAddr
{
  H[a := v]
}

function {:inline true} ReadObject(H: HeapObject, a: Addr) : Object
{
  H[a]
}

function {:inline true} WriteObject(H: HeapObject, a: Addr, v : Object) : HeapObject
{
  H[a := v]
}


procedure AllocAnimalInstanceFields(o : Object)
{
  var _a1 : Addr;

  // val1 allocation
  call _a1 := AllocAddr();
  $memoryInt := WriteInt($memoryInt, _a1, 0);

  $Heap$Animal$val1 := StoreInstanceFieldAddr($Heap$Animal$val1, o, _a1);
}


// **********************************************************************************

//int sum(int x, int y){
//  int res = 0;
//  res = x + y;
//  Contract.Assert(x + y);
//  return res;
//}

procedure sum(x_value: int, y_value : int) returns (return_value: int)
{
  var _x : Addr;
  var _y : Addr;
  var _res : Addr;
  
  var $x_read : int;
  var $y_read : int;
  var $res_read : int;
  
  // alloc stack space and store argument values there
  call _x := AllocAddr();
  $memoryInt := WriteInt($memoryInt, _x, x_value);
  call _y := AllocAddr();
  $memoryInt := WriteInt($memoryInt, _y, y_value);
  
  // alloc stack space for local variables (not arguments)
  call _res := AllocAddr();
  $memoryInt := WriteInt($memoryInt, _res, 0);
  
  // read x and y values for sum
  $x_read := ReadInt($memoryInt, _x);
  $y_read := ReadInt($memoryInt, _y);
  
  // store sum in _res's data
  $memoryInt := WriteInt($memoryInt, _res, $x_read + $y_read);
  
  // read res and make an assert, also we need to return a value
  $res_read := ReadInt($memoryInt, _res);
  
  assert $res_read == x_value + y_value;
  
  return_value := $res_read;
}

// **********************************************************************************

/*

class Animal{
   public int val1;

   public Animal(){

   } 

   public static void Foo(Animal a){
      a.val1 = 1000;
   }
}


public static void Main_Test1(){
  Animal a = new Animal();
  Animal.Foo(a);
  Contract.Assert(a.val1 == 1000);
}
*/


procedure Main_Test1() 
{
  // allocate space for stack variables
  var _a : Addr;
  var _r1 : Object;
  var _r2 : Object;
  var _r3 : Addr;
  var _r4 : int;

  // allocating
  call _a := AllocAddr();

  call _r1 := AllocObject();
  call AllocAnimalInstanceFields(_r1);

  $memoryObject := WriteObject($memoryObject, _a, _r1);

  // read allocated object
  _r2 := ReadObject($memoryObject, _a);

  // commented because it is not declared in this example
  // call Animal.Constructor(_r2);

  call Animal.Foo(_r2);

  _r3 := LoadInstanceFieldAddr($Heap$Animal$val1, _r2);

  // read animal field val1
  _r4 := ReadInt($memoryInt, _r3);

  assert _r4 == 1000;
}

procedure Animal.Foo(a_value: Object) 
{
  var _a : Addr;
  var _r1 : Object; 
  var _r2 : Addr;

  // allocate space for stack and copy argument values
  call _a := AllocAddr();
  $memoryObject := WriteObject($memoryObject, _a, a_value);

  // read object from stack
  _r1 := ReadObject($memoryObject, _a);

  _r2 := LoadInstanceFieldAddr($Heap$Animal$val1, _r1);

  $memoryInt := WriteInt($memoryInt, _r2, 1000);
}

// **************************************************************************


/*
        class Animal
        {
            public int val1;
        }
        static void Main1()
        {
            int x = 5;
            Animal y = new Animal();

            ref int w = ref x;
            ref Animal z = ref y;

            foo(ref w, ref z.val1);
        }

        private static void foo(ref int w, ref int z)
        {
            w = 11;
            z = 12;
        }

*/

/*

  local Int32 x;
  local Animal y;
  local Int32* w;
  local Animal* z;
  local Animal $r7;
  local Int32* $r8;

  B_0002:  nop;
           x = 5;
           y = new Animal;
           Animal::.ctor(y);
           w = &x;
           z = &y;
           $r7 = *z;
           $r8 = &$r7.val1;
           RefKeyword::foo(w, $r8);
           nop;
           return;

*/

procedure Main_Test2()
{
  var _x : Addr;
  var _y : Addr;
  var _z : Addr;
  var _w : Addr;
  var _r7 : Addr;
  var _r8 : Addr;

  // las temp las genere yo
  var _temp1 : Object;

  // alloc stack
  call _x := AllocAddr();
  call _y := AllocAddr(); 
  call _z := AllocAddr();
  call _w := AllocAddr();
  call _r7 := AllocAddr(); 
  call _r8 := AllocAddr();

  // x = 5;
  $memoryInt := WriteInt($memoryInt, _x, 5);

  // y = new Animal;
  call _temp1 := AllocObject();
  call AllocAnimalInstanceFields(_temp1);
  $memoryObject := WriteObject($memoryObject, _y, _temp1);
  // call Animal::.ctor(ReadObject($memoryObject, _y));

  // w = &x;
  $memoryAddr := WriteAddr($memoryAddr, _w, _x);

  // z = &y;
  $memoryAddr := WriteAddr($memoryAddr, _z, _y);

  // $r7 = *z;
  $memoryObject := WriteObject($memoryObject, _r7, ReadObject($memoryObject, ReadAddr($memoryAddr, _z)));

  // $r8 = &$r7.val1;
  $memoryAddr := WriteAddr($memoryAddr, _r8, LoadInstanceFieldAddr($Heap$Animal$val1, ReadObject($memoryObject, _r7)));

  //RefKeyword::foo(w, $r8);
  //  call Foo(ReadAddr($memoryAddr, _w), )

}
