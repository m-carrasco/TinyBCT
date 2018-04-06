using Backend;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyBCT.Translators;

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

        String TranslateInstructions()
        {
            StringBuilder insSb = new StringBuilder();

            int idx = 0;
            InstructionTranslator instTranslator = new InstructionTranslator();
            methodBody.Instructions
                .Select(ins =>
                {
                    var r = instTranslator.Translate(methodBody.Instructions, idx);
                    idx++;
                    return r;
                }).ToList().ForEach(str => insSb.AppendLine(str)); ;

            foreach (var v in instTranslator.RemovedVariables)
                methodBody.Variables.Remove(v);

            foreach (var v in instTranslator.AddedVariables)
                methodBody.Variables.Add(v);

            //methodBody.UpdateVariables();

            return insSb.ToString();
        }

        String TranslateLocalVariables()
        {
            StringBuilder localVariablesSb = new StringBuilder();
            // local variables declaration - arguments are already declared
            methodBody.Variables.Except(methodBody.Parameters)//.Where(v => v.Type!=null)
                .Select(v =>
                        String.Format("\tvar {0} : {1};", v.Name, Helpers.GetBoogieType(v.Type))
                ).ToList().ForEach(str => localVariablesSb.AppendLine(str));

            return localVariablesSb.ToString();
        }

        String TranslateAttr()
        {
            return Helpers.IsMain(methodDefinition) ? " {:entrypoint}" : String.Empty;

        }

        String TranslateReturnTypeIfAny()
        {
            if (methodDefinition.Type.TypeCode != PrimitiveTypeCode.Void)
            {
                var returnType = Helpers.GetMethodBoogieReturnType(methodDefinition);
                return String.Format("returns (r : {0})", returnType);
            }

            return String.Empty;
        }

        public String Translate()
        {
            // instructions must be translated before local variables
            // modification to local variables can ocurr while instruction translation is done
            // for example when delegate creation is detected some local variables are deleted.

            var ins = TranslateInstructions();
            var localVariables = TranslateLocalVariables();
            var methodName = Helpers.GetMethodName(methodDefinition);
            var attr = TranslateAttr();
            var parametersWithTypes = Helpers.GetParametersWithBoogieType(methodDefinition);
            var returnTypeIfAny = TranslateReturnTypeIfAny();

            var boogieProcedureTemplate = new BoogieProcedureTemplate(methodName, attr, localVariables, ins, parametersWithTypes, returnTypeIfAny, Helpers.IsExternal(methodDefinition));
            return boogieProcedureTemplate.TransformText();
        }

    }
}
