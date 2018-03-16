using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    class Settings
    {
        public static void Load(string[] args)
        {
            if (args.Length == 0)
            {
                /*const string*/
                root = @"..\..\..";
                /*const string*/
                input = root + @"\Test\bin\Debug\Test.dll";
            }
            else
            {
                var i = 0;
                while (args[i].StartsWith(@"/"))
                    i++;
                root = Path.GetDirectoryName(args[i]);
                input = args[i];
                if (String.IsNullOrWhiteSpace(root))
                {
                    root = Directory.GetCurrentDirectory();
                    input = Path.Combine(root, input);
                }
                System.Console.WriteLine(input);
            }
        }

        public static string Root() { return root; }
        public static string Input() { return input; }

        private static string root = String.Empty;
        private static string input = String.Empty;
    }
}
