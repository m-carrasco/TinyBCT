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

class Bank
{
    static int balance;
    static bool withdraw;
    static bool deposit;

    public static async Task Withdraw_Reachability(int i)
    {
        await TinyBCT.AsyncStubs.Eventually();
        int temp;
        temp = balance;
        temp = temp - i;
        balance = temp;
        withdraw = true;
        Contract.Assert(!withdraw);
    }

    public static async Task Test_Withdraw_Reachability(int i)
    {
        withdraw = false;
        deposit = false;
        await Withdraw_Reachability(i);
    }
}