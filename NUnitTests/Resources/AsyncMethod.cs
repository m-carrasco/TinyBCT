using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
        await Task.Delay(delay);
        Contract.Assert(p == 1);
        //p = 1 + p;
        Console.WriteLine("After delay");
        //Contract.Assert(p == 2);
    }

    static async Task TwoDelaysMethod(TimeSpan delay, int i)
    {
        Console.WriteLine("Before first delay");
        await Task.Delay(delay);

        Console.WriteLine("Between delays");
        await Task.Delay(delay);
        Console.WriteLine("After second delay");
    }
}

class Bank
{
    static int balance;
    static bool withdraw;
    static bool deposit;

    public static async Task Withdraw(int i)
    {
        await Task.Delay(new TimeSpan(1));
        int temp;
        temp = balance;
        temp = temp - i;
        balance = temp;
        withdraw = true;
    }

    public static async Task Withdraw_Reachability(int i)
    {
        Contract.Assert(false);
        await Task.Delay(new TimeSpan(1));
        int temp;
        temp = balance;
        temp = temp - i;
        balance = temp;
        withdraw = true;
        Contract.Assert(!withdraw); 
    }

    /*public static async Task Withdraw_Bug(int i)
    {
        await Task.Delay(new TimeSpan(1));
        int temp;
        temp = balance;
        temp = temp - i;
        temp = temp - 1; // bug
        balance = temp;
        withdraw = true;
    }*/

    public static async Task Deposit(int i)
    {
        await Task.Delay(new TimeSpan(1));
        int temp;
        temp = balance;
        temp = temp + i;
        balance = temp;
        deposit = true;
    }

    public static async Task Deposit_Reachability(int i)
    {
        await Task.Delay(new TimeSpan(1));
        int temp;
        temp = balance;
        temp = temp + i;
        balance = temp;
        deposit = true;
        Contract.Assert(!deposit);
    }

    /*public static async Task Deposit_Bug(int i)
    {
        await Task.Delay(new TimeSpan(1));
        int temp;
        temp = balance;
        temp = temp + i;
        temp = temp + 1; // bug
        balance = temp;
        deposit = true;
    }*/

    public static async Task Start_Serial_Reachability_NoBugs(int i)
    {
        withdraw = false;
        deposit = false;
        //int pre_balance = balance;
        await Withdraw(i);
        await Deposit(i);
        //Contract.Assert(pre_balance == balance);
        Contract.Assert(deposit && withdraw);
    }

    public static async Task Start_Serial_Reachability_Bugs(int i)
    {
        withdraw = false;
        deposit = false;
        //int pre_balance = balance;
        await Withdraw(i);
        await Deposit(i);
        //Contract.Assert(pre_balance == balance);
        Contract.Assert(!deposit || !withdraw);
    }

    public static async Task Start_Serial_Withdraw_Reachability(int i)
    {
        withdraw = false;
        deposit = false;
        await Withdraw_Reachability(i);
    }

    public static async Task Start_Serial_Deposit_Reachability(int i)
    {
        withdraw = false;
        deposit = false;
        await Deposit_Reachability(i);
    }

    public static async Task Start_Serial_Withdraw_Reachability_2(int i)
    {
        withdraw = false;
        deposit = false;
        await Withdraw(i);
        Contract.Assert(!withdraw);
    }

    public static async Task Start_Serial_Deposit_Reachability_2(int i)
    {
        withdraw = false;
        deposit = false;
        await Deposit(i);
        Contract.Assert(!deposit);
    }

    /*public async Task Start_Parallel(int i)
    {
        withdraw = false;
        deposit = false;

        int pre_balance = balance;
        var t1 = Withdraw(i);
        var t2 = Deposit(i);
        await t1;
        await t2;
        Contract.Assert(pre_balance == balance);
        Contract.Assert(deposit && withdraw);
    }

    public async Task Start_Serial_Bug(int i)
    {
        withdraw = false;
        deposit = false;

        int pre_balance = balance;
        await Withdraw(i);
        await Deposit(i);
        Contract.Assume(withdraw && deposit);
        Contract.Assert(pre_balance == balance);
    }

    public async Task Start_Parallel_Bug(int i)
    {
        withdraw = false;
        deposit = false;

        int pre_balance = balance;
        var t1 = Withdraw(i);
        var t2 = Deposit(i);
        await t1;
        await t2;
        Contract.Assume(withdraw && deposit);
        Contract.Assert(pre_balance == balance);
    }*/

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
