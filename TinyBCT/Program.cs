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

        static void Main(string[] args)
		{
            Settings.Load(args);

			using (var host = new PeReader.DefaultHost())
			using (var assembly = new Assembly(host))
			{
				assembly.Load(Settings.Input());

				Types.Initialize(host);

                // *********** store code in TAC *****************

                var mtVisitor = new TACWriterVisitor(host, assembly.PdbReader);
                mtVisitor.Traverse(assembly.Module);
				var outputPath = Path.GetDirectoryName(Settings.Input());

				streamWriter = new StreamWriter(outputPath += @"\tac_output.txt");
                streamWriter.WriteLine(mtVisitor.ToString());
                streamWriter.Close();

				// ***********************************************

				var tinyBCTExeFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				var streamReader = new StreamReader(Path.Combine(tinyBCTExeFolder, @"prelude.bpl"));
				var outputResultPath = Path.ChangeExtension(Settings.Input(), "bpl");
                streamWriter = new StreamWriter(outputResultPath);
                // prelude
                streamWriter.WriteLine(streamReader.ReadToEnd());
                streamReader.Close();
                
                var visitor = new MethodTranslationVisitor(host, assembly.PdbReader);
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

                streamWriter.Close();
			}

			System.Console.WriteLine("Done!");
			//System.Console.ReadKey();
		}
	}
}
