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
        public static extern Task<T> Eventually<T>();
    }
}


class Bank
{
    public class State
    {
        public State()
        {
            balance = 0;
            withdraw = false;
            deposit = false;
        }

        public int balance;
        public bool withdraw;
        public bool deposit;
    }

    public static async Task<int> Withdraw(int i, State state)
    {
        int temp;
        temp = state.balance;
        temp = temp - i;
        await TinyBCT.AsyncStubs.Eventually<int>();
        state.balance = temp;
        state.withdraw = true;
        return state.balance;
    }

    public static async Task<int> Deposit(int i, State state)
    {
        int temp;
        temp = state.balance;
        temp = temp + i;
        state.balance = temp;
        state.deposit = true;
        await TinyBCT.AsyncStubs.Eventually<int>();
        return state.balance;
    }

    public static async Task<int> Test_Interleaved_Transaction1(int i)
    {
        State state = new State();
        int preBalance = state.balance;
        var task_withdraw = Withdraw(i, state);
        var task_deposit = Deposit(i, state);
        await task_withdraw;
        int b = await task_deposit;
        Contract.Assert(preBalance == b);

        return state.balance;
    }
}