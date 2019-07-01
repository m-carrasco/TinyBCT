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

    public static async Task<int> Withdraw(int i)
    {
        await Task.FromResult(i);
        int temp;
        temp = balance;
        temp = temp - i;
        balance = temp;
        withdraw = true;
        return balance;
    }

    public static async Task<int> Test_Withdraw_Reachability_2(int i)
    {
        withdraw = false;
        int t = await Withdraw(i);
        Contract.Assert(!withdraw);
        return t;
    }
}