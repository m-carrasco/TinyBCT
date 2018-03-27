using Backend;
using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Backend.Visitors;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    class DelegateTranslator
    {
        public static IList<IMethodReference> delegatedMethods
            = new List<IMethodReference>();

        public static IDictionary<IMethodReference, string> methodIdentifiers = 
            new Dictionary<IMethodReference, string>();

        public static void TrackDelegatedMethods(IMethodDefinition mD, MethodBody mB)
        {
            // translate instructions
            foreach (var ins in mB.Instructions)
            {
                DelegateInstructionVisitor instTranslator = new DelegateInstructionVisitor();
                ins.Accept(instTranslator);
            }
        }

        public static string CreateDelegateMethod()
        {
            var sb = new StringBuilder();

            sb.AppendLine("procedure {:inline 1} CreateDelegate(Method: int, Receiver: Ref, TypeParameters: Ref) returns(c: Ref);");
            sb.AppendLine("implementation {:inline 1} CreateDelegate(Method: int, Receiver: Ref, TypeParameters: Ref) returns(c: Ref)");
            sb.AppendLine("{");
            sb.AppendLine("     call c := Alloc();");
            sb.AppendLine("     assume $RefToDelegateReceiver(Method, c) == Receiver;");
            sb.AppendLine("     assume $RefToDelegateTypeParameters(Method, c) == TypeParameters;");

            foreach (var id in methodIdentifiers.Values)
                sb.AppendLine(String.Format("     assume $RefToDelegateMethod({0}, c) <==> Method == {0};", id));

            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string DefineMethodsIdentifiers()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var methodId in methodIdentifiers.Values)
                sb.AppendLine(String.Format("const unique {0}: int;", methodId));

            return sb.ToString();
        }

        public static void AddMethodIdentifier(IMethodReference methodRef)
        {
            if (methodIdentifiers.ContainsKey(methodRef))
                return;

            DelegateTranslator.delegatedMethods.Add(methodRef);

            var methodName = Helpers.GetMethodName(methodRef);
            var methodArity = Helpers.GetArityWithNonBoogieTypes(methodRef);

            // example: const unique cMain2.objectParameter$System.Object: int;
            //var methodId = String.Format("const unique {0}: int;", methodName + methodArity);
            var methodId = methodName + methodArity;

            methodIdentifiers.Add(methodRef, methodId);
        }

        class DelegateInstructionVisitor : InstructionVisitor
        {
            public override void Visit(LoadInstruction instruction)
            {
                if (instruction.Operand is StaticMethodReference) // delegates handling
                {
                    // we keep track of every function used for delegates
                    var loadDelegateStmt = instruction.Operand as StaticMethodReference;
                    var methodRef = loadDelegateStmt.Method;
                    AddMethodIdentifier(methodRef);
                }
            }
        }
    }
}
