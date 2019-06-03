using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitTests
{
    class CompilerOptions
    {
        public string SourceCode { get; set; } = null;

        public string OutputName { get; set; } = null;

        public string OutputDirectory { get; set; } = null;

        public IEnumerable<MetadataReference> References { get; set; } = null;
    }

    class Compiler
    {
        public static string CompileSource(string source)
        {
            CompilerOptions compilerOptions = new CompilerOptions();
            var fullPathDll = Path.ChangeExtension(Path.GetTempFileName(), ".dll");
            compilerOptions.OutputName = Path.GetFileName(fullPathDll);
            compilerOptions.OutputDirectory = Path.GetDirectoryName(fullPathDll);
            compilerOptions.SourceCode = source;

            Compiler compiler = new Compiler();
            var dll = compiler.CompileSource(compilerOptions);

            return dll;
        }
        public string CompileSource(CompilerOptions compilerOpts)
        {
            var parseOptions = new CSharpParseOptions().WithPreprocessorSymbols("DEBUG", "CONTRACTS_FULL").WithLanguageVersion(LanguageVersion.CSharp7_3);
            var syntaxTree = CSharpSyntaxTree.ParseText(compilerOpts.SourceCode, options: parseOptions);

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Debug);

            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            IEnumerable<MetadataReference> defaultReferences = new[]
            {
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "Microsoft.CSharp.dll")) // Microsoft.CSharp is required for the dynamic keyword
            };

            var finalReferences = compilerOpts.References ?? defaultReferences;

            CSharpCompilation compilation = CSharpCompilation.Create(
                compilerOpts.OutputName,
                syntaxTrees: new[] { syntaxTree },
                references: finalReferences,
                options: options);


            var outputPath = Path.Combine(compilerOpts.OutputDirectory, Path.ChangeExtension(compilerOpts.OutputName, "dll"));
            var pdbPath = Path.Combine(compilerOpts.OutputDirectory, Path.ChangeExtension(compilerOpts.OutputName, "pdb"));
            if (Environment.OSVersion.Platform.Equals(PlatformID.MacOSX) ||
                Environment.OSVersion.Platform.Equals(PlatformID.Unix))
            {
                pdbPath = null; // pdb writing is only supported in Windows
            }

            var emitResult = compilation.Emit(outputPath, pdbPath);
            Contract.Assert(emitResult.Success);

            return outputPath;
        }
    }
}

