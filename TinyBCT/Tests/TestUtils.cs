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
