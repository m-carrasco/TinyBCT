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

    public static async Task Test_Atomic_Transaction_3(int i)
    {
        withdraw = false;
        deposit = false;
        int preBalance = balance;
        await Withdraw(i);
        await Deposit(i);
        Contract.Assert(preBalance != balance);
    }
}