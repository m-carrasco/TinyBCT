using Fclp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyBCT.Utils;

namespace TinyBCT
{
        public class ProgramOptions{

        [Flags]
        public enum MemoryModelOption
        {
            SplitFields = 1,
            Addresses = 2,
            Mixed = 3
        }

        [Flags]
        public enum CheckNullDereferencesLevel
        {
            None = 1,
            Assert = 2,
            Assume = 3
        }

        public ProgramOptions(){
            bplInputFiles = new List<string>();
            inputFiles = new List<string>();
            EmitLineNumbers = false;
            Exceptions = true;
            AtomicInitArray = false;
            CheckNullDereferences = CheckNullDereferencesLevel.None;
            SilentExceptionsForMethods = false;
            DebugLines = false;
            Verbose = false;
            MemoryModel = MemoryModelOption.SplitFields;
            StubGettersSetters = false;
            Z3Strings = false;
            AsyncSupport = false;
            AsyncSupportGenerics = false;
            StubGettersSettersWhitelist = new List<string>();
            TimeoutMinutes = 0;
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
            sb.AppendLine("AtomicinitArray " + AtomicInitArray);
            sb.AppendLine("CheckNullDereferences " + CheckNullDereferences);
            sb.AppendLine("SilentExceptionsForMethods " + SilentExceptionsForMethods);
            sb.AppendLine("DebugLines " + DebugLines);
            sb.AppendLine("Verbose " + Verbose);
            sb.AppendLine("MemoryModel " + MemoryModel);
            sb.AppendLine("StubGettersSetters " + StubGettersSetters);
            sb.AppendLine("Z3Strings " + Z3Strings);
            sb.AppendLine("AsyncSupportGenerics " + AsyncSupportGenerics);
            sb.AppendLine("AsyncSupport " + AsyncSupport);
            sb.AppendLine("StubGettersSetters " + StubGettersSetters);
            sb.AppendLine("Stubs for methods (getter/setter): ");
            if (StubGettersSettersWhitelist.Count > 0)
                foreach (var f in StubGettersSettersWhitelist)
                    sb.AppendLine(f);
            else
                sb.AppendLine("none");
            sb.AppendLine("Timeout (minutes): " + TimeoutMinutes);
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
            OutputFile = Path.ChangeExtension(inputFiles.First(), ".bpl");
        }
        public void SetBplFiles(List<String> files){
            bool ok = files.All((f) => {return File.Exists(f);});
            if (!ok)
                throw new ArgumentException("Bpl input file do not exist");
            bplInputFiles = files;
        }

        public MemoryModelOption MemoryModel;
        private List<String> inputFiles;
        private List<String> bplInputFiles;

        public String OutputFile;

        public bool EmitLineNumbers;
        public bool Exceptions;
        public bool AtomicInitArray;
        public CheckNullDereferencesLevel CheckNullDereferences;
        public bool SilentExceptionsForMethods;
        public bool DebugLines;
        public bool Verbose;
        public bool StubGettersSetters;
        public List<string> StubGettersSettersWhitelist;
        public bool Z3Strings;
        public bool AsyncSupport;
        public bool AsyncSupportGenerics;
        public int TimeoutMinutes;
    }

    class Settings
    {
        public static ProgramOptions programOptions = new ProgramOptions();

        public static void SetProgramOptions(ProgramOptions op){
            programOptions = op;

            if (StubGettersSetters && StubGettersSettersWhitelist.Count > 0)
                throw new Exception("You can automatically stub all getters/setters or just the specified ones (not both at the same time!). You can't use StubGettersSetters and StubGettersSettersWhitelist at the same time. ");

            if ((AsyncSupport || AsyncSupportGenerics) && !AddressesEnabled())
                throw new Exception("AsyncSupport requires addresses. The state machine generated by the compiler for async tasks uses managed pointers.");

            if (TimeoutMinutes > 0)
                Timeout.InitiateTimeoutCheck();
        }

        public static ProgramOptions.MemoryModelOption MemoryModel
        {
            get { return programOptions.MemoryModel; }
        }
        public static IList<string> InputFiles
        {
            get { return programOptions.GetInputFiles(); }
        }
        public static IList<string> BplInputFiles
        {
            get { return programOptions.GetBplInputFiles(); }
        }
        public static string OutputFile
        {
            get { return programOptions.OutputFile; }
        }
        public static bool EmitLineNumbers {
            get { return programOptions.EmitLineNumbers; }
        }
        public static bool Exceptions
        {
            get { return programOptions.Exceptions; }
        }
        public static bool AtomicInit
        {
            get { return programOptions.AtomicInitArray; }
        }
        public static ProgramOptions.CheckNullDereferencesLevel CheckNullDereferences
        {
            get { return programOptions.CheckNullDereferences; }
        }
        public static bool SilentExceptionsForMethods
        {
            get { return programOptions.SilentExceptionsForMethods; }
        }
        public static bool DebugLines
        {
            get { return programOptions.DebugLines; }
        }
        public static bool Verbose
        {
            get { return programOptions.Verbose; }
        }
        public static bool StubGettersSetters
        {
            get { return programOptions.StubGettersSetters; }
        }
        public static bool Z3Strings
        {
            get { return programOptions.Z3Strings; }
        }
        public static bool AsyncSupport
        {
            get { return programOptions.AsyncSupport; }
        }
        public static bool AsyncSupportGenerics
        {
            get { return programOptions.AsyncSupportGenerics; }
        }
        public static List<string> StubGettersSettersWhitelist
        {
            get { return programOptions.StubGettersSettersWhitelist; }
        }
        public static int TimeoutMinutes
        {
            get { return programOptions.TimeoutMinutes; }
        }
        public static bool AddressesEnabled() { return MemoryModel >= ProgramOptions.MemoryModelOption.Addresses; }
        public static bool SplitFieldsEnabled() { return MemoryModel == ProgramOptions.MemoryModelOption.SplitFields || MemoryModel == ProgramOptions.MemoryModelOption.Mixed; }
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

            p.Setup<bool>('a', "atomicInitArray")
            .Callback(b => options.AtomicInitArray = b)
            .WithDescription("Handles atomic initialization of arrays.");

            p.Setup<ProgramOptions.CheckNullDereferencesLevel>("CheckNullDereferences")
            .Callback(b => options.CheckNullDereferences = b)
            .WithDescription("Assert or assume that an object cannot be null before a dereference. By default there is no assertion or assumption added.");

            // dangerous option because the exception can leave the translator in a inconsistent state
            p.Setup<bool>("SilentExceptionsForMethods")
            .Callback(b => options.SilentExceptionsForMethods = b)
            .WithDescription(
                "If InvalidOperationException is thrown during a method translation, " +
                "the translation will continue silently and the method will be replaced " +
                "with an extern method (not necessarily a sound over-approximation).");
                
            p.Setup<bool>("DebugLines")
                .Callback(b => options.DebugLines = b)
                .WithDescription("This settings forces the line numbers to be printed even when no input file exists (TinyBCT can be called without a file).");

            p.Setup<bool>('v', "Verbose")
                .Callback(b => options.Verbose = b)
                .WithDescription("Verbose output.");

            p.Setup<ProgramOptions.MemoryModelOption>("MemoryModel")
                .Callback(d => options.MemoryModel = d);

            p.Setup<bool>("StubGettersSetters")
                .Callback(d => options.StubGettersSetters = d)
                .WithDescription("Automatically generates stubs for every getter/setter being invoked but not defined in the input assemblies (heuristic).");

            p.Setup<List<string>>("StubGettersSettersWhitelist")
                .Callback(d => options.StubGettersSettersWhitelist = d)
                .WithDescription("Generates stubs for specified getters & setters (you have to write the boogie procedure name of the getter/setter).");

            p.Setup<bool>("Z3Strings")
                .Callback(d => options.Z3Strings = d);

            p.Setup<bool>("AsyncSupport")
                .Callback(d => options.AsyncSupport = d)
                .WithDescription("Add stubs to support async keyword with Task (no generic version).");

            p.Setup<bool>("AsyncSupportGenerics")
                .Callback(d => options.AsyncSupportGenerics = d)
                .WithDescription("Add stubs to support async keyword with Task<T> (generic version).");

            p.Setup<int>("TimeoutMinutes")
                .Callback(d => options.TimeoutMinutes = d)
                .WithDescription("Timeout for TinyBCT (minutes). It throws an exception when the time limit is reached.");

            var result = p.Parse(args);

            if (result.HasErrors)
            {
                throw new Exception($"Unable to parse command line arguments: {result.ErrorText}");
            }

            return options;
        }
    }
}
