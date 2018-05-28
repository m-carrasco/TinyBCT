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
            var boogieType = Helpers.GetBoogieType(value.Type);
            Contract.Assert(!string.IsNullOrEmpty(boogieType) && !boogieType.Equals("Ref"));

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
            var boogieType = Helpers.GetBoogieType(value.Type);
            Contract.Assert(!string.IsNullOrEmpty(boogieType) && !boogieType.Equals("Ref"));

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

            var boogieType = Helpers.GetBoogieType(value.Type);
            Contract.Assert(!string.IsNullOrEmpty(boogieType));

            if (!boogieType.Equals("Ref")) // int, bool, real
            {
                sb.AppendLine(AssumeInverseRelationUnionAndPrimitiveType(value));
                sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, PrimitiveType2Union(boogieType, opStr)));
            }
            else
            {
                sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, opStr));
            }

            return sb.ToString();
        }

        public string ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result)
        {
            StringBuilder sb = new StringBuilder();

            String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

            var boogieType = Helpers.GetBoogieType(result.Type);
            Contract.Assert(!string.IsNullOrEmpty(boogieType));

            if (!boogieType.Equals("Ref")) // int, bool, real
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

            return sb.ToString();
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

        public string VariableAssignment(IVariable variableA, string expr)
        {
            return VariableAssignment(variableA.ToString(), expr);
        }

        public string VariableAssignment(string variableA, string expr)
        {
            return string.Format("{0} := {1};", variableA, expr);
        }

        public string BinaryOperationExpression(IVariable op1, IVariable op2, BinaryOperation binaryOperation)
        {
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

            return BinaryOperationExpression(op1.ToString(), op2.ToString(), operation);
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
            return String.Format("assume $DynamicType({0}) == T${1}();", reference.Name, type);
        }

        public string TypeConstructor(string type)
        {
            return String.Format("$TypeConstructor({0})", type);
        }

        public string AssumeTypeConstructor(string arg, ITypeReference type)
        {
            return String.Format("assume $TypeConstructor({0}) == T${1};", arg, type);
        }
    }
}
