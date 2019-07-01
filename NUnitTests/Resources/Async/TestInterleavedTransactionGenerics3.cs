using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
        await Task.FromResult(i);
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
        Contract.Assert(state.withdraw);
        await Task.FromResult(i);
        return state.balance;
    }

    public static async Task<int> Test_Interleaved_Transaction3(int i)
    {
        State state = new State();
        var task_withdraw = Withdraw(i, state);
        var task_deposit = Deposit(i, state);
        await task_withdraw;
        await task_deposit;
        return state.balance;
    }
}