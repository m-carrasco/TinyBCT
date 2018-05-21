type Ref;

type Field;

type Union = Ref;

type HeapType = [Ref][Field]Union;

const unique null: Ref;

var $Alloc: [Ref]bool;

procedure {:inline 1} Alloc() returns (x: Ref);
  modifies $Alloc;

implementation {:inline 1} Alloc() returns (x: Ref)
{
    assume $Alloc[x] == false && x != null;
    $Alloc[x] := true;
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

function Bool2Union(boolValue: bool) : Union;

function Int2Union(intValue: int) : Union;

function Real2Union(realValue: real) : Union;

axiom Union2Int(null) == 0;

axiom Union2Real(null) == 0;

axiom Union2Bool(null) == false;

function Int2Bool(intValue: int) : bool;

axiom Int2Bool(1) == true;

axiom Int2Bool(0) == false;

axiom(forall i:int :: {  Int2Union(i) } Union2Int(Int2Union(i)) == i);

axiom(forall i:int :: {  Real2Union(i) } Union2Real(Real2Union(i)) == i);

axiom(forall b:bool :: {  Bool2Union(b) } Union2Bool(Bool2Union(b)) == b);


type Type = Ref;

function $TypeConstructor(Ref) : int;
function $DynamicType(Ref) : Type;
function $Subtype(Type, Type) : bool;

axiom(forall $T,$T1: Ref:: {  $Subtype($T1, $T) } ($Subtype($T1, $T) && $Subtype($T, $T1)) ==> $T1 == $T );


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

var $Exception : Ref;
var $ExceptionType : Ref;

var $ArrayContents: [Ref][int]Union;
function $ArrayLength(Ref) : int;

procedure $ReadArrayElement(array: Ref, index : int) returns ($result: Union);
implementation $ReadArrayElement(array: Ref, index : int) returns ($result: Union)
{
    assert $ArrayLength(array) > index && index >= 0;
	$result := $ArrayContents[array][index];
}

procedure $WriteArrayElement(array: Ref, index : int, data : Union);
implementation $WriteArrayElement(array: Ref, index : int, data : Union)
{
    assert $ArrayLength(array) > index && index >= 0;
	$ArrayContents := $ArrayContents[array := $ArrayContents[array][index := data]];
}
