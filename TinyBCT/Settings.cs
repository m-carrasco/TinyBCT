using Fclp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
        public class ProgramOptions{

        public ProgramOptions(){
            bplInputFiles = new List<string>();
            inputFiles = new List<string>();
            EmitLineNumbers = false;
            Exceptions = true;
            SplitFields = true;
            AtomicInitArray = false;
            AvoidSubtypeCheckingForInterfaces = false;
            CheckNullDereferences = false;
            DebugLargeDLL = false;
            SilentExceptionsForMethods = false;
            NewAddrModelling = false;
            FastAddrModelling = false;
            DebugLines = false;
            Verbose = false;
        }

        public override string ToString(){
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("TinyBCT is executed with the following options:");
            sb.AppendLine("Input files: ");
            foreach(var f in inputFiles)
                sb.AppendLine(f);
            sb.AppendLine("Bpl input files: ");
            if (bplInputFiles.Count > 0)
                foreach(var f in bplInputFiles)
                    sb.AppendLine(f);
            else
                sb.AppendLine("non bpl input files");
            sb.AppendLine("OutputFile " + OutputFile);
            sb.AppendLine("EmitLineNumbers " + EmitLineNumbers);
            sb.AppendLine("Exceptions " + Exceptions);
            sb.AppendLine("SplitFields " + SplitFields);
            sb.AppendLine("AtomicinitArray " + AtomicInitArray);
            sb.AppendLine("AvoidSubtypeCheckingForInterfaces " + AvoidSubtypeCheckingForInterfaces);
            sb.AppendLine("CheckNullDereferences " + CheckNullDereferences);
            sb.AppendLine("DebugLargeDLL " + DebugLargeDLL);
            sb.AppendLine("SilentExceptionsForMethods " + SilentExceptionsForMethods);
            sb.AppendLine("NewAddrModelling " + NewAddrModelling);
            sb.AppendLine("FastAddrModelling " + FastAddrModelling);
            sb.AppendLine("DebugLines " + DebugLines);
            sb.AppendLine("Verbose " + Verbose);
            return sb.ToString();
        }
        public IList<string> GetInputFiles(){return inputFiles;}
        public IList<string> GetBplInputFiles() {return bplInputFiles;}
        public void SetInputFiles(List<String> files){
            if (files.Count == 0)
                throw new ArgumentOutOfRangeException("At least there must be a input file");

            bool ok = files.All((f) => {return File.Exists(f);});
            if (!ok)
                throw new ArgumentException("Input files do not exist");

            inputFiles = files;
            OutputFile = inputFiles.First();
        }
        public void SetBplFiles(List<String> files){
            bool ok = files.All((f) => {return File.Exists(f);});
            if (!ok)
                throw new ArgumentException("Bpl input file do not exist");
            bplInputFiles = files;
        }

        private List<String> inputFiles;
        private List<String> bplInputFiles;

        public String OutputFile;

        public bool EmitLineNumbers;
        public bool Exceptions;
        public bool SplitFields;
        public bool AtomicInitArray;
        public bool AvoidSubtypeCheckingForInterfaces;
        public bool CheckNullDereferences;
        public bool DebugLargeDLL;
        public bool SilentExceptionsForMethods;
        public bool NewAddrModelling;
        public bool FastAddrModelling;
        public bool DebugLines;
        public bool Verbose;
    }


    class Settings
    {
        public static void SetProgramOptions(ProgramOptions op){
            InputFiles = op.GetInputFiles();
            BplInputFiles = op.GetBplInputFiles();
            OutputFile = op.OutputFile;
            EmitLineNumbers = op.EmitLineNumbers;
            Exceptions = op.Exceptions;
            SplitFields = op.SplitFields;
            AtomicInit = op.AtomicInitArray;
            AvoidSubtypeCheckingForInterfaces = op.AvoidSubtypeCheckingForInterfaces;
            CheckNullDereferences = op.CheckNullDereferences;
            DebugLargeDLL = op.DebugLargeDLL;
            SilentExceptionsForMethods = op.SilentExceptionsForMethods;
            NewAddrModelling = op.NewAddrModelling;
            FastAddrModelling = op.FastAddrModelling;
            DebugLines = op.DebugLines;
            Verbose = op.Verbose;
        }

        public static IList<string> InputFiles;
        public static IList<string> BplInputFiles;
        public static string OutputFile;
        public static bool EmitLineNumbers;
        public static bool Exceptions;
        public static bool SplitFields;
        public static bool AtomicInit;
        public static bool AvoidSubtypeCheckingForInterfaces;
        public static bool CheckNullDereferences;
        public static bool DebugLargeDLL;
        public static bool SilentExceptionsForMethods;
        public static bool NewAddrModelling;
        public static bool FastAddrModelling;
        public static bool DebugLines;
        public static bool Verbose;

        public static ProgramOptions CreateProgramOptions(string[] args)
        {
            ProgramOptions options = new ProgramOptions();
            var p = new FluentCommandLineParser();

            // example --inputFiles C:\file1.txt C:\file2.txt "C:\other file.txt"
            p.Setup<List<string>>('i', "inputFiles")
                .Callback(items => options.SetInputFiles(items))
                .Required()
                .WithDescription(@"Path for input files. By default it targets the test dlls. --inputFiles C:\file1.dll C:\file2.exe ""C:\other file.dll""");

            p.Setup<string>('o', "outputFile")
                .Callback(o => options.OutputFile = o)
                .WithDescription("Path to output file. Any previous extension will be removed and .bpl will be added. By default it is the same name and path of the first input file.");

            p.Setup<List<string>>('b', "bplFiles")
            .Callback(bs => options.SetBplFiles(bs))
            .WithDescription("Path to input bpl files that will be appended at the end of the resulting output file");

            p.Setup<bool>('l', "lineNumbers")
            .Callback(b => options.EmitLineNumbers = b)
            .WithDescription("Emit line numbers from source code in .bpl file. By default is true");

            p.Setup<bool>('e', "exceptions")
             .Callback(b => options.Exceptions = b)
             .WithDescription("Enable translation of exceptions handling.");

            p.Setup<bool>('s', "splitFields")
             .Callback(b => options.SplitFields = b)
             .WithDescription("Models the heap splitting instance fields into different dictionaries.");

            p.Setup<bool>('a', "atomicInitArray")
            .Callback(b => options.AtomicInitArray = b)
            .WithDescription("Handles atomic initialization of arrays.");

            p.Setup<bool>('t', "avoidSubtypeForInterfaces")
            .Callback(b => options.AvoidSubtypeCheckingForInterfaces = b)
            .WithDescription("Do not use subtypes hierarquies for variables/parameters of interface types (open world)");

            p.Setup<bool>("checkNullDereferences")
            .Callback(b => options.CheckNullDereferences = b)
            .WithDescription("Add assertions at every dereference checking that the reference is not null.");

            p.Setup<bool>('d', "DebugLargeDLL")
            .Callback(b => options.DebugLargeDLL = b)
            .WithDescription("Do not use this option.");

            p.Setup<bool>("SilentExceptionsForMethods")
            .Callback(b => options.SilentExceptionsForMethods = b)
            .WithDescription(
                "If InvalidOperationException is thrown during a method translation, " +
                "the translation will continue silently and the method will be replaced " +
                "with an extern method (not necessarily a sound over-approximation).");

            p.Setup<bool>('m', "NewAddrModelling")
                .Callback(b => options.NewAddrModelling = b)
                .WithDescription("Every variable of the three address code will be explicitly allocated in the boogie code. Every variable or field will have a memory address.");

            p.Setup<bool>('f', "FastAddrModelling")
                .Callback(b => options.FastAddrModelling = b)
                .WithDescription("Every variable of the three address code will be explicitly allocated in the boogie code. Every variable or field will have a memory address.");
                
            p.Setup<bool>("DebugLines")
                .Callback(b => options.DebugLines = b)
                .WithDescription("This settings forces the line numbers to be printed even when no input file exists (TinyBCT can be called without a file).");

            p.Setup<bool>('v', "Verbose")
                .Callback(b => options.Verbose = b)
                .WithDescription("Verbose output.");

            var result = p.Parse(args);

            if (result.HasErrors)
            {
                throw new Exception($"Unable to parse command line arguments: {result.ErrorText}");
            }

            return options;
        }
    }
}
