using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

public class Test
{
    public class State
    {
        public State()
        {
            A = false;
            B = false;
        }

        public bool A;
        public bool B;
    }

    public async Task EntryPoint()
    {
        State state = new State();

        var t = ChangeState(state);
        bool check = state.A == true && state.B == false;
        await t;

        Contract.Assert(!check);
    }

    public async Task ChangeState(State state)
    {
        state.A = true;
        await TinyBCT.AsyncStubs.Eventually();
        state.B = true;
    }
}

namespace TinyBCT
{
    class AsyncStubs
    {
        public static extern Task Eventually();
    }
}
