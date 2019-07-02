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

    public static async Task Withdraw(int i)
    {
        await TinyBCT.AsyncStubs.Eventually();
        int temp;
        temp = balance;
        temp = temp - i;
        balance = temp;
        withdraw = true;
    }

    public static async Task Deposit(int i)
    {
        await TinyBCT.AsyncStubs.Eventually();
        int temp;
        temp = balance;
        temp = temp + i;
        balance = temp;
        deposit = true;
    }

    public static async Task Test_Atomic_Transaction_Withdraw_2(int i) // bugs
    {
        withdraw = false;
        deposit = false;
        int preBalance = balance;
        await Withdraw(i);
        Contract.Assert(balance == preBalance - i + 1);
        await Deposit(i);
    }
}