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
        Contract.Assert(!state.deposit);
        return state.balance;
    }
    
    public static async Task<int> Test_Interleaved_Transaction6(int i)
    {
        State state = new State();
        var t_withdraw = Withdraw(i, state);
        state.deposit = true;
        await t_withdraw;
        return state.balance;
    }
}