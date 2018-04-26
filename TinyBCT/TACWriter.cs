using Backend;
using Backend.ThreeAddressCode.Instructions;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    class TACWriter
    {
        private static StringBuilder sb;
        private static StreamWriter sw;

        // called from Traverser
        // set in Main
        public static void IMethodDefinitionTraverse(IMethodDefinition mD, MethodBody mB)
        {
            TACWriter.AddMethod(mB);
            TACWriter.Write();
        }

        public static void Open(string inputFile)
        {
            var outputPath = Path.GetDirectoryName(inputFile);
            var name = Path.GetFileName(inputFile);
            sb = new StringBuilder();
            sw = new StreamWriter(Path.Combine(outputPath, String.Format(@"{0}_tac_output.txt",name))); 
        }

        public static void Close()
        {
            sw.Close();
        }

        // strings are appended to the string builder
        public static void AddMethod(MethodBody methodBody)
        {
            sb.Clear();
            sb.Append(MethodBodyToString(methodBody));
        }

        public static void Write()
        {
            sw.WriteLine(sb.ToString());
        }

        // MethodBody's ToString modified to print type of instruction.
        private static string MethodBodyToString(MethodBody methodBody)
        {
            var result = new StringBuilder();
            var header = MemberHelper.GetMethodSignature(methodBody.MethodDefinition, NameFormattingOptions.Signature | NameFormattingOptions.ParameterName);

            result.AppendLine(header);

            foreach (var variable in methodBody.Variables)
            {
                var type = "unknown";

                if (variable.Type != null)
                {
                    type = TypeHelper.GetTypeName(variable.Type);
                }

                result.AppendFormat("  {0} {1};", type, variable.Name);
                result.AppendLine();
            }

            result.AppendLine();

            var notImplemented = " INSTRUCTION NOT IMPLEMENTED ";

            foreach (var instruction in methodBody.Instructions)
            {
                var isImplemented = Helpers.IsInstructionImplemented(instruction);

                if (!isImplemented)
                    Console.WriteLine("Instruction not implemented. Check TAC for more details.");

                result.Append("  ");
                result.Append(instruction);
                result.Append(String.Format("   //{0} {1}", instruction.GetType(), !isImplemented ? notImplemented : String.Empty));
                result.AppendLine();
            }

            foreach (var handler in methodBody.ExceptionInformation)
            {
                result.AppendLine();
                result.Append(handler);
            }

            return result.ToString();
        }
    }
}
