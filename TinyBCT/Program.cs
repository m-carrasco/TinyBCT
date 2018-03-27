// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

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
            var outputResultPath = Path.ChangeExtension(Settings.Input(), "bpl");
            streamWriter = new StreamWriter(outputResultPath);
        }

        static void Main(string[] args)
		{
            Settings.Load(args);
            SetupOutputFile();

            using (var host = new PeReader.DefaultHost())
			using (var assembly = new Assembly(host))
			{
                // analysis-net setup
				assembly.Load(Settings.Input());
				Types.Initialize(host);

                // ***********************************************

                Prelude.Write(); // writes prelude.bpl content into the output file
                TACWriter.Open(); //  creates file that will have the tac code
                
                // the first traverser is used for pre processing actions
                var visitor = new Traverser(host, assembly.PdbReader);
                visitor.AddMethodDefinitionAction(TACWriter.IMethodDefinitionTraverse); // saves tac code for debugging
                visitor.AddMethodDefinitionAction(DelegateTranslator.TrackDelegatedMethods); // store every method used in delegates 
                visitor.AddNamedTypeDefinitionAction(TypeDefinitionTranslator.TypeDefinitionTranslatorTraverse); // generates axioms for typing 
                visitor.Traverse(assembly.Module);

                TypeDefinitionTranslator.TypeAxioms();
                streamWriter.WriteLine(DelegateTranslator.DefineMethodsIdentifiers());
                streamWriter.WriteLine(DelegateTranslator.CreateDelegateMethod());

                visitor = new Traverser(host, assembly.PdbReader);
                visitor.AddMethodDefinitionAction(MethodTranslator.IMethodDefinitionTraverse); // given a IMethodDefinition and a MethodBody are passed to a MethodTranslator object
				visitor.Traverse(assembly.Module);

                // extern method called
                foreach (var methodRef in InstructionTranslator.ExternMethodsCalled)
                {
                    var head = Helpers.GetMethodDefinition(methodRef, true);
                    streamWriter.WriteLine(head);
                }

                // we declare read or written fields
                foreach (var field in FieldTranslator.GetFieldDefinitions())
                    streamWriter.WriteLine(field);


            }

            streamWriter.Close();
            TACWriter.Close();

            System.Console.WriteLine("Done!");
			//System.Console.ReadKey();
		}
	}
}
