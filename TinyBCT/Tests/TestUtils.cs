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
        public static bool CreateAssemblyDefinition(string code, string name)
        {
            var parseOptions = new CSharpParseOptions().WithPreprocessorSymbols("DEBUG");
            var syntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions);

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithOptimizationLevel(OptimizationLevel.Debug);
            //options = options.WithGeneralDiagnosticOption(ReportDiagnostic.Warn);
            //options = options.;
 
            CSharpCompilation compilation = CSharpCompilation.Create(
                name,
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
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
