// procedure System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create() returns ($result : Object)
procedure System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.Create() returns ($result : Object)
{
    var $r0 : Object;
    var $r1 : Object;
    var builder : Object;
    var $r2 : Object;
    var local_1 : Object;
    var $r3 : Object;
    var $defaultIntValue : int;
    var $defaultRealValue : real;
    var $defaultBoolValue : bool;
    var $defaultRef : Object;

    $defaultIntValue := 0;
    $defaultRealValue := 0.000000000;
    $defaultBoolValue := false;
    $defaultRef := null;
    $r0 := $defaultRef;
    $r1 := $defaultRef;
    builder := $defaultRef;
    $r2 := $defaultRef;
    local_1 := $defaultRef;
    $r3 := $defaultRef;
    call $r0 := Alloc();

    // changed for generic types 
    //assume  ($DynamicType($r0) == T$System.Runtime.CompilerServices.AsyncTaskMethodBuilder());
    //assume $TypeConstructor($DynamicType($r0)) == T$System.Runtime.CompilerServices.AsyncTaskMethodBuilder;

    assume  ($DynamicType($r0) == T$System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1(T$T$_0()));
    assume $TypeConstructor($DynamicType($r0)) == T$System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1;

    call AsyncStubs.AsyncMethodBuilder`1.#ctor($r0);

    if (($Exception != null))
    {
        return ;
    }

    $r1 := $r0;
    builder := $r1;
    $r2 := builder;
    local_1 := $r2;
    goto L_000B;
L_000B:
    $r3 := local_1;
    $result := $r3;
    return ;

}

//procedure AsyncStubs.AsyncMethodBuilder.#ctor(this : Ref) returns () 
procedure AsyncStubs.AsyncMethodBuilder`1.#ctor(this : Ref) returns ()
{
    var $r0 : Object;
    var $r1 : Object;
    var $r2 : Object;
    var $defaultIntValue : int;
    var $defaultRealValue : real;
    var $defaultBoolValue : bool;
    var $defaultRef : Object;

    assume  (this != null_object);
    // changed for generics
    assume  $Subtype($DynamicType(this),T$System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1(T$T$_0()));
    $defaultIntValue := 0;
    $defaultRealValue := 0.000000000;
    $defaultBoolValue := false;
    $defaultRef := null;
    $r0 := $defaultRef;
    $r1 := $defaultRef;
    $r2 := $defaultRef;
    F$AsyncStubs.AsyncMethodBuilder.Task[this] := $defaultRef;
    $r0 := this;
    call System.Object.#ctor($r0);

    if (($Exception != null))
    {
        return ;
    }

    $r1 := this;
    call $r2 := Alloc();
    // changed for generics
    //assume  ($DynamicType($r2) == T$System.Threading.Tasks.Task());
    //assume $TypeConstructor($DynamicType($r2)) == T$System.Threading.Tasks.Task;
    assume  ($DynamicType($r2) == T$System.Threading.Tasks.Task`1(T$T$_0()));
    assume $TypeConstructor($DynamicType($r2)) == T$System.Threading.Tasks.Task`1;
    call AsyncStubs.Task`1.#ctor($r2);
    F$AsyncStubs.AsyncMethodBuilder.Task[$r1] := $r2;
    return ;

}

//procedure System.Runtime.CompilerServices.AsyncTaskMethodBuilder.get_Task(this : Ref) returns ($result : Object)
procedure System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.get_Task(this : Ref) returns ($result : Object)
{
    var $r0 : Object;
    var $r1 : Object;
    var local_0 : Object;
    var $r2 : Object;
    var $defaultIntValue : int;
    var $defaultRealValue : real;
    var $defaultBoolValue : bool;
    var $defaultRef : Object;

    assume  (this != null_object);
    // changed for generics
    //assume  $Subtype($DynamicType(this), T$System.Runtime.CompilerServices.AsyncTaskMethodBuilder());
    assume  $Subtype($DynamicType(this), T$System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1(T$T$_0()));
    $defaultIntValue := 0;
    $defaultRealValue := 0.000000000;
    $defaultBoolValue := false;
    $defaultRef := null;
    $r0 := $defaultRef;
    $r1 := $defaultRef;
    local_0 := $defaultRef;
    $r2 := $defaultRef;
    $r0 := this;
    $r1 := F$AsyncStubs.AsyncMethodBuilder.Task[$r0];
    // changed for generics
    //assume  $Subtype($DynamicType($r1), T$System.Threading.Tasks.Task());
    assume  $Subtype($DynamicType($r1), T$System.Threading.Tasks.Task`1(T$T$_0()));
    local_0 := $r1;
    goto L_000A;
L_000A:
    $r2 := local_0;
    $result := $r2;
    return ;

}

procedure System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.AwaitUnsafeOnCompleted``2$``0$$``1$(this : Ref,param0 : Addr,param1 : Addr) returns ()
{
    var $r0 : Object;
    var $r1 : Object;
    var $r2 : Object;
    var $r3 : bool;
    var local_0 : bool;
    var $r4 : bool;
    var $r5 : Object;
    var $r6 : Object;
    var $r7 : Object;
    var $r8 : Object;
    var $r9 : Object;
    var $r10 : Object;
    var $r11 : Object;
    var $r12 : Object;
    var $r13 : bool;
    var $defaultIntValue : int;
    var $defaultRealValue : real;
    var $defaultBoolValue : bool;
    var $defaultRef : Object;

    var awaiter : Object;
    var sm : Object;

    // added
    awaiter := ReadObject($memoryObject, param0);
    sm := ReadObject($memoryObject, param1);

    assume  (this != null_object);

    assume  $Subtype($DynamicType(this), T$System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1(T$T$_0()));
    assume  ((awaiter == null_object) || $Subtype($DynamicType(awaiter), T$System.Runtime.CompilerServices.TaskAwaiter`1(T$T$_0())));
    assume  ((sm == null_object) || $Subtype($DynamicType(sm), T$System.Runtime.CompilerServices.IAsyncStateMachine()));

    //assume  $Subtype($DynamicType(this), T$System.Runtime.CompilerServices.AsyncTaskMethodBuilder());
    //assume  ((awaiter == null_object) || $Subtype($DynamicType(awaiter), T$System.Runtime.CompilerServices.TaskAwaiter()));
    //assume  ((sm == null_object) || $Subtype($DynamicType(sm), T$System.Runtime.CompilerServices.IAsyncStateMachine()));

    $defaultIntValue := 0;
    $defaultRealValue := 0.000000000;
    $defaultBoolValue := false;
    $defaultRef := null;
    $r0 := $defaultRef;
    $r1 := $defaultRef;
    $r2 := $defaultRef;
    $r3 := $defaultBoolValue;
    local_0 := $defaultBoolValue;
    $r4 := $defaultBoolValue;
    $r5 := $defaultRef;
    $r6 := $defaultRef;
    $r7 := $defaultRef;
    $r8 := $defaultRef;
    $r9 := $defaultRef;
    $r10 := $defaultRef;
    $r11 := $defaultRef;
    $r12 := $defaultRef;
    $r13 := $defaultBoolValue;
    $r0 := this;
    $r1 := F$AsyncStubs.AsyncMethodBuilder.Task[$r0];
    //assume  $Subtype($DynamicType($r1), T$System.Threading.Tasks.Task());
    assume  $Subtype($DynamicType($r1), T$System.Threading.Tasks.Task`1(T$T$_0()));
    $r2 := null;
    $r3 := ($r1 == $r2);
    local_0 := $r3;
    $r4 := local_0;

    $r8 := this;
    $r9 := F$AsyncStubs.AsyncMethodBuilder.Task[$r8];
    //assume  $Subtype($DynamicType($r9), T$System.Threading.Tasks.Task());
    assume  $Subtype($DynamicType($r9), T$System.Threading.Tasks.Task`1(T$T$_0()));
    $r10 := sm;
    F$AsyncStubs.Task.Sm[$r9] := $r10;
    $r11 := this;
    $r12 := F$AsyncStubs.AsyncMethodBuilder.Task[$r11];
    //assume  $Subtype($DynamicType($r12), T$System.Threading.Tasks.Task());
    assume  $Subtype($DynamicType($r12), T$System.Threading.Tasks.Task`1(T$T$_0()));
    $r13 := false;
    F$AsyncStubs.Task.IsCompleted[$r12] := $r13;
L_0033:

    async call $AsyncStubs`1$ScheduleTask(awaiter, sm);
    return ;

}


procedure System.Threading.Tasks.Task`1.GetAwaiter(this : Ref) returns ($result : Object)
{
    var $r0 : Object;
    var $r1 : Object;
    var awaiter : Object;
    var $r2 : Object;
    var $r3 : Object;
    var $r4 : Object;
    var local_1 : Object;
    var $r5 : Object;
    var $defaultIntValue : int;
    var $defaultRealValue : real;
    var $defaultBoolValue : bool;
    var $defaultRef : Object;

    assume  (this != null_object);
    //assume  $Subtype($DynamicType(this), T$System.Threading.Tasks.Task());
    assume  $Subtype($DynamicType(this), T$System.Threading.Tasks.Task`1(T$T$_0()));
    $defaultIntValue := 0;
    $defaultRealValue := 0.000000000;
    $defaultBoolValue := false;
    $defaultRef := null;
    $r0 := $defaultRef;
    $r1 := $defaultRef;
    awaiter := $defaultRef;
    $r2 := $defaultRef;
    $r3 := $defaultRef;
    $r4 := $defaultRef;
    local_1 := $defaultRef;
    $r5 := $defaultRef;
    call $r0 := Alloc();
    assume  ($DynamicType($r0) == T$System.Runtime.CompilerServices.TaskAwaiter`1(T$T$_0()));
    assume $TypeConstructor($DynamicType($r0)) == T$System.Runtime.CompilerServices.TaskAwaiter`1;
    //assume  ($DynamicType($r0) == T$AsyncStubs.TaskAwaiter());
    //assume $TypeConstructor($DynamicType($r0)) == T$AsyncStubs.TaskAwaiter;
    call AsyncStubs.TaskAwaiter`1.#ctor($r0);

    if (($Exception != null))
    {
        return ;
    }

    $r1 := $r0;
    awaiter := $r1;
    $r2 := awaiter;
    $r3 := this;
    F$AsyncStubs.TaskAwaiter.Task[$r2] := $r3;
    $r4 := awaiter;
    local_1 := $r4;
    goto L_0012;
L_0012:
    $r5 := local_1;
    $result := $r5;
    return ;

}

procedure  AsyncStubs.TaskAwaiter`1.#ctor(this : Ref) returns ()
{
    var $r0 : Object;
    var $defaultIntValue : int;
    var $defaultRealValue : real;
    var $defaultBoolValue : bool;
    var $defaultRef : Object;

    assume  (this != null_object);
    //assume  $Subtype($DynamicType(this), T$System.Runtime.CompilerServices.TaskAwaiter());
    assume  $Subtype($DynamicType(this), T$System.Runtime.CompilerServices.TaskAwaiter`1(T$T$_0()));
    $defaultIntValue := 0;
    $defaultRealValue := 0.000000000;
    $defaultBoolValue := false;
    $defaultRef := null;
    $r0 := $defaultRef;
    F$AsyncStubs.TaskAwaiter.Task[this] := $defaultRef;
    $r0 := this;
    // not sure if null is the default value of Union
    F$AsyncStubs.Task.Result[this] := null;
    call System.Object.#ctor($r0);

    if (($Exception != null))
    {
        return ;
    }

    return ;

}

procedure  System.Runtime.CompilerServices.TaskAwaiter`1.get_IsCompleted(this : Ref) returns ($result : bool)
{
    var $r0 : Object;
    var $r1 : Object;
    var $r2 : bool;
    var local_0 : bool;
    var $r3 : bool;
    var $defaultIntValue : int;
    var $defaultRealValue : real;
    var $defaultBoolValue : bool;
    var $defaultRef : Object;

    assume  (this != null_object);
    //assume  $Subtype($DynamicType(this), T$System.Runtime.CompilerServices.TaskAwaiter());
    assume  $Subtype($DynamicType(this), T$System.Runtime.CompilerServices.TaskAwaiter`1(T$T$_0()));
    $defaultIntValue := 0;
    $defaultRealValue := 0.000000000;
    $defaultBoolValue := false;
    $defaultRef := null;
    $r0 := $defaultRef;
    $r1 := $defaultRef;
    $r2 := $defaultBoolValue;
    local_0 := $defaultBoolValue;
    $r3 := $defaultBoolValue;
    $r0 := this;
    $r1 := F$AsyncStubs.TaskAwaiter.Task[$r0];
    //assume  $Subtype($DynamicType($r1), T$System.Threading.Tasks.Task());
    assume  $Subtype($DynamicType($r1), T$System.Threading.Tasks.Task`1(T$T$_0()));
    $r2 := F$AsyncStubs.Task.IsCompleted[$r1];
    local_0 := $r2;
    goto L_000F;
L_000F:
    $r3 := local_0;
    $result := $r3;
    return ;

}

procedure System.Runtime.CompilerServices.TaskAwaiter`1.GetResult(this : Ref) returns ($result : Object)
{
    var $task : Object;
    $task := F$AsyncStubs.TaskAwaiter.Task[this];
    $result := F$AsyncStubs.Task.Result[$task];
}

procedure System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.SetResult$`0(this : Ref,param0 : Object) returns ()
{
    var $r0 : Object;
    var $r1 : Object;
    var $r2 : bool;
    var $defaultIntValue : int;
    var $defaultRealValue : real;
    var $defaultBoolValue : bool;
    var $defaultRef : Object;
    var $task : Object;

    assume  (this != null_object);
    assume  $Subtype($DynamicType(this), T$System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1(T$T$_0()));
    $defaultIntValue := 0;
    $defaultRealValue := 0.000000000;
    $defaultBoolValue := false;
    $defaultRef := null;
    $r0 := $defaultRef;
    $r1 := $defaultRef;
    $r2 := $defaultBoolValue;
    $r0 := this;
    $r1 := F$AsyncStubs.AsyncMethodBuilder.Task[$r0];

    assume  $Subtype($DynamicType($r1), T$System.Threading.Tasks.Task`1(T$T$_0()));
    $r2 := true;
    F$AsyncStubs.Task.IsCompleted[$r1] := $r2;

    $task := F$AsyncStubs.AsyncMethodBuilder.Task[this];
    F$AsyncStubs.Task.Result[$task] := param0;
    return ;
}

procedure  AsyncStubs.Task`1.#ctor(this : Ref) returns ()
{
    var $r0 : Object;
    var $defaultIntValue : int;
    var $defaultRealValue : real;
    var $defaultBoolValue : bool;
    var $defaultRef : Object;

    assume  (this != null_object);
    assume  $Subtype($DynamicType(this), T$System.Threading.Tasks.Task`1(T$T$_0()));
    $defaultIntValue := 0;
    $defaultRealValue := 0.000000000;
    $defaultBoolValue := false;
    $defaultRef := null;
    $r0 := $defaultRef;
    F$AsyncStubs.Task.IsCompleted[this] := $defaultBoolValue;
    F$AsyncStubs.Task.Sm[this] := $defaultRef;
    $r0 := this;
    call System.Object.#ctor($r0);

    if (($Exception != null))
    {
        return ;
    }
    
    return ;
}

var F$AsyncStubs.Task.Result : [Ref]Union;

// intended to mock the behavior for "default(StructType)"
procedure $AsyncStubs$InitTaskAwaiterGeneric(target : Addr) returns ()
{
    var $r0 : Object;

    call $r0 := Alloc();
    assume  ($DynamicType($r0) == T$System.Runtime.CompilerServices.TaskAwaiter`1(T$T$_0()));
    assume $TypeConstructor($DynamicType($r0)) == T$System.Runtime.CompilerServices.TaskAwaiter`1;
    call AsyncStubs.TaskAwaiter`1.#ctor($r0);

    $memoryObject := WriteObject($memoryObject, target, $r0);
}

procedure System.Threading.Tasks.Task.FromResult``1$``0(param0 : Object) returns ($result : Object)
{
    call $result := Alloc();
    
    call AsyncStubs.Task`1.#ctor($result);
    
    assume  ($DynamicType($result) == T$System.Threading.Tasks.Task`1(T$T$_0()));
    assume $TypeConstructor($DynamicType($result)) == T$System.Threading.Tasks.Task`1;
    
    F$AsyncStubs.Task.Result[$result] := param0;
    
    // i think this may not be appropiate according to docs
    async call $AsyncStubs$EventuallyFinish($result);
}

procedure TinyBCT.AsyncStubs.Eventually``1() returns ($result : Object)
{
    var r : Union;
    
    call $result := Alloc();
    
    call AsyncStubs.Task`1.#ctor($result);
    
    assume  ($DynamicType($result) == T$System.Threading.Tasks.Task`1(T$T$_0()));
    assume $TypeConstructor($DynamicType($result)) == T$System.Threading.Tasks.Task`1;
    
    async call $AsyncStubs$EventuallyFinishGeneric($result);
}

procedure $AsyncStubs$EventuallyFinishGeneric(param0 : Object) returns ()
{
    var r : Union;
    yield;
    F$AsyncStubs.Task.IsCompleted[param0] := true;
    F$AsyncStubs.Task.Result[param0] := r;
}
