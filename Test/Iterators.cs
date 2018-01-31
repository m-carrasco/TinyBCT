using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    public static class Iterators
    {
        static void Main()
        {
            var values = new[] { "a", "b", "c" };
            values.ForEach((pos, s) => Console.WriteLine("{0}: {1}", pos, s));
        }

        static void ForEach<T>(
                this IEnumerable<T> source,
                Action<int, T> action)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (action == null) throw new ArgumentNullException("action");

            int position = 0;
            foreach (T item in source)
            {
                action(position++, item);
            }
        }
    }
}
