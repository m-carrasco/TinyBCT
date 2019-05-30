// ************** NEW MEMORY MODELLING ************

type Addr;
type Object = Union;
// already declared in old prelude
// type Union;

var $AllocAddr: [Addr]bool;
var $AllocObject: [Object]bool;

const unique null_addr: Addr;
const null_object : Object;
axiom null_object == null;

type HeapInt = [Addr]int;
type HeapBool = [Addr]bool;
type HeapReal = [Addr]real;
type HeapAddr = [Addr]Addr;
type HeapObject = [Addr]Object;

// for fields
type InstanceFieldAddr = [Object]Addr;

procedure {:allocator} {:inline 1} AuxAllocAddr() returns (x: Addr);

procedure {:inline 1} AllocAddr() returns (x: Addr);
  modifies $AllocAddr;

implementation {:inline 1} AllocAddr() returns (x: Addr)
{
    call x := AuxAllocAddr();
    assume $AllocAddr[x] == false; //&& x != null_addr;
	assume (forall aA : InstanceFieldAddr, oA : Object :: { LoadInstanceFieldAddr(aA, oA) } LoadInstanceFieldAddr(aA, oA) != x);
    $AllocAddr[x] := true;
}

axiom (forall aA, aB : InstanceFieldAddr, oA, oB : Object :: {LoadInstanceFieldAddr(aA, oA), LoadInstanceFieldAddr(aB, oB)} (oA != oB || aA != aB) ==> LoadInstanceFieldAddr(aA, oA) != LoadInstanceFieldAddr(aB, oB));

procedure {:allocator} {:inline 1} AuxAllocObject() returns (x: Object);
procedure {:inline 1} AllocObject() returns (x: Object);
  modifies $AllocObject;

implementation {:inline 1} AllocObject() returns (x: Object)
{
    call x := AuxAllocObject();
    assume $AllocObject[x] == false && x != null_object;
    $AllocObject[x] := true;
}

var $memoryInt : HeapInt;
var $memoryAddr : HeapAddr;
var $memoryBool : HeapBool;
var $memoryReal : HeapReal;
var $memoryObject : HeapObject;

function {:inline true} LoadInstanceFieldAddr(H: InstanceFieldAddr, o: Object) : Addr
{
  H[o]
}

function {:inline true} StoreInstanceFieldAddr(H: InstanceFieldAddr, o: Object, v : Addr) : InstanceFieldAddr
{
  H[o := v]
}

function {:inline true} ReadBool(H: HeapBool, a: Addr) : bool
{
  H[a]
}

function {:inline true} WriteBool(H: HeapBool, a: Addr, v : bool) : HeapBool
{
  H[a := v]
}

function {:inline true} ReadInt(H: HeapInt, a: Addr) : int
{
  H[a]
}

function {:inline true} WriteInt(H: HeapInt, a: Addr, v : int) : HeapInt
{
  H[a := v]
}

function {:inline true} ReadReal(H: HeapReal, a: Addr) : real
{
  H[a]
}

function {:inline true} WriteReal(H: HeapReal, a: Addr, v : real) : HeapReal
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

// **************************

type Ref;

type Field;

type Union = Ref;

type HeapType = [Ref][Field]Union;

const {:allocated} unique null: Ref;

var $Alloc: [Ref]bool;

procedure {:allocator} AuxAlloc() returns (x: Ref);
procedure {:inline 1} Alloc() returns (x: Ref);
  modifies $AllocObject;

implementation {:inline 1} Alloc() returns (x: Ref)
{
    call x := AllocObject();
}

var $Heap: HeapType;

function {:inline true} Read(H: HeapType, o: Ref, f: Field) : Union
{
  H[o][f]
}

function {:inline true} Write(H: HeapType, o: Ref, f: Field, v: Union) : HeapType
{
  H[o := H[o][f := v]]
}

function Union2Bool(u: Union) : bool;

function Union2Int(u: Union) : int;

function Union2Real(u: Union) : real;

function Bool2Union(boolValue: bool) : Union;

function Int2Union(intValue: int) : Union;

function Real2Union(realValue: real) : Union;

function Int2Bool(intValue: int) : bool;

type Type = Ref;

function $TypeConstructor(Ref) : int;
function $DynamicType(Ref) : Type;
function $Subtype(Type, Type) : bool;

axiom(forall $T,$T1: Ref:: {  $Subtype($T1, $T) } $T1 == $T ==> $Subtype($T1, $T) );
axiom(forall $T,$T1: Ref:: {  $Subtype($T1, $T) } ($Subtype($T1, $T) && $Subtype($T, $T1)) ==> $T1 == $T );
axiom(forall $T,$T1,$T2: Ref:: {  $Subtype($T2, $T1), $Subtype($T1, $T) } ($Subtype($T2, $T1) && $Subtype($T1, $T)) ==> $Subtype($T2, $T) );
axiom(forall $T,$T1,$T2: Ref:: {  $Subtype($T2, $T1), $Subtype($T2, $T) } ($Subtype($T2, $T1) && $Subtype($T1, $T)) ==> $Subtype($T2, $T) );

function {:extern} T$System.Object() : Ref;
const {:extern} unique T$System.Object: int;

axiom $TypeConstructor(T$System.Object()) == T$System.Object;

axiom $Subtype(T$System.Object(), T$System.Object());


function $As(a: Ref, b: Type) : Ref;

axiom (forall a: Ref, b: Type :: { $As(a, b): Ref } $As(a, b): Ref == (if $Subtype($DynamicType(a), b) then a else null));

procedure {:inline 1} System.Object.GetType(this: Ref) returns ($result: Ref);



implementation {:inline 1} System.Object.GetType(this: Ref) returns ($result: Ref)
{
    $result := $DynamicType(this);
}

// used for delegates
function $RefToDelegateMethod(int, Ref) : bool;
function $RefToDelegateReceiver(int, Ref) : Ref;
function $RefToDelegateTypeParameters(int, Ref) : Type;
// used for delegates - don't know yet what it is for (from BCT)
function Type0() : Ref;

procedure System.String.Equals$System.String(a$in: Ref, b$in: Ref) returns ($result: bool);
procedure System.String.op_Equality$System.String$System.String(a$in: Ref, b$in: Ref) returns ($result: bool);
procedure System.String.op_Inequality$System.String$System.String(a$in: Ref, b$in: Ref) returns ($result: bool);

implementation System.String.Equals$System.String(a$in: Ref, b$in: Ref) returns ($result: bool)
{
  $result := (a$in == b$in);
}

implementation System.String.op_Equality$System.String$System.String(a$in: Ref, b$in: Ref) returns ($result: bool)
{
  $result := (a$in == b$in);
}

implementation System.String.op_Inequality$System.String$System.String(a$in: Ref, b$in: Ref) returns ($result: bool)
{
  $result := (a$in != b$in);
}

// global state for exceptions
var $Exception : Ref;
var $ExceptionType : Ref;
// state of exceptions during catch execution - useful for rethrows
// catch handlers set $Exception to null, we might need the previous value if there is a rethrow.
var $ExceptionInCatchHandler : Ref;
var $ExceptionInCatchHandlerType : Ref;

var $ArrayContents: [Ref][int]Union;
function $ArrayLength(Ref) : int;

procedure $ReadArrayElement(array: Ref, index : int) returns ($result: Union);
implementation $ReadArrayElement(array: Ref, index : int) returns ($result: Union)
{
    //assert $ArrayLength(array) > index && index >= 0;
	var m : [int]Union;
	m := $ArrayContents[array];
	$result := m[index];
}

procedure $WriteArrayElement(array: Ref, index : int, data : Union);
implementation $WriteArrayElement(array: Ref, index : int, data : Union)
{
    //assert $ArrayLength(array) > index && index >= 0;
	var m : [int]Union;
	m := $ArrayContents[array];
	m[index] := data;
	$ArrayContents[array] := m;
}

// atomic initialization of array elements
procedure $HavocArrayElementsNoNull(array: Ref);
implementation $HavocArrayElementsNoNull(array: Ref)
{
	var $newArrayContents: [int]Union;
	assume (forall $tmp1: int :: $newArrayContents[$tmp1] != null);
	$ArrayContents[array] := $newArrayContents;
}

const unique $BoolValueType: int;

const unique $IntValueType: int;

const unique $RealValueType: int;

procedure {:inline 1} $BoxFromBool(b: bool) returns (r: Ref);

implementation {:inline 1} $BoxFromBool(b: bool) returns (r: Ref)
{
    call r := Alloc();
    assume $TypeConstructor($DynamicType(r)) == $BoolValueType;
    assume Union2Bool(r) == b;
}

procedure {:inline 1} $BoxFromInt(i: int) returns (r: Ref);

implementation {:inline 1} $BoxFromInt(i: int) returns (r: Ref)
{
    call r := Alloc();
    assume $TypeConstructor($DynamicType(r)) == $IntValueType;
    assume Union2Int(r) == i;
}

procedure {:inline 1} $BoxFromReal(r: real) returns (rf: Ref);

implementation {:inline 1} $BoxFromReal(r: real) returns (rf: Ref)
{
    call rf := Alloc();
    assume $TypeConstructor($DynamicType(rf)) == $RealValueType;
    assume Union2Real(rf) == r;
}

procedure {:inline 1} $BoxFromUnion(u: Union) returns (r: Ref);

implementation {:inline 1} $BoxFromUnion(u: Union) returns (r: Ref)
{
    r := u;
}

procedure {:inline 1} $BoxFromObject(u: Union) returns (r: Ref);

implementation {:inline 1} $BoxFromObject(u: Union) returns (r: Ref)
{
    r := u;
}
