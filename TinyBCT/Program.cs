// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Backend;
using System.IO;

namespace TinyBCT
{
	class Program
	{
        public static StreamWriter streamWriter;

        static void Main(string[] args)
		{
            string root = String.Empty;
            string input = String.Empty;

            if (args.Length == 0)
            {
                /*const string*/ root = @"..\..\..";
                /*const string*/ input = root + @"\Test\bin\Debug\Test.dll";
            } else
            {
                root = args[0];
                input = args[1];
            }


			using (var host = new PeReader.DefaultHost())
			using (var assembly = new Assembly(host))
			{
				assembly.Load(input);

				Types.Initialize(host);

                // *********** store code in TAC *****************

                var mtVisitor = new TACWriterVisitor(host, assembly.PdbReader);
                mtVisitor.Traverse(assembly.Module);
                streamWriter = new StreamWriter(@"C:\tac_output.txt");
                streamWriter.WriteLine(mtVisitor.ToString());
                streamWriter.Close();

                // ***********************************************

                var streamReader = new StreamReader(@"prelude.bpl");
                streamWriter = new StreamWriter(@"C:\result.bpl");
                // prelude
                streamWriter.WriteLine(streamReader.ReadToEnd());
                streamReader.Close();
                
                var tVisitor = new TypeVisitor(host, assembly.PdbReader);
                tVisitor.Traverse(assembly.Module);

                var visitor = new MethodTranslationVisitor(host, assembly.PdbReader);
				visitor.Traverse(assembly.Module);

                // extern method called
                foreach (var methodRef in InstructionTranslator.ExternMethodsCalled)
                {
                    var head = Helpers.GetMethodDefinition(methodRef, true);
                    streamWriter.WriteLine(head);
                }

                streamWriter.Close();
			}

			System.Console.WriteLine("Done!");
			System.Console.ReadKey();
		}
	}
}
