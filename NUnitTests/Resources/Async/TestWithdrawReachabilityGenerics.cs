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
    static int balance;
    static bool withdraw;

    public static async Task<int> Withdraw_Reachability(int i)
    {
        await TinyBCT.AsyncStubs.Eventually<int>();
        int temp;
        temp = balance;
        temp = temp - i;
        balance = temp;
        withdraw = true;
        Contract.Assert(!withdraw);
        return balance;
    }

    public static async Task<int> Test_Withdraw_Reachability(int i)
    {
        withdraw = false;
        int t = await Withdraw_Reachability(i);
        return t;
    }
}