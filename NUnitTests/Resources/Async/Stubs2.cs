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

    public async Task<string> EntryPoint()
    {
        State state = new State();

        var t = ChangeState(state);
        bool check = state.A == true && state.B == false;
        var r = await t;

        Contract.Assert(!check);

        return r;
    }

    public async Task<string> ChangeState(State state)
    {
        state.A = true;
        var str = await TinyBCT.AsyncStubs.Eventually<string>();
        state.B = true;
        return str;
    }
}

namespace TinyBCT
{
    class AsyncStubs
    {
        public static extern Task<T> Eventually<T>();
    }
}
