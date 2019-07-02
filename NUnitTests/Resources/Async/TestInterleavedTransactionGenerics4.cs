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
        Contract.Assert(!state.deposit);
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

    public static async Task<int> Test_Interleaved_Transaction4(int i)
    {
        State state = new State();
        var t_withdraw = Withdraw(i, state);
        var t_deposit = Deposit(i, state);
        await t_withdraw;
        await t_deposit;
        return state.balance;
    }
}