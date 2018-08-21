// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Backend;
using System.IO;
using TinyBCT.Translators;
using Backend.Model;

namespace TinyBCT
{
    class Program 
    {
        public static StreamWriter streamWriter;

        private static string SetupOutputFile()
        {
            var outputResultPath = Path.ChangeExtension(Settings.OutputFile, "bpl");
            streamWriter = File.CreateText(outputResultPath);
            return outputResultPath;
        }

        static void ProcessFiles(Action<string> processAction)
        {
            foreach (var inputFile in Settings.InputFiles)
                processAction(inputFile);
        }

        static ClassHierarchyAnalysis CreateCHAnalysis(IMetadataHost host)
        {
            foreach (var inputFile in Settings.InputFiles)
            {
                var assembly = new Assembly(host);
                assembly.Load(inputFile);
   
            }
            var CHAnalysis = new ClassHierarchyAnalysis(host);
            CHAnalysis.Analyze();
            return CHAnalysis;

        }


        public static void Main(string[] args)
        {
            Settings.Load(args);
            var outputPath = SetupOutputFile();

            Prelude.Write(); // writes prelude.bpl content into the output file

            using (var host = new PeReader.DefaultHost())
            {
                var CHAnalysis = CreateCHAnalysis(host);
                Types.Initialize(host);

                // TODO(diegog): Analysis not integrated yet
                // This can be used to obtain the allocated types and delegates
                var allocationsAndDelelegatesAnalysis = new TypesAndDelegatesCollector(host);
                //allocationsAndDelelegatesAnalysis.Analyze();

                Action<string> writeTAC = (String inputFile) =>
                {
                    using (var assembly = new Assembly(host))
                    {
                        // analysis-net setup
                        assembly.Load(inputFile);

                        TACWriter.Open(inputFile);
                        var visitor = new Traverser(host, assembly.PdbReader, CHAnalysis);
                        visitor.AddMethodDefinitionAction(TACWriter.IMethodDefinitionTraverse); // saves tac code for debugging
                        visitor.Traverse(assembly.Module);
                        TACWriter.Close();
                    }
                };


                ProcessFiles(writeTAC);

                Action<string> translateTypeDefinitions = (String inputFile) =>
                {
                    using (var assembly = new Assembly(host))
                    {
                        // analysis-net setup
                        assembly.Load(inputFile);
                        Types.Initialize(host);

                        var visitor = new Traverser(host, assembly.PdbReader, CHAnalysis);
                        visitor.AddNamedTypeDefinitionAction(TypeDefinitionTranslator.TypeDefinitionTranslatorTraverse); // generates axioms for typing 
                        visitor.Traverse(assembly.Module);
                    }
                };

                ProcessFiles(translateTypeDefinitions);


                Action<string> translateMethodDefinitions = (String inputFile) =>
                {
                    using (var assembly = new Assembly(host))
                    {
                        // analysis-net setup
                        assembly.Load(inputFile);
                        Types.Initialize(host);

                        var visitor = new Traverser(host, assembly.PdbReader, CHAnalysis);
                        visitor.AddMethodDefinitionAction(MethodTranslator.IMethodDefinitionTraverse); // given a IMethodDefinition and a MethodBody are passed to a MethodTranslator object
                        visitor.Traverse(assembly.Module);
                    }
                };

                ProcessFiles(translateMethodDefinitions);

                Action<string> translateCallsToStaticConstructors = (String inputFile) =>
                {
                    using (var assembly = new Assembly(host))
                    {
                        // analysis-net setup
                        assembly.Load(inputFile);
                        Types.Initialize(host);

                        var visitor = new Traverser(host, assembly.PdbReader, CHAnalysis);
                        visitor.AddMethodDefinitionAction(StaticInitializer.IMethodDefinitionTraverse); // given a IMethodDefinition and a MethodBody are passed to a MethodTranslator object
                        visitor.Traverse(assembly.Module);
                    }
                };

                ProcessFiles(translateCallsToStaticConstructors);
                streamWriter.WriteLine(StaticInitializer.CreateInitializeGlobals());
                streamWriter.WriteLine(StaticInitializer.CreateMainWrappers());

                // TypeDefinitionTranslator.TypeAxioms(); diego's axioms
                // information stored from previous steps is used
                TypeDefinitionTranslator.DefineUndeclaredSuperClasses();

            BoogieLiteral.Strings.WriteStringConsts(streamWriter);

            streamWriter.WriteLine(DelegateStore.DefineMethodsIdentifiers());
            streamWriter.WriteLine(DelegateStore.CreateDelegateMethod());
            streamWriter.WriteLine(DelegateStore.InvokeDelegateMethod());

            

            // extern method called
            foreach (var methodRef in InstructionTranslator.ExternMethodsCalled)
            {
                var head = Helpers.GetExternalMethodDefinition(Helpers.GetUnspecializedVersion(methodRef));
                streamWriter.WriteLine(head);
            }
            foreach (var methodRef in InstructionTranslator.PotentiallyMissingMethodsCalled)
            {
                if (Helpers.IsCurrentlyMissing(methodRef))
                {
                    var head = Helpers.GetExternalMethodDefinition(Helpers.GetUnspecializedVersion(methodRef));
                    streamWriter.WriteLine(head);
                }
            }

            // we declare read or written fields
            foreach (var field in FieldTranslator.GetFieldDefinitions())
                streamWriter.WriteLine(field);

            streamWriter.Close();

            foreach (var bplInputFile in Settings.BplInputFiles)
            {
                var output = new FileStream(outputPath, FileMode.Append, FileAccess.Write);
                ////output.WriteLine("// Appending {0}", bplInputFile);
                ////streamWriter.Flush();

                using (var inputStream = File.OpenRead(bplInputFile))
                {
                    inputStream.CopyTo(output);//streamWriter.BaseStream);
                }

                output.Close();
            }
            }

        }
    }
}
