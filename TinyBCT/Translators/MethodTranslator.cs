using Backend;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    class MethodTranslator
    {
        IMethodDefinition methodDefinition;
        MethodBody methodBody;

        // called from Traverser
        // set in Main
        public static void IMethodDefinitionTraverse(IMethodDefinition mD, MethodBody mB)
        {
            MethodTranslator methodTranslator = new MethodTranslator(mD, mB);
            // todo: improve this piece of code
            StreamWriter streamWriter = Program.streamWriter;
            streamWriter.WriteLine(methodTranslator.Translate());
        }

        public MethodTranslator(IMethodDefinition methodDefinition, MethodBody methodBody)
        {
            this.methodDefinition = methodDefinition;
            this.methodBody = methodBody;
        }
        public String Translate()
        {
            StringBuilder sb = new StringBuilder();
            var head = Helpers.GetMethodDefinition(methodDefinition, false);
            sb.AppendLine(head);
            sb.AppendLine("{");

            // local variables declaration - arguments are already declared
            methodBody.Variables.Except(methodBody.Parameters)//.Where(v => v.Type!=null)
                .Select(v =>
                        String.Format("\tvar {0} : {1};", v.Name, Helpers.GetBoogieType(v.Type))
                ).ToList().ForEach(str => sb.AppendLine(str));

            // translate instructions
            methodBody.Instructions
                .Select(ins =>
                        {
                            InstructionTranslator instTranslator = new InstructionTranslator();
                            ins.Accept(instTranslator);
                            return instTranslator.Result();
                        }).ToList().ForEach(str => sb.AppendLine(str)); ;

            sb.AppendLine("}");
            
            return sb.ToString();
        }

    }
}
