﻿// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Backend;
using System.IO;
using TinyBCT.Translators;

namespace TinyBCT
{
    class Program
    {
        public static StreamWriter streamWriter;

        private static void SetupOutputFile()
        {
            var outputResultPath = Path.ChangeExtension(Settings.OutputFile, "bpl");
            streamWriter = new StreamWriter(outputResultPath);
        }

        static void ProcessFiles(Action<string> processAction)
        {
            foreach (var inputFile in Settings.InputFiles)
                processAction(inputFile);
        }

        static void Main(string[] args)
        {
            Settings.Load(args);
            SetupOutputFile();

            Prelude.Write(); // writes prelude.bpl content into the output file

            using (var host = new PeReader.DefaultHost())
            {
                Action<string> writeTAC = (String inputFile) =>
                {
                    using (var assembly = new Assembly(host))
                    {
                        // analysis-net setup
                        assembly.Load(inputFile);
                        Types.Initialize(host);

                        TACWriter.Open(inputFile);
                        var visitor = new Traverser(host, assembly.PdbReader);
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

                        var visitor = new Traverser(host, assembly.PdbReader);
                        visitor.AddNamedTypeDefinitionAction(TypeDefinitionTranslator.TypeDefinitionTranslatorTraverse); // generates axioms for typing 
                        visitor.Traverse(assembly.Module);
                    }
                };

                ProcessFiles(translateTypeDefinitions);

                // TypeDefinitionTranslator.TypeAxioms(); diego's axioms
                // information stored from previous steps is used
                TypeDefinitionTranslator.DefineUndeclaredSuperClasses();

                Action<string> translateMethodDefinitions = (String inputFile) =>
                {
                    using (var assembly = new Assembly(host))
                    {
                        // analysis-net setup
                        assembly.Load(inputFile);
                        Types.Initialize(host);

                        var visitor = new Traverser(host, assembly.PdbReader);
                        visitor.AddMethodDefinitionAction(MethodTranslator.IMethodDefinitionTraverse); // given a IMethodDefinition and a MethodBody are passed to a MethodTranslator object
                        visitor.Traverse(assembly.Module);
                    }
                };

                ProcessFiles(translateMethodDefinitions);
            }

            streamWriter.WriteLine(DelegateStore.DefineMethodsIdentifiers());
            streamWriter.WriteLine(DelegateStore.CreateDelegateMethod());
            streamWriter.WriteLine(DelegateStore.InvokeDelegateMethod());

            // extern method called
            foreach (var methodRef in InstructionTranslator.ExternMethodsCalled)
            {
                var head = Helpers.GetExternalMethodDefinition(methodRef);
                streamWriter.WriteLine(head);
            }

            // we declare read or written fields
            foreach (var field in FieldTranslator.GetFieldDefinitions())
                streamWriter.WriteLine(field);

            streamWriter.Close();
        }
	}
}
