using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    class AsyncStubs
    {
        public static extern Task Eventually();
    }
}

class AsyncClass
{
    static async Task EmptyAsyncMethod(TimeSpan delay, int i)
    {

    }

    static async Task OneDelayMethod(TimeSpan delay, int i)
    {
        int p = 0;
        Console.WriteLine("Before first delay");
        p = 1;
        await TinyBCT.AsyncStubs.Eventually();
        Contract.Assert(p == 1);
        //p = 1 + p;
        Console.WriteLine("After delay");
        //Contract.Assert(p == 2);
    }

    static async Task TwoDelaysMethod(TimeSpan delay, int i)
    {
        Console.WriteLine("Before first delay");
        await TinyBCT.AsyncStubs.Eventually();

        Console.WriteLine("Between delays");
        await TinyBCT.AsyncStubs.Eventually();
        Console.WriteLine("After second delay");
    }
}

/*namespace AsyncStubs
{
    class AsyncMethodBuilder
    {
        AsyncMethodBuilder()
        {
            this.Task = new Task();          
        }

        void AwaitUnsafeOnCompleted(TaskAwaiter awaiter, IAsyncStateMachine sm)
        {
            // pre: sm is configured to execute its next state when MoveNext is called
            // sm next's state must be scheduled as a continuation and executed after awaiter finished.
            // here we must create a task that represents the execution of the state machine (sm) and we don't have to block
            // sm keeps a reference to awaiter because when it resumes must consume it
                
            this.Task.Sm = sm;
            this.Task.IsCompleted = false;

            //call async {
            //    assume awaiter.IsCompleted();
            //    yield;
            //    call sm.MoveNext(); // si termina setea la tarea como completada
            //}
        }


        static AsyncMethodBuilder Create()
        {
            AsyncMethodBuilder builder = new AsyncMethodBuilder();
            return builder;
        }

        void SetResult()
        {
            this.Task.IsCompleted = true;
        }

        //void SetResult<T>(T result)
        //{
        //    this.Task.IsCompleted = true;
        //    this.Task.Result = result;
        //}

        void Start(IAsyncStateMachine sm)
        {
            // pre: sm esta seteada para arrancar desde el estado inicial y capturo su entorno
            sm.MoveNext();
        }

        Task get_Task()
        {
            return Task;
        }

        public Task Task;
        //private Task<T> task;
        //private Task<void> t;
    }


    class Task
    {
        public TaskAwaiter GetAwaiter()
        {
            var awaiter = new TaskAwaiter();
            awaiter.Task = this;
            return awaiter; // siempre uno distinto?
        }

        public bool IsCompleted;
        public IAsyncStateMachine Sm;
    }

    class TaskAwaiter
    {
        public bool IsCompleted()
        {
            return Task.IsCompleted;
        }

        public Task Task;
    }*/

/*class Task<T>
{
    public TaskAwaiter<T> GetAwaiter()
    {
        var awaiter = new TaskAwaiter<T>();
        awaiter.Task = this;
        return awaiter; // siempre uno distinto?
    }

    public bool IsCompleted;
    public IAsyncStateMachine Sm;
    public T Result;
}*/

/*class TaskAwaiter<T>
{
    public bool IsCompleted()
    {
        return Task.IsCompleted;
    }

    public Task<T> Task;
}*/
//}
