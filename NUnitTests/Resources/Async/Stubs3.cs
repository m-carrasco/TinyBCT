using System;
using System.Threading.Tasks;
using TinyBCT;

namespace TinyBCT
{
    public class AsyncStubs
    {
        public extern static Task<T> Eventually<T>();
    }

    public static class Extensions
    {
        public static async Task<T> ReadAsAsyncStub<T>(this HttpContent content)
        {
            return await TinyBCT.AsyncStubs.Eventually<T>();
        }
    }
}

public class HttpContent
{

}

namespace NUnitTests.Resources.Async
{
    public class Stubs3
    {
        public Stubs3()
        {
        }

        public async Task<int> Stubs(HttpContent content)
        {
            return await content.ReadAsAsyncStub<int>();
        }
    }
}
