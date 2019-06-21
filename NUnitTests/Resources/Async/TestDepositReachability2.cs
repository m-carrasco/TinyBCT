using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

class Bank
{
    static int balance;
    static bool withdraw;
    static bool deposit;

    public static async Task Deposit(int i)
    {
        await Task.Delay(new TimeSpan(1));
        int temp;
        temp = balance;
        temp = temp + i;
        balance = temp;
        deposit = true;
    }

    public static async Task Test_Deposit_Reachability_2(int i)
    {
        withdraw = false;
        deposit = false;
        await Deposit(i);
        Contract.Assert(!deposit);
    }
}