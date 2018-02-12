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

axiom Union2Int(null) == 0;

axiom Union2Bool(null) == false;

function Int2Bool(intValue: int) : bool;

axiom Int2Bool(1) == true;

axiom Int2Bool(0) == false;

type Type = Ref;

function $TypeConstructor(Ref) : int;
function $DynamicType(Ref) : Type;
function $Subtype(Type, Type) : bool;

axiom(forall $T,$T1: Ref:: {  $Subtype($T1, $T) } $Subtype($T1, $T) <==> $T1 == $T || !$Subtype($T, $T1));


function {:extern} T$System.Object() : Ref;
const {:extern} unique T$System.Object: int;

axiom $TypeConstructor(T$System.Object()) == T$System.Object;


function $As(a: Ref, b: Type) : Ref;

axiom (forall a: Ref, b: Type :: { $As(a, b): Ref } $As(a, b): Ref == (if $Subtype($DynamicType(a), b) then a else null));

procedure {:inline 1} System.Object.GetType(this: Ref) returns ($result: Ref);



implementation {:inline 1} System.Object.GetType(this: Ref) returns ($result: Ref)
{
    $result := $DynamicType(this);
}
