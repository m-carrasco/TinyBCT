using Fclp;
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
        public static IList<string> InputFiles 
            = new List<string>();

        public static IList<string> BplInputFiles
            = new List<string>();


        public static string OutputFile;

        public static bool EmitLineNumbers = false;
        public static bool Exceptions = true;
        public static bool SplitFields = true;
        public static bool AtomicInit = false;
        public static bool AvoidSubtypeCheckingForInterfaces = false;
        public static bool CheckNullDereferences = false;
        public static bool DebugLargeDLL = false;
        public static bool SilentExceptionsForMethods = false;

        // options should start with /  (currently there are no options)
        // every argument found after the first arg not starting with / will be considered a file to be processed
        public static void Load(string[] args)
        {
            var defaultFiles = new List<string>();
            defaultFiles.Add(@"..\..\..\Test\bin\Debug\Test.dll");
            //defaultFiles.Add(@"..\..\..\Test2\bin\Debug\Test2.dll");
            //defaultFiles.Add(@"..\..\..\Test3\bin\Debug\Test3.dll");

            var p = new FluentCommandLineParser();

            // example --inputFiles C:\file1.txt C:\file2.txt "C:\other file.txt"
            p.Setup<List<string>>('i', "inputFiles")
                .Callback(items => InputFiles = items)
                .SetDefault(defaultFiles)
                .WithDescription(@"Path for input files. By default it targets the test dlls. --inputFiles C:\file1.dll C:\file2.exe ""C:\other file.dll""");

            p.Setup<string>('o', "outputFile")
                .Callback(o => OutputFile = o)
                .SetDefault(String.Empty)
                .WithDescription("Path to output file. Any previous extension will be removed and .bpl will be added. By default it is the same name and path of the first input file.");

            p.Setup<List<string>>('b', "bplFiles")
            .Callback(bs => BplInputFiles = bs)
            .SetDefault(new List<string>())
            .WithDescription("Path to input bpl files that will be appended at the end of the resulting output file");

            p.Setup<bool>('l', "lineNumbers")
            .Callback(b => EmitLineNumbers = b)
            .SetDefault(false)
            .WithDescription("Emit line numbers from source code in .bpl file. By default is true");

            p.Setup<bool>('e', "exceptions")
             .Callback(b => Exceptions = b)
             .SetDefault(true)
             .WithDescription("Enable translation of exceptions handling.");

            p.Setup<bool>('s', "splitFields")
             .Callback(b => SplitFields = b)
             .SetDefault(true)
             .WithDescription("Models the heap splitting instance fields into different dictionaries.");

            p.Setup<bool>('a', "atomicInitArray")
            .Callback(b => AtomicInit = b)
            .SetDefault(false)
            .WithDescription("Handles atomic initialization of arrays.");

            p.Setup<bool>('t', "avoidSubtypeForInterfaces")
            .Callback(b => AvoidSubtypeCheckingForInterfaces = b)
            .SetDefault(false)
            .WithDescription("Do not use subtypes hierarquies for variables/parameters of interface types (open world)");

            p.Setup<bool>("checkNullDereferences")
            .Callback(b => CheckNullDereferences = b)
            .SetDefault(false)
            .WithDescription("Add assertions at every dereference checking that the reference is not null.");

            p.Setup<bool>('d', "DebugLargeDLL")
            .Callback(b => DebugLargeDLL = b)
            .SetDefault(false)
            .WithDescription("Do not use this option.");

            p.Setup<bool>("SilentExceptionsForMethods")
            .Callback(b => SilentExceptionsForMethods = b)
            .SetDefault(false)
            .WithDescription(
                "If InvalidOperationException is thrown during a method translation, " +
                "the translation will continue silently and the method will be replaced " +
                "with an extern method (not necessarily a sound over-approximation).");

            var result = p.Parse(args);

            if (result.HasErrors)
            {
                foreach (var error in result.Errors)
                    Console.WriteLine(error);

                Console.ReadLine();
                System.Environment.Exit(1);
            }
            else
            {
                // check if the input files exist
                bool error = false;
                foreach (var f in InputFiles)
                {
                    if (!File.Exists(f))
                    {
                        error = true;
                        Console.WriteLine(String.Format("File {0} doesn't exit.", f));
                    }
                }

                if (error)
                {
                    Console.ReadLine();
                    System.Environment.Exit(1);
                }

                if (OutputFile.Equals(String.Empty))
                    OutputFile = InputFiles.First();
            }
        }

    }
}
