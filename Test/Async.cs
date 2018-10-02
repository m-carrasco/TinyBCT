using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Async
    {
        public static void Async1()
        {
            // Start the HandleFile method.
            Task<int> task = DoSomethingAsync(10);

            task.Wait();

            var x = task.Result;
            Contract.Assert(x == 11);

        }

        static async Task<int> DoSomethingAsync(int initValue)
        {
            int count = initValue+1;
            await RunAsync();
            return count;
        }
        static Task RunAsync()
        {
            return Task.CompletedTask;
        }
    }
}
