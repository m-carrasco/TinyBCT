using Backend;
using Backend.Analyses;
using Backend.ThreeAddressCode.Instructions;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    class Helpers
    {
        public static bool IsInstructionImplemented(Instruction inst)
        {
            if (inst is MethodCallInstruction ||
                inst is LoadInstruction ||
                inst is UnconditionalBranchInstruction ||
                inst is BinaryInstruction ||
                inst is NopInstruction ||
                inst is ReturnInstruction ||
                inst is ConditionalBranchInstruction ||
                inst is CreateObjectInstruction ||
                inst is StoreInstruction)
                return true;

            return false;
        }
        public static String GetBoogieType(ITypeReference type)
        {
            if (type.TypeCode.Equals(PrimitiveTypeCode.Int32))
                return "int";

            if (type.TypeCode.Equals(PrimitiveTypeCode.Boolean))
                return "bool";
            // hack 
            if (type.TypeCode.Equals(PrimitiveTypeCode.NotPrimitive))
                return "Ref";

            return null;
        }

        public static String GetMethodName(IMethodReference methodDefinition)
        {
            var signature = MemberHelper.GetMethodSignature(methodDefinition, NameFormattingOptions.Signature | NameFormattingOptions.SupressAttributeSuffix);
            // workaround
            // Test.NoHeap.subtract(System.Int32, System.Int32) -> Test.NoHeap.subtract
            var split = signature.Split('(');
            //..ctor()
            return split[0].Replace("..", ".");
        }

        public static String GetMethodBoogieReturnType(IMethodReference methodDefinition)
        {
            return GetBoogieType(methodDefinition.Type);
        }

        public static String GetMethodDefinition(IMethodReference methodRef, bool IsExtern)
        {
            var methodName = Helpers.GetMethodName(methodRef);
            var arguments = Helpers.GetParametersWithBoogieType(methodRef);
            var returnType = Helpers.GetMethodBoogieReturnType(methodRef);

            var head = String.Empty;
            if (methodRef.Type.TypeCode != PrimitiveTypeCode.Void)
                head = String.Format("procedure{0} {1}({2}) returns (r : {3}){4}", IsExtern ? " {:extern}" : String.Empty, methodName, arguments, returnType, IsExtern ? ";" : String.Empty);
            else
                head = String.Format("procedure{0} {1}({2}){3}", IsExtern ? " {:extern}" : String.Empty, methodName, arguments, IsExtern ? ";" : String.Empty);

            return head;
        }

        public static String GetParametersWithBoogieType(IMethodReference methodRef)
        {
            var parameters = String.Empty;
            IMethodDefinition methodDef = methodRef as IMethodDefinition;
            if (methodDef != null)
                parameters =  String.Join(",", methodDef.Parameters.Select(v => v.Name + " : " + GetBoogieType(v.Type)));
            else
                parameters = String.Join(",", methodRef.Parameters.Select(v => String.Format("param{0}", v.Index) + " : " + GetBoogieType(v.Type)));

            if (methodRef.CallingConvention.HasFlag(Microsoft.Cci.CallingConvention.HasThis))
                parameters = String.Format("this : Ref{0}{1}", methodRef.ParameterCount > 0 ? "," : String.Empty, parameters);

            return parameters;
        }

        // workaround
        public static Boolean IsExternal(IMethodDefinition methodDefinition)
        {
            if (methodDefinition.IsConstructor)
            {
                var methodName = Helpers.GetMethodName(methodDefinition);
                if (methodName.Equals("System.Object.ctor"))
                    return true;
            }

            if (methodDefinition.IsExternal)
                return true;

            return false;
        }
    }
}
