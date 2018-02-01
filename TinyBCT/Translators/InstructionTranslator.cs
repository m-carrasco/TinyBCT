using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Backend.Visitors;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyBCT.Translators;

namespace TinyBCT
{
    class InstructionTranslator : InstructionVisitor
    {

        public static ISet<IMethodReference> ExternMethodsCalled = new HashSet<IMethodReference>();
        public string Result() { return sb.ToString(); }
        private StringBuilder sb = new StringBuilder();

        private void addLabel(Instruction instr)
        {
            sb.AppendLine(String.Format("\t{0}:", instr.Label));
        }

        public override void Visit(NopInstruction instruction)
        {
            //addLabel(instruction);
            sb.Append(String.Format("\t{0}:", instruction.Label));
        }

        public override void Visit(BinaryInstruction instruction)
        {
            addLabel(instruction);

            IVariable left = instruction.LeftOperand;
            IVariable right = instruction.RightOperand;

            String operation = String.Empty;

            switch (instruction.Operation)
            {
                case BinaryOperation.Add: operation = "+"; break;
                case BinaryOperation.Sub: operation = "-"; break;
                case BinaryOperation.Mul: operation = "*"; break;
                case BinaryOperation.Div: operation = "/"; break;
                /*case BinaryOperation.Rem: operation = "%"; break;
                case BinaryOperation.And: operation = "&"; break;
                case BinaryOperation.Or: operation = "|"; break;
                case BinaryOperation.Xor: operation = "^"; break;
                case BinaryOperation.Shl: operation = "<<"; break;
                case BinaryOperation.Shr: operation = ">>"; break;*/
                case BinaryOperation.Eq: operation = "=="; break;
                case BinaryOperation.Neq: operation = "!="; break;
                case BinaryOperation.Gt: operation = ">"; break;
                case BinaryOperation.Ge: operation = ">="; break;
                case BinaryOperation.Lt: operation = "<"; break;
                case BinaryOperation.Le: operation = "<="; break;
            }

            // workaround for bug typing bools and integers
            // we need to tell if an integer is used as a boolean
            // temp fix until fixed from analysis-net framework.
            switch (instruction.Operation)
            {
                case BinaryOperation.Eq:
                case BinaryOperation.Neq:
                case BinaryOperation.Gt:
                case BinaryOperation.Ge:
                case BinaryOperation.Lt:
                case BinaryOperation.Le:
                    if ( (Helpers.GetBoogieType(left.Type).Equals("int") && Helpers.GetBoogieType(right.Type).Equals("bool")) ||
                        (Helpers.GetBoogieType(right.Type).Equals("int") && Helpers.GetBoogieType(left.Type).Equals("bool")))
                    {
                        var leftFixed = Helpers.GetBoogieType(left.Type).Equals("int") ? String.Format("Int2Bool({0})", left.ToString()) : left.ToString();
                        var rightFixed = Helpers.GetBoogieType(right.Type).Equals("int") ? String.Format("Int2Bool({0})", right.ToString()) : right.ToString();
                        sb.Append(String.Format("\t\t{0} {1} {2} {3} {4};", instruction.Result, ":=", leftFixed, operation, rightFixed));
						return;
					}
					break;
            }

            sb.Append(String.Format("\t\t{0} {1} {2} {3} {4};", instruction.Result, ":=", left, operation, right));
        }

        public override void Visit(UnconditionalBranchInstruction instruction)
        {
            addLabel(instruction);
            sb.Append(String.Format("\t\tgoto {0};", instruction.Target));
        }

        public override void Visit(ReturnInstruction instruction)
        {
            addLabel(instruction);
            if (instruction.HasOperand)
                sb.Append(String.Format("\t\tr := {0};", instruction.Operand.Name));
        }

        public override void Visit(LoadInstruction instruction)
        {
            addLabel(instruction);
            InstanceFieldAccess op = instruction.Operand as InstanceFieldAccess;
            if (op != null)
            {
                if (Helpers.GetBoogieType(op.Type).Equals("int"))
                    sb.Append(String.Format("\t\t{0} := Union2Int(Read($Heap,{1},{2}));", instruction.Result, op.Instance, FieldTranslator.fieldNames[op.Field]));
                else if (Helpers.GetBoogieType(op.Type).Equals("Ref"))
                    // Union and Ref are alias. There is no need of Union2Ref
                    sb.Append(String.Format("\t\t{0} := Read($Heap,{1},{2});", instruction.Result, op.Instance, FieldTranslator.fieldNames[op.Field]));

            } else
                sb.Append(String.Format("\t\t{0} := {1};", instruction.Result, instruction.Operand));
        }

        public override void Visit(TryInstruction instruction)
        {
            sb.Append("// TryInstruction not implemented yet.");
        }

        public override void Visit(FinallyInstruction instruction)
        {
            sb.Append("// FinallyInstruction not implemented yet.");
        }

        public override void Visit(MethodCallInstruction instruction)
        {
            addLabel(instruction);
            var signature = Helpers.GetMethodName(instruction.Method);
            var arguments = string.Join(", ", instruction.Arguments);

            if (instruction.HasResult)
                sb.Append(String.Format("\t\tcall {0} := {1}({2});", instruction.Result, signature, arguments));
            else
                sb.Append(String.Format("\t\tcall {0}({1});", signature, arguments));

            if (Helpers.IsExternal(instruction.Method.ResolvedMethod))
                ExternMethodsCalled.Add(instruction.Method);
        }

        public override void Visit(ConditionalBranchInstruction instruction)
        {
            addLabel(instruction);

            IVariable leftOperand = instruction.LeftOperand;
            IInmediateValue rightOperand = instruction.RightOperand;

            var operation = string.Empty;

            switch (instruction.Operation)
            {
                case BranchOperation.Eq: operation = "=="; break;
                case BranchOperation.Neq: operation = "!="; break;
                case BranchOperation.Gt: operation = ">"; break;
                case BranchOperation.Ge: operation = ">="; break;
                case BranchOperation.Lt: operation = "<"; break;
                case BranchOperation.Le: operation = "<="; break;
            }


            sb.AppendLine(String.Format("\t\tif ({0} {1} {2})", leftOperand, operation, rightOperand));
            sb.AppendLine("\t\t{");
            sb.AppendLine(String.Format("\t\t\tgoto {0};", instruction.Target));
            sb.Append("\t\t}");

        }

        public override void Visit(CreateObjectInstruction instruction)
        {
            //addLabel(instruction);
            sb.Append(String.Format("\t\tcall {0}:= Alloc();", instruction.Result));
        }

        public override void Visit(StoreInstruction instruction)
        {
            addLabel(instruction);

            var op = instruction.Operand; // what it is stored
            var instanceFieldAccess = instruction.Result as InstanceFieldAccess; // where it is stored

            if (instanceFieldAccess != null)
            {
                if (Helpers.GetBoogieType(op.Type).Equals("int"))
                {
                    sb.AppendLine(String.Format("\t\tassume Union2Int(Int2Union({0})) == {0};", op));
                    sb.Append(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, FieldTranslator.fieldNames[instanceFieldAccess.Field], String.Format("Int2Union({0})",op)));
                } else if (Helpers.GetBoogieType(op.Type).Equals("Ref"))
                {
                    //sb.AppendLine(String.Format("\t\tassume Union2Int(Int2Union({0})) == {0};", op));
                    // Union y Ref son el mismo type, forman un alias.
                    sb.Append(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, FieldTranslator.fieldNames[instanceFieldAccess.Field], /*String.Format("Int2Union({0})", op)*/op));
                }
            }
        }
    }
}
