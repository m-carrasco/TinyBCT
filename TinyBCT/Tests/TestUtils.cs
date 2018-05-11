using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Test
{
    class TestUtils
    {
        public static string rootTinyBCT = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\");
        public static string corralPath = Path.Combine(rootTinyBCT, @"..\corral\bin\Debug\corral.exe");
        public class CorralResult
        {
            public class CorralOutputException : Exception { }

            private string output;
            private string err;
            public CorralResult(string pOutput, string pErr)
            {
                output = pOutput;
                err = pErr;
            }
            private static void SanityCheck(string output, string err, bool checkSyntaxError = true)
            {
                bool result = true;
                result = result && !output.Equals("");
                if (checkSyntaxError)
                {
                    result = result && err.Equals("");
                    result = result && !output.Contains(": error:");
                }
                if (!result)
                {
                    throw new CorralOutputException();
                }
            }
            public bool AssertionFails()
            {
                SanityCheck(this.output, this.err);
                return output.Contains("Program has a potential bug: True bug");
            }
            public bool NoBugs()
            {
                SanityCheck(this.output, this.err);
                return output.Contains("Program has no bugs") && !AssertionFails();
            }
            public bool SyntaxErrors()
            {
                SanityCheck(this.output, this.err, checkSyntaxError: false);
                return this.output.Contains(": error:");
            }
        }
        
        public static CorralResult CallCorral(int recursionBound, string path, string additionalArguments = "")
        {
            System.Diagnostics.Contracts.Contract.Assume(System.IO.File.Exists(path));

            System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.FileName = corralPath;
            pProcess.StartInfo.Arguments = "/recursionBound:" + recursionBound.ToString() + " " + additionalArguments + " " + path;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.RedirectStandardError = true;
            pProcess.Start();
            pProcess.WaitForExit();
            string output = pProcess.StandardOutput.ReadToEnd();
            string err = pProcess.StandardError.ReadToEnd();
            return new CorralResult(output, err);
        }

        public static bool CreateAssemblyDefinition(string code, string name, string[] references = null)
        {
            var parseOptions = new CSharpParseOptions().WithPreprocessorSymbols("DEBUG", "CONTRACTS_FULL");
            var syntaxTree = CSharpSyntaxTree.ParseText(code, options: parseOptions);

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithOptimizationLevel(OptimizationLevel.Debug);

            var metadataRefences = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
            //if (references != null)
            //{
            //    metadataRefences = new[] {
            //        MetadataReference.CreateFromFile(references[0]),
            //        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            //                                }; 
            //}

            CSharpCompilation compilation = CSharpCompilation.Create(
                name,
                new[] { syntaxTree },
                metadataRefences,
                options);

            //using (var dllStream = new MemoryStream())
            //using (var pdbStream = new MemoryStream())
            {
                var outputPath = Path.ChangeExtension(name, "dll");
                var pdbPath = Path.ChangeExtension(name, "pdb");
                var emitResult = compilation.Emit(outputPath, pdbPath);
                //var emitResult = compilation.Emit(dllStream, pdbStream);
                if (!emitResult.Success)
                {
                    return false;
                    // emitResult.Diagnostics
                }
            }
            return true;
        }
    }
}
