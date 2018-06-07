using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyBCT.Translators;

namespace TinyBCT
{
    public class BoogieGenerator
    {
        private static BoogieGenerator singleton;

        public static BoogieGenerator Instance()
        {
            if (singleton == null)
                singleton = new BoogieGenerator();

            return singleton;
        }

        public string AssumeInverseRelationUnionAndPrimitiveType(string variable, string boogieType)
        {
            boogieType = boogieType[0].ToString().ToUpper() + boogieType.Substring(1);
            var e1 = string.Format("{0}2Union({1})", boogieType, variable);
            return string.Format("assume Union2{0}({1}) == {2};", boogieType, e1, variable);
        }

        public string AssumeInverseRelationUnionAndPrimitiveType(IVariable variable)
        {
            var boogieType = Helpers.GetBoogieType(variable.Type);
            Contract.Assert(!String.IsNullOrEmpty(boogieType));

            return AssumeInverseRelationUnionAndPrimitiveType(variable.ToString(), boogieType);
        }

        public string PrimitiveType2Union(IVariable value)
        {
            Contract.Assert(!Helpers.IsBoogieRefType(value.Type));
            var boogieType = Helpers.GetBoogieType(value.Type);
            return PrimitiveType2Union(boogieType, value.ToString());
        }
        public string PrimitiveType2Union(string boogieType, string value)
        {
            // int -> Int, bool -> Bool
            boogieType = boogieType[0].ToString().ToUpper() + boogieType.Substring(1);
            return string.Format("{0}2Union({1})", boogieType, value);
        }

        public string Union2PrimitiveType(IVariable value)
        {
            Contract.Assert(!Helpers.IsBoogieRefType(value.Type));
            var boogieType = Helpers.GetBoogieType(value.Type);

            return Union2PrimitiveType(boogieType, value.ToString());
        }
        public string Union2PrimitiveType(string boogieType, string value)
        {
            // int -> Int, bool -> Bool
            boogieType = boogieType[0].ToString().ToUpper() + boogieType.Substring(1);
            return string.Format("Union2{0}({1})", boogieType, value);
        }

        public string WriteStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            StringBuilder sb = new StringBuilder();

            string opStr = value.ToString();
            if (value.Type.TypeCode.Equals(PrimitiveTypeCode.String))
                opStr = Helpers.Strings.fixStringLiteral(value);

            String fieldName = FieldTranslator.GetFieldName(staticFieldAccess.Field);
            sb.Append(VariableAssignment(fieldName, opStr));

            return sb.ToString();
        }

        public string ReadStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            StringBuilder sb = new StringBuilder();

            String fieldName = FieldTranslator.GetFieldName(staticFieldAccess.Field);
            sb.Append(VariableAssignment(value, fieldName));

            return sb.ToString();
        }

        public string WriteInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable value)
        {
            StringBuilder sb = new StringBuilder();

            string opStr = value.ToString();
            if (value.Type.TypeCode.Equals(PrimitiveTypeCode.String))
                opStr = Helpers.Strings.fixStringLiteral(value);

            String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

            if (!Settings.SplitFields)
            {
                if (!Helpers.IsBoogieRefType(value.Type)) // int, bool, real
                {
                    sb.AppendLine(AssumeInverseRelationUnionAndPrimitiveType(value));
                    sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, PrimitiveType2Union(Helpers.GetBoogieType(value.Type), opStr)));
                }
                else
                {
                    sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, opStr));
                }
            }
            else
            {
                var boogieType = Helpers.GetBoogieType(value.Type);
                Contract.Assert(!string.IsNullOrEmpty(boogieType));
                var heapAccess = String.Format("{0}[{1}]", fieldName, instanceFieldAccess.Instance);
                //F$ConsoleApplication3.Foo.p[f_Ref] := $ArrayContents[args][0];

                if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals("Ref"))
                {
                    sb.AppendLine(AssumeInverseRelationUnionAndPrimitiveType(value));
                    sb.AppendLine(VariableAssignment(heapAccess, PrimitiveType2Union(boogieType, opStr)));
                } else
                    sb.AppendLine(VariableAssignment(heapAccess, opStr));
            }


            return sb.ToString();
        }

        public string ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result)
        {
            StringBuilder sb = new StringBuilder();

            String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

            var boogieType = Helpers.GetBoogieType(result.Type);
            Contract.Assert(!string.IsNullOrEmpty(boogieType));

            if (!Settings.SplitFields)
            {

                if (!Helpers.IsBoogieRefType(result.Type)) // int, bool, real
                {
                    // example: Union2Int(Read(...))
                    var expr = Union2PrimitiveType(boogieType, String.Format("Read($Heap,{0},{1})", instanceFieldAccess.Instance, fieldName));
                    sb.AppendLine(VariableAssignment(result, expr));
                }
                else
                {
                    var expr = String.Format("Read($Heap,{0},{1})", instanceFieldAccess.Instance, fieldName);
                    sb.AppendLine(VariableAssignment(result, expr));
                }
            } else
            {
                var heapAccess = string.Format("{0}[{1}]", fieldName, instanceFieldAccess.Instance.Name);

                //p_int:= F$ConsoleApplication3.Holds`1.x[$tmp2];
                if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals("Ref"))
                {
                    sb.AppendLine(VariableAssignment(result, Union2PrimitiveType(boogieType, heapAccess)));
                } else
                    sb.AppendLine(VariableAssignment(result, heapAccess));
            }

            return sb.ToString();
        }

        public string ProcedureCall(IMethodReference procedure, List<IVariable> argumentList, IVariable resultVariable = null)
        {
            StringBuilder sb = new StringBuilder();
            var boogieProcedureName = Helpers.GetMethodName(procedure);
            return ProcedureCall(boogieProcedureName, argumentList.Select(v => v.Name).ToList(), resultVariable == null ? String.Empty : resultVariable.Name);
        }

        public string ProcedureCall(string boogieProcedureName, List<string> argumentList, string resultVariable = null)
        {
            StringBuilder sb = new StringBuilder();

            var arguments = String.Join(",", argumentList);

            if (!String.IsNullOrEmpty(resultVariable))
                return string.Format("call {0} := {1}({2});", resultVariable, boogieProcedureName, arguments);
            else
                return string.Format("call {1}({2});", resultVariable, boogieProcedureName, arguments);
        }

        public string VariableAssignment(IVariable variableA, IValue value)
        {
            Constant cons = value as Constant;
            if (cons != null && (cons.Value is Single || cons.Value is Double || cons.Value is Decimal))
            {
                // default string representation of floating point types is not suitable for boogie
                // boogie wants dot instead of ,
                // "F" forces to add decimal part
                string str = "";
                if (cons.Value is Single)
                {
                    Single v = (Single)cons.Value;
                    str = v.ToString("F").Replace(",", ".");
                } else if (cons.Value is Double)
                {
                    Double v = (Double)cons.Value;
                    str = v.ToString("F").Replace(",", ".");
                } else if (cons.Value is Decimal)
                {
                    Decimal v = (Decimal)cons.Value;
                    str = v.ToString("F").Replace(",", ".");
                }

                return VariableAssignment(variableA.ToString(), str);
            }

            return VariableAssignment(variableA.ToString(), value.ToString());
        }

        public string VariableAssignment(IVariable variableA, string expr)
        {
            return VariableAssignment(variableA.ToString(), expr);
        }

        public string VariableAssignment(string variableA, string expr)
        {
            return string.Format("{0} := {1};", variableA, expr);
        }

        public static bool IsSupportedBinaryOperation(BinaryOperation binaryOperation)
        {
            switch (binaryOperation)
            {
                case BinaryOperation.Add:
                case BinaryOperation.Sub:
                case BinaryOperation.Mul:
                case BinaryOperation.Div:
                case BinaryOperation.Eq:
                case BinaryOperation.Neq:
                case BinaryOperation.Gt:
                case BinaryOperation.Ge:
                case BinaryOperation.Lt:
                case BinaryOperation.Le:
                    return true;
                default:
                    return false;
            }
        }

        public string HavocResult(DefinitionInstruction instruction)
        {
            return String.Format("havoc {0};", instruction.Result);
        }

        public string BranchOperationExpression(IVariable op1, IInmediateValue op2, BranchOperation branchOperation)
        {
            var operation = string.Empty;

            switch (branchOperation)
            {
                case BranchOperation.Eq: operation = "=="; break;
                case BranchOperation.Neq: operation = "!="; break;
                case BranchOperation.Gt: operation = ">"; break;
                case BranchOperation.Ge: operation = ">="; break;
                case BranchOperation.Lt: operation = "<"; break;
                case BranchOperation.Le: operation = "<="; break;
            }

            return BranchOperationExpression(op1.Name, op2.ToString(), operation);
        }

        public string BranchOperationExpression(string op1, string op2, string operation)
        {
            return string.Format("{0} {1} {2}", op1, operation, op2);
        }

        public string BinaryOperationExpression(IVariable op1, IVariable op2, BinaryOperation binaryOperation)
        {
            Contract.Assume(IsSupportedBinaryOperation(binaryOperation));
            string operation = String.Empty;
            switch (binaryOperation)
            {
                case BinaryOperation.Add: operation = "+"; break;
                case BinaryOperation.Sub: operation = "-"; break;
                case BinaryOperation.Mul: operation = "*"; break;
                case BinaryOperation.Div: operation = "/"; break;
                // not implemented yet
                /*case BinaryOperation.Rem: operation = "%"; break;
                case BinaryOperation.And: operation = "&"; break;
                case BinaryOperation.Or: operation = "|"; break;
                case BinaryOperation.Xor: operation = "^"; break;
                case BinaryOperation.Shl: operation = "<<"; break;
                case BinaryOperation.Shr: operation = ">>"; break;*/
                case BinaryOperation.Eq: operation = "=="; break;
                case BinaryOperation.Neq: operation = "!="; break;
                case BinaryOperation.Gt:
                    operation = ">";
                    var leftAsConstant = op1 as Constant;
                    var rightAsConstant = op2 as Constant;
                    if (leftAsConstant != null && rightAsConstant != null)
                    {
                        // There was a bug when comparing references, checking that this doesn't show up again.
                        Contract.Assume(leftAsConstant.Value != null && rightAsConstant.Value != null);
                    }
                    break;
                case BinaryOperation.Ge: operation = ">="; break;
                case BinaryOperation.Lt: operation = "<"; break;
                case BinaryOperation.Le: operation = "<="; break;
                default:
                    Contract.Assert(false);
                    break;
            }

            return BinaryOperationExpression(op1.Name, op2.Name, operation);
        }

        public string BinaryOperationExpression(string op1, string op2, string operation)
        {
            return string.Format("{0} {1} {2}", op1, operation, op2);
        }

        public string Goto(string label)
        {
            return String.Format("\t\tgoto {0};", label);
        }

        public string DynamicType(IVariable reference)
        {
            return DynamicType(reference.Name);
        }

        public string DynamicType(string reference)
        {
            return String.Format("$DynamicType({0})", reference);
        }

        public string AssumeDynamicType(IVariable reference, ITypeReference type)
        {
            return AssumeDynamicType(reference.Name, type);
        }

        public string AssumeDynamicType(string name, ITypeReference type)
        {
            return String.Format("assume $DynamicType({0}) == {1};", name, Helpers.GetNormalizedTypeFunction(type, InstructionTranslator.MentionedClasses));
        }

        public string TypeConstructor(string type)
        {
            return String.Format("$TypeConstructor({0})", type);
        }

        public string AssumeTypeConstructor(string arg, ITypeReference type)
        {
            return AssumeTypeConstructor(arg, type.ToString());
        }

        public string AssumeTypeConstructor(string arg, string type)
        {
            return String.Format("assume $TypeConstructor($DynamicType({0})) == T${1};", arg, type);
        }

        public string Assert(IVariable cond)
        {
            return Assert(cond.Name);
        }

        public string Assert(string cond)
        {
            return String.Format("assert {0};", cond);
        }

        public string Assume(IVariable cond)
        {
            return Assume(cond.Name);
        }

        public string Assume(string cond)
        {
            return String.Format("assume {0};", cond);
        }

        public string If(string condition, string body)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(String.Format("if ({0})", condition));
            sb.AppendLine("{");
            sb.AppendLine(body);
            sb.AppendLine("}");

            return sb.ToString();
        }

        public string Else( string body)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.AppendLine(body);
            sb.AppendLine("}");

            return sb.ToString();
        }


        public string ElseIf(string condition, string body)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(String.Format("else if ({0})", condition));
            sb.AppendLine("{");
            sb.AppendLine(body);
            sb.AppendLine("}");

            return sb.ToString();
        }

        public string As(IVariable arg1, ITypeReference arg2)
        {
            // TODO(rcastano): Fix for generics
            return String.Format("$As({0},{1})", arg1, Helpers.GetNormalizedTypeFunction(arg2, InstructionTranslator.MentionedClasses));
        }

        public string Subtype(IVariable var, ITypeReference type)
        {
            return Subtype(var.Name, type);
        }

        public string Subtype(string var, ITypeReference type)
        {
            return string.Format("$Subtype({0}, {1})", var, Helpers.GetNormalizedTypeFunction(type, InstructionTranslator.MentionedClasses));
        }

        public string AssumeArrayLength(IVariable array, string length)
        {
            return Assume(String.Format("{0} == {1}", ArrayLength(array), length));
        }

        public string ArrayLength(IVariable arr)
        {
            return String.Format("$ArrayLength({0})", arr.Name);
        }

        public string ArrayLength(string arr)
        {
            return String.Format("$ArrayLength({0})", arr);
        }

        public string ReadArrayElement(string array, string index)
        {
            return string.Format("$ReadArrayElement({0}, {1})", array, index);
        }

        public string ReadArrayElement(IVariable array, IVariable index)
        {
            return ReadArrayElement(array.Name, index.Name);
        }

        public string CallReadArrayElement(string result, string array, string index)
        {
            var l = new List<string>();
            l.Add(array);
            l.Add(index);
            return ProcedureCall("$ReadArrayElement", l, result);
        }

        public string CallReadArrayElement(IVariable result, IVariable array, IVariable index)
        {
            return CallReadArrayElement(result.Name, array.Name, index.Name);
        }

        public string WriteArrayElement(string array, string index, string value)
        {
            return string.Format("$WriteArrayElement({0}, {1}, {2})", array, index, value);
        }

        public string WriteArrayElement(IVariable array, IVariable index, IVariable value)
        {
            return WriteArrayElement(array.Name, index.Name, value.Name);
        }

        public string CallWriteArrayElement(string array, string index, string value)
        {
            var l = new List<string>();
            l.Add(array);
            l.Add(index);
            l.Add(value);
            return ProcedureCall("$WriteArrayElement", l);
        }

        public string CallWriteArrayElement(IVariable array, IVariable index, IVariable value)
        {
            return CallWriteArrayElement(array.Name, index.Name, value.Name);
        }

        public string Return()
        {
            return "return;";
        }

        public string Negation(string b)
        {
            return String.Format("!{0}", b);
        }
    }
}
