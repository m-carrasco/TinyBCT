var F$AsyncStubs.AsyncMethodBuilder.Task : [Ref]Object;
var F$AsyncStubs.Task.Sm : [Ref]Object;
var F$AsyncStubs.Task.IsCompleted : [Ref]bool;
var F$AsyncStubs.TaskAwaiter.Task : [Ref]Object;

procedure $AsyncStubs$EventuallyFinish(param0 : Object) returns ()
{
    yield;
    F$AsyncStubs.Task.IsCompleted[param0] := true;
}